import { useCallback, useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { getAccessToken, resolveBackendBaseUrl } from '@/shared/api/httpClient';
import type { CefrLevel, ConversationModeCode, TeacherVoiceName } from '@/shared/api/openapi.types';
import type {
  BilingualTurnItem,
  GrammarCorrectionItem,
  SessionPhase,
  ThreeHubStates,
} from '../types/tutor.types';

function arrayBufferToBase64(buffer: ArrayBuffer): string {
  const bytes = new Uint8Array(buffer);
  let binary = '';
  const chunkSize = 0x8000;
  for (let i = 0; i < bytes.length; i += chunkSize) {
    binary += String.fromCharCode(...bytes.subarray(i, i + chunkSize));
  }
  return btoa(binary);
}

function base64ToInt16Array(base64: string): Int16Array {
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) {
    bytes[i] = binary.charCodeAt(i);
  }
  return new Int16Array(bytes.buffer);
}

interface StartLiveSessionParams {
  cefrLevel: CefrLevel;
  extendedThinking: boolean;
  voiceName: TeacherVoiceName;
  speechSpeed: number;
  conversationMode: ConversationModeCode;
  topic: string;
  customSystemPrompt?: string;
  conversationId?: string | null;
}

export function useThreeHubLiveSession() {
  const [phase, setPhase] = useState<SessionPhase>('idle');
  const [hubStates, setHubStates] = useState<ThreeHubStates>({
    tutorHub: 'disconnected',
    transcribeHub: 'disconnected',
    translateHub: 'disconnected',
  });
  const [turns, setTurns] = useState<BilingualTurnItem[]>([]);
  const [corrections, setCorrections] = useState<GrammarCorrectionItem[]>([]);
  const [errorBanner, setErrorBanner] = useState<string | null>(null);
  const [micLevel, setMicLevel] = useState<number>(0);
  const [aiAudioLevel, setAiAudioLevel] = useState<number>(0);

  const tutorConnRef = useRef<signalR.HubConnection | null>(null);
  const transcribeConnRef = useRef<signalR.HubConnection | null>(null);
  const translateConnRef = useRef<signalR.HubConnection | null>(null);

  const captureCtxRef = useRef<AudioContext | null>(null);
  const mediaStreamRef = useRef<MediaStream | null>(null);
  const workletNodeRef = useRef<AudioWorkletNode | null>(null);

  const playbackCtxRef = useRef<AudioContext | null>(null);
  const nextPlayTimeRef = useRef<number>(0);
  const activeSourcesRef = useRef<Set<AudioBufferSourceNode>>(new Set());
  const sessionIdRef = useRef<string>('');
  const isAiSpeakingRef = useRef<boolean>(false);

  const flushPlaybackQueue = useCallback(() => {
    for (const src of activeSourcesRef.current) {
      try {
        src.stop();
        src.disconnect();
      } catch {
        // ignore
      }
    }
    activeSourcesRef.current.clear();
    if (playbackCtxRef.current) {
      nextPlayTimeRef.current = playbackCtxRef.current.currentTime;
    }
    isAiSpeakingRef.current = false;
    setAiAudioLevel(0);
  }, []);

  const scheduleAiPcm24kChunk = useCallback((pcmBase64: string) => {
    const ctx = playbackCtxRef.current;
    if (!ctx) return;
    if (ctx.state === 'suspended') {
      void ctx.resume();
    }

    const int16 = base64ToInt16Array(pcmBase64);
    if (int16.length === 0) return;

    const float32 = new Float32Array(int16.length);
    let sumSq = 0;
    for (let i = 0; i < int16.length; i++) {
      const sample = int16[i] / 32768.0;
      float32[i] = sample;
      sumSq += sample * sample;
    }
    const rms = Math.sqrt(sumSq / float32.length);
    setAiAudioLevel(Math.min(1, rms * 4.5));

    const audioBuffer = ctx.createBuffer(1, float32.length, 24000);
    audioBuffer.copyToChannel(float32, 0);

    const source = ctx.createBufferSource();
    source.buffer = audioBuffer;
    source.connect(ctx.destination);

    const now = ctx.currentTime;
    if (nextPlayTimeRef.current < now + 0.015) {
      nextPlayTimeRef.current = now + 0.03;
    }

    source.start(nextPlayTimeRef.current);
    nextPlayTimeRef.current += audioBuffer.duration;
    activeSourcesRef.current.add(source);
    isAiSpeakingRef.current = true;
    setPhase('ai_speaking');

    source.onended = () => {
      activeSourcesRef.current.delete(source);
      if (activeSourcesRef.current.size === 0) {
        isAiSpeakingRef.current = false;
        setAiAudioLevel(0);
        setPhase((prev) => (prev === 'ai_speaking' ? 'listening' : prev));
      }
    };
  }, []);

  const upsertTurnLine1 = useCallback(
    (turnId: string, speaker: 'user' | 'ai', englishText: string, isFinal: boolean) => {
      if (!englishText) return;
      setTurns((prev) => {
        const idx = prev.findIndex((t) => t.turnId === turnId);
        if (idx >= 0) {
          const updated = [...prev];
          updated[idx] = {
            ...updated[idx],
            englishLine1: englishText,
            isFinal: updated[idx].isFinal || isFinal,
          };
          return updated;
        }
        return [
          ...prev,
          {
            turnId,
            speaker,
            englishLine1: englishText,
            vietnameseLine2: '',
            isFinal,
            timestamp: Date.now(),
          },
        ];
      });
    },
    []
  );

  const upsertTurnLine2 = useCallback(
    (turnId: string, speaker: 'user' | 'ai', vietnameseText: string, isFinal: boolean) => {
      if (!vietnameseText) return;
      setTurns((prev) => {
        const idx = prev.findIndex((t) => t.turnId === turnId);
        if (idx >= 0) {
          const updated = [...prev];
          updated[idx] = {
            ...updated[idx],
            vietnameseLine2: vietnameseText,
            isFinal: updated[idx].isFinal || isFinal,
          };
          return updated;
        }
        return [
          ...prev,
          {
            turnId,
            speaker,
            englishLine1: '',
            vietnameseLine2: vietnameseText,
            isFinal,
            timestamp: Date.now(),
          },
        ];
      });
    },
    []
  );

  const stopSession = useCallback(async () => {
    const sid = sessionIdRef.current;
    sessionIdRef.current = '';

    try {
      workletNodeRef.current?.disconnect();
    } catch {
      // ignore
    }
    workletNodeRef.current = null;

    if (mediaStreamRef.current) {
      mediaStreamRef.current.getTracks().forEach((t) => t.stop());
      mediaStreamRef.current = null;
    }

    if (captureCtxRef.current) {
      try {
        await captureCtxRef.current.close();
      } catch {
        // ignore
      }
      captureCtxRef.current = null;
    }

    flushPlaybackQueue();
    if (playbackCtxRef.current) {
      try {
        await playbackCtxRef.current.close();
      } catch {
        // ignore
      }
      playbackCtxRef.current = null;
    }

    if (tutorConnRef.current) {
      try {
        if (sid && tutorConnRef.current.state === signalR.HubConnectionState.Connected) {
          await tutorConnRef.current.invoke('StopSession', sid);
        }
        await tutorConnRef.current.stop();
      } catch {
        // ignore
      }
      tutorConnRef.current = null;
    }

    if (transcribeConnRef.current) {
      try {
        await transcribeConnRef.current.stop();
      } catch {
        // ignore
      }
      transcribeConnRef.current = null;
    }

    if (translateConnRef.current) {
      try {
        await translateConnRef.current.stop();
      } catch {
        // ignore
      }
      translateConnRef.current = null;
    }

    setHubStates({
      tutorHub: 'disconnected',
      transcribeHub: 'disconnected',
      translateHub: 'disconnected',
    });
    setMicLevel(0);
    setAiAudioLevel(0);
    setPhase('idle');
  }, [flushPlaybackQueue]);

  const startSession = useCallback(
    async (params: StartLiveSessionParams) => {
      await stopSession();
      setErrorBanner(null);
      setPhase('connecting');

      const sessionId =
        params.conversationId ||
        (typeof crypto.randomUUID === 'function' ? crypto.randomUUID() : `sess-${Date.now()}`);
      sessionIdRef.current = sessionId;

      const baseUrl = resolveBackendBaseUrl();
      const accessTokenFactory = () => getAccessToken() ?? '';

      const buildHub = (path: string) =>
        new signalR.HubConnectionBuilder()
          .withUrl(`${baseUrl}${path}`, { accessTokenFactory })
          .withAutomaticReconnect([0, 1500, 3000, 5000])
          .configureLogging(signalR.LogLevel.Warning)
          .build();

      const tutorHub = buildHub('/hubs/tutor');
      const transcribeHub = buildHub('/hubs/transcribe');
      const translateHub = buildHub('/hubs/translate');

      tutorConnRef.current = tutorHub;
      transcribeConnRef.current = transcribeHub;
      translateConnRef.current = translateHub;

      tutorHub.on('ReceiveAiAudioChunk', (payload: { pcm24kBase64: string }) => {
        if (payload?.pcm24kBase64) {
          scheduleAiPcm24kChunk(payload.pcm24kBase64);
        }
      });

      tutorHub.on('AiInterrupted', () => {
        flushPlaybackQueue();
        setPhase('user_speaking');
      });

      tutorHub.on('AiTurnComplete', () => {
        if (activeSourcesRef.current.size === 0) {
          setPhase('listening');
        }
      });

      tutorHub.on('SessionError', (payload: { message: string }) => {
        setErrorBanner(payload?.message || 'Lỗi phiên kết nối AI Tutor.');
        setPhase('error');
      });

      transcribeHub.on(
        'ReceiveTranscription',
        (payload: {
          turnId: string;
          speaker: 'user' | 'ai';
          englishText: string;
          isFinal: boolean;
        }) => {
          if (payload?.turnId) {
            upsertTurnLine1(payload.turnId, payload.speaker, payload.englishText, payload.isFinal);
          }
        }
      );

      transcribeHub.on('ReceiveGrammarCorrection', (payload: GrammarCorrectionItem) => {
        if (payload?.turnId && payload.hasErrors) {
          setCorrections((prev) => {
            const filtered = prev.filter((c) => c.turnId !== payload.turnId);
            return [{ ...payload, timestamp: Date.now() }, ...filtered];
          });
        }
      });

      translateHub.on(
        'ReceiveVietnameseTranslation',
        (payload: {
          turnId: string;
          speaker: 'user' | 'ai';
          vietnameseText: string;
          isFinal: boolean;
        }) => {
          if (payload?.turnId) {
            upsertTurnLine2(
              payload.turnId,
              payload.speaker,
              payload.vietnameseText,
              payload.isFinal
            );
          }
        }
      );

      try {
        setHubStates({
          tutorHub: 'connecting',
          transcribeHub: 'connecting',
          translateHub: 'connecting',
        });

        await Promise.all([tutorHub.start(), transcribeHub.start(), translateHub.start()]);

        setHubStates({
          tutorHub: 'connected',
          transcribeHub: 'connected',
          translateHub: 'connected',
        });

        await Promise.all([
          transcribeHub.invoke('JoinTranscriptionSession', sessionId),
          translateHub.invoke('JoinTranslationSession', sessionId),
        ]);

        await tutorHub.invoke('StartSession', {
          sessionId,
          cefrLevel: params.cefrLevel,
          extendedThinking: params.extendedThinking,
          voiceName: params.voiceName,
          speechSpeed: params.speechSpeed,
          conversationMode: params.conversationMode,
          topic: params.topic,
          customSystemPrompt: params.customSystemPrompt ?? '',
        });

        // Initialize 24kHz Playback AudioContext
        playbackCtxRef.current = new AudioContext({ sampleRate: 24000 });
        nextPlayTimeRef.current = playbackCtxRef.current.currentTime;

        // Initialize 16kHz Microphone Capture + AudioWorklet
        const stream = await navigator.mediaDevices.getUserMedia({
          audio: {
            channelCount: 1,
            sampleRate: 16000,
            echoCancellation: true,
            noiseSuppression: true,
            autoGainControl: true,
          },
        });
        mediaStreamRef.current = stream;

        const captureCtx = new AudioContext({ sampleRate: 16000 });
        captureCtxRef.current = captureCtx;
        await captureCtx.audioWorklet.addModule('/pcm16k-capture-worklet.js');

        const source = captureCtx.createMediaStreamSource(stream);
        const workletNode = new AudioWorkletNode(captureCtx, 'pcm16k-capture-processor');
        workletNodeRef.current = workletNode;

        workletNode.port.onmessage = (event) => {
          const { pcm16Buffer, rms } = event.data as { pcm16Buffer: ArrayBuffer; rms: number };
          setMicLevel(Math.min(1, (rms || 0) * 6));

          if (rms > 0.025 && isAiSpeakingRef.current) {
            flushPlaybackQueue();
          }

          if (tutorHub.state === signalR.HubConnectionState.Connected && sessionIdRef.current) {
            const base64Pcm = arrayBufferToBase64(pcm16Buffer);
            void tutorHub.invoke('SendMicrophonePcm16k', sessionIdRef.current, base64Pcm);
          }
        };

        source.connect(workletNode);
        setPhase('listening');
      } catch (err: any) {
        setErrorBanner(
          err?.message ||
            'Không thể kết nối 3 SignalR Hubs hoặc truy cập Microphone. Hãy kiểm tra Backend (.NET) đang chạy ở cổng 5099.'
        );
        setPhase('error');
      }
    },
    [flushPlaybackQueue, scheduleAiPcm24kChunk, stopSession, upsertTurnLine1, upsertTurnLine2]
  );

  const loadPersistedSessionData = useCallback(
    (loadedTurns: BilingualTurnItem[], loadedCorrections: GrammarCorrectionItem[]) => {
      setTurns(loadedTurns);
      setCorrections(loadedCorrections);
    },
    []
  );

  useEffect(() => {
    return () => {
      void stopSession();
    };
  }, [stopSession]);

  return {
    phase,
    hubStates,
    turns,
    corrections,
    errorBanner,
    micLevel,
    aiAudioLevel,
    startSession,
    stopSession,
    loadPersistedSessionData,
    clearError: () => setErrorBanner(null),
  };
}
