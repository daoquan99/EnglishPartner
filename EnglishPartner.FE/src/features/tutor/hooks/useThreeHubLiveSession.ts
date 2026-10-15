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
  apiKey: string;
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
    const convId = sessionIdRef.current;
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

    if (convId) {
      await Promise.all([
        tutorConnRef.current?.invoke('NotifyUserSpeechEnd', convId).catch(() => {}),
        transcribeConnRef.current?.invoke('EndAudioTurn', convId).catch(() => {}),
        translateConnRef.current?.invoke('EndTranslateAudioTurn', convId).catch(() => {}),
      ]);
    }

    await Promise.all([
      tutorConnRef.current?.stop().catch(() => {}),
      transcribeConnRef.current?.stop().catch(() => {}),
      translateConnRef.current?.stop().catch(() => {}),
    ]);

    tutorConnRef.current = null;
    transcribeConnRef.current = null;
    translateConnRef.current = null;

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
      if (!params.apiKey || !params.apiKey.trim()) {
        setErrorBanner('Vui lòng nhập Google AI Studio API Key (AIzaSy...) ở cột Thiết lập trước khi bắt đầu.');
        setPhase('error');
        return;
      }

      await stopSession();
      setErrorBanner(null);
      setPhase('connecting');

      const convId =
        params.conversationId ||
        (typeof crypto.randomUUID === 'function' ? crypto.randomUUID() : `sess-${Date.now()}`);
      sessionIdRef.current = convId;

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

      // Support both event names from Backend Hubs
      tutorHub.on(
        'ReceiveTutorAudio',
        (dto: { turnId: string; base64Pcm24k: string; interactionStatus?: string }) => {
          if (dto?.base64Pcm24k) {
            scheduleAiPcm24kChunk(dto.base64Pcm24k);
          }
        }
      );

      tutorHub.on('ReceiveLanguageCorrection', (payload: GrammarCorrectionItem) => {
        if (payload?.turnId && payload.hasErrors) {
          setCorrections((prev) => {
            const filtered = prev.filter((c) => c.turnId !== payload.turnId);
            return [{ ...payload, timestamp: Date.now() }, ...filtered];
          });
        }
      });

      tutorHub.on('TutorInterrupted', () => {
        flushPlaybackQueue();
        setPhase('user_speaking');
      });

      transcribeHub.on(
        'ReceiveTranscriptLine1',
        (dto: {
          speaker: 'user' | 'ai';
          turnId: string;
          sequenceOrder: number;
          fullTranscriptText: string;
          isFinal: boolean;
        }) => {
          if (dto?.turnId) {
            upsertTurnLine1(dto.turnId, dto.speaker, dto.fullTranscriptText, dto.isFinal);
          }
        }
      );

      translateHub.on(
        'ReceiveTranslationLine2',
        (dto: {
          speaker: 'user' | 'ai';
          turnId: string;
          sequenceOrder: number;
          fullVietnameseText: string;
          isFinal: boolean;
        }) => {
          if (dto?.turnId) {
            upsertTurnLine2(dto.turnId, dto.speaker, dto.fullVietnameseText, dto.isFinal);
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

        // Initialize Tutor session first, then Sub-Hubs sequentially
        await tutorHub.invoke('InitializeTutorSession', {
          conversationId: convId,
          apiKey: params.apiKey.trim(),
          cefrLevel: params.cefrLevel,
          extendedThinking: params.extendedThinking,
          thinkingLevel: 'medium',
          voiceName: params.voiceName,
          speechSpeed: params.speechSpeed,
          conversationMode: params.conversationMode,
          topic: params.topic.trim(),
          customSystemPrompt: params.customSystemPrompt?.trim() || null,
        });
        setHubStates((prev) => ({ ...prev, tutorHub: 'connected' }));

        await transcribeHub.invoke('InitializeTranscribeSession', {
          conversationId: convId,
          apiKey: params.apiKey.trim(),
        });
        setHubStates((prev) => ({ ...prev, transcribeHub: 'connected' }));

        await translateHub.invoke('InitializeTranslateSession', {
          conversationId: convId,
          apiKey: params.apiKey.trim(),
        });
        setHubStates({
          tutorHub: 'connected',
          transcribeHub: 'connected',
          translateHub: 'connected',
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
        const source = captureCtx.createMediaStreamSource(stream);

        const handleAudioChunk = (pcmBuffer: ArrayBuffer) => {
          const int16 = new Int16Array(pcmBuffer);
          let sumSq = 0;
          for (let i = 0; i < int16.length; i++) {
            const norm = int16[i] / 32768.0;
            sumSq += norm * norm;
          }
          const rms = int16.length > 0 ? Math.sqrt(sumSq / int16.length) : 0;
          setMicLevel(Math.min(1, rms * 6));

          const base64Pcm16k = arrayBufferToBase64(pcmBuffer);
          const payload = {
            conversationId: convId,
            speaker: 'user',
            turnId: null,
            base64Pcm16k,
          };

          tutorHub.invoke('StreamUserAudioToTutor', payload).catch(() => {});
          transcribeHub.invoke('StreamAudioForTranscription', payload).catch(() => {});
          translateHub.invoke('StreamAudioForTranslation', payload).catch(() => {});
        };

        try {
          await captureCtx.audioWorklet.addModule('/pcm16k-capture-worklet.js');
          const workletNode = new AudioWorkletNode(captureCtx, 'pcm16k-capture-processor');
          workletNodeRef.current = workletNode;
          workletNode.port.onmessage = (event: MessageEvent<{ pcmBuffer?: ArrayBuffer; pcm16Buffer?: ArrayBuffer }>) => {
            const buf = event.data.pcmBuffer || event.data.pcm16Buffer;
            if (buf) handleAudioChunk(buf);
          };
          source.connect(workletNode);
          workletNode.connect(captureCtx.destination);
        } catch {
          const processor = captureCtx.createScriptProcessor(2048, 1, 1);
          processor.onaudioprocess = (e) => {
            const floatSamples = e.inputBuffer.getChannelData(0);
            const pcm16 = new Int16Array(floatSamples.length);
            for (let i = 0; i < floatSamples.length; i++) {
              const s = Math.max(-1, Math.min(1, floatSamples[i]));
              pcm16[i] = s < 0 ? s * 0x8000 : s * 0x7fff;
            }
            handleAudioChunk(pcm16.buffer);
          };
          source.connect(processor);
          processor.connect(captureCtx.destination);
        }

        setPhase('listening');
      } catch (err: any) {
        setErrorBanner(
          err?.message ||
            'Không thể kết nối 3 SignalR Hubs hoặc Google AI Studio. Hãy kiểm tra API Key và Backend (.NET) đang chạy ở cổng 5099.'
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
