import { useCallback, useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { getAccessToken, resolveBackendBaseUrl } from '@/shared/api/httpClient';
import type { CefrLevel, ConversationModeCode, TeacherVoiceName } from '@/shared/api/openapi.types';
import {
  buildClientGrammarCorrectionFallback,
  normalizeGrammarCorrection,
  sanitizeLiveTranscriptText,
  type BilingualTurnItem,
  type GrammarCorrectionItem,
  type SessionPhase,
  type ThreeHubStates,
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
  const [isAnalyzingGrammar, setIsAnalyzingGrammar] = useState<boolean>(false);
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
  const activeCefrRef = useRef<CefrLevel>('B1');
  const activeApiKeyRef = useRef<string>('');
  const isAiSpeakingRef = useRef<boolean>(false);

  // Track received corrections and pending auto-analysis timers per user turn
  const receivedCorrectionTurnsRef = useRef<Set<string>>(new Set());
  const grammarTimersByTurnRef = useRef<Map<string, ReturnType<typeof setTimeout>>>(new Map());
  const latestUserTextByTurnRef = useRef<Map<string, string>>(new Map());

  const clearGrammarTimers = useCallback(() => {
    for (const timer of grammarTimersByTurnRef.current.values()) {
      clearTimeout(timer);
    }
    grammarTimersByTurnRef.current.clear();
  }, []);

  const upsertCorrectionItem = useCallback((item: GrammarCorrectionItem) => {
    receivedCorrectionTurnsRef.current.add(item.turnId);
    const existingTimer = grammarTimersByTurnRef.current.get(item.turnId);
    if (existingTimer) {
      clearTimeout(existingTimer);
      grammarTimersByTurnRef.current.delete(item.turnId);
    }
    setIsAnalyzingGrammar(false);
    setCorrections((prev) => {
      const filtered = prev.filter((c) => c.turnId !== item.turnId);
      return [item, ...filtered];
    });
  }, []);

  /**
   * Direct Gemini REST API grammar & vocabulary analysis fallback if the SignalR Hub
   * roundtrip does not return within timeout, falling back to deterministic rule analysis.
   */
  const analyzeWithGeminiClientFallback = useCallback(
    async (turnId: string, transcriptText: string, cefrLevel: CefrLevel, apiKey: string) => {
      const cleaned = sanitizeLiveTranscriptText(transcriptText);
      if (!cleaned || cleaned.split(/\s+/).length < 2) return;

      if (apiKey && apiKey.trim()) {
        const modelsToTry = ['gemini-2.5-flash', 'gemini-2.0-flash'];
        for (const model of modelsToTry) {
          try {
            const url = `https://generativelanguage.googleapis.com/v1beta/models/${model}:generateContent?key=${encodeURIComponent(
              apiKey.trim()
            )}`;
            const prompt = [
              `You are an expert English speaking coach evaluating a spoken sentence from a Vietnamese learner (CEFR Level: ${cefrLevel}).`,
              `Learner utterance: "${cleaned}"`,
              '',
              'Analyze grammar, vocabulary, collocations, plural/article usage, tense, and natural phrasing.',
              'Even if the sentence is grammatically acceptable, provide a natural speaking upgrade and vocabulary tip.',
              'Return ONLY a valid JSON object (no markdown fences):',
              '{',
              '  "originalText": "exact learner sentence",',
              '  "correctedText": "corrected and natural English sentence",',
              '  "naturalAlternative": "an idiomatic or higher-CEFR alternative way to say it",',
              '  "errorCategory": "Grammar | Vocabulary & Collocation | Plural & Article | Speaking Upgrade",',
              '  "explanationVi": "Giải thích chi tiết, ngắn gọn, dễ hiểu bằng tiếng Việt",',
              `  "cefrTip": "[${cefrLevel}] Mẹo luyện nói hoặc từ vựng nâng cấp bằng tiếng Việt"`,
              '}',
            ].join('\n');

            const resp = await fetch(url, {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify({
                contents: [{ role: 'user', parts: [{ text: prompt }] }],
                generationConfig: { temperature: 0.15 },
              }),
            });

            if (resp.ok) {
              const data = await resp.json();
              const rawText: string | undefined =
                data?.candidates?.[0]?.content?.parts?.[0]?.text;
              if (rawText) {
                const startBrace = rawText.indexOf('{');
                const endBrace = rawText.lastIndexOf('}');
                if (startBrace >= 0 && endBrace > startBrace) {
                  const parsed = JSON.parse(rawText.slice(startBrace, endBrace + 1));
                  const normalized = normalizeGrammarCorrection({
                    ...parsed,
                    turnId,
                    timestamp: Date.now(),
                  });
                  if (normalized) {
                    upsertCorrectionItem(normalized);
                    return;
                  }
                }
              }
            }
          } catch {
            // Fall through to next model or local fallback
          }
        }
      }

      const fallbackItem = buildClientGrammarCorrectionFallback(turnId, cleaned, cefrLevel);
      if (fallbackItem) {
        upsertCorrectionItem(fallbackItem);
      }
    },
    [upsertCorrectionItem]
  );

  /**
   * Triggers Grammar & Vocabulary analysis for a specific turn via TutorConversationHub
   * ('AnalyzeTurnGrammar'), with automatic client-side fallback if SignalR does not emit
   * 'ReceiveLanguageCorrection' within 1.8s.
   */
  const analyzeTurnGrammar = useCallback(
    async (turnId: string, transcriptText: string, forceReanalyze = true) => {
      const cleaned = sanitizeLiveTranscriptText(transcriptText);
      if (!cleaned || cleaned.split(/\s+/).length < 2) return;

      if (!forceReanalyze && receivedCorrectionTurnsRef.current.has(turnId)) {
        return;
      }

      setIsAnalyzingGrammar(true);
      const convId = sessionIdRef.current || 'manual-session';
      const cefr = activeCefrRef.current;
      const apiKey = activeApiKeyRef.current;

      const hub = tutorConnRef.current;
      if (hub && hub.state === signalR.HubConnectionState.Connected) {
        try {
          await hub.invoke('AnalyzeTurnGrammar', convId, turnId, cleaned, cefr);
          // Wait briefly to see if Backend broadcasts ReceiveLanguageCorrection
          setTimeout(() => {
            if (!receivedCorrectionTurnsRef.current.has(turnId)) {
              void analyzeWithGeminiClientFallback(turnId, cleaned, cefr, apiKey);
            } else {
              setIsAnalyzingGrammar(false);
            }
          }, 1600);
          return;
        } catch {
          // Fall through to client fallback if Hub invoke fails
        }
      }

      await analyzeWithGeminiClientFallback(turnId, cleaned, cefr, apiKey);
    },
    [analyzeWithGeminiClientFallback]
  );

  const scheduleAutoGrammarAnalysisForUserTurn = useCallback(
    (turnId: string, fullTranscriptText: string, isFinal: boolean) => {
      const cleaned = sanitizeLiveTranscriptText(fullTranscriptText);
      if (!cleaned || cleaned.split(/\s+/).length < 2) return;

      latestUserTextByTurnRef.current.set(turnId, cleaned);

      const prevTimer = grammarTimersByTurnRef.current.get(turnId);
      if (prevTimer) {
        clearTimeout(prevTimer);
      }

      const delayMs = isFinal ? 450 : 1250;
      const timer = setTimeout(() => {
        grammarTimersByTurnRef.current.delete(turnId);
        const latestText = latestUserTextByTurnRef.current.get(turnId) || cleaned;
        if (!receivedCorrectionTurnsRef.current.has(turnId)) {
          void analyzeTurnGrammar(turnId, latestText, false);
        }
      }, delayMs);

      grammarTimersByTurnRef.current.set(turnId, timer);
    },
    [analyzeTurnGrammar]
  );

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
    (turnId: string, speaker: 'user' | 'ai', rawEnglishText: string, isFinal: boolean) => {
      const englishText =
        speaker === 'user' ? sanitizeLiveTranscriptText(rawEnglishText) : rawEnglishText.trim();
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
    clearGrammarTimers();
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
    setIsAnalyzingGrammar(false);
    setMicLevel(0);
    setAiAudioLevel(0);
    setPhase('idle');
  }, [clearGrammarTimers, flushPlaybackQueue]);

  const startSession = useCallback(
    async (params: StartLiveSessionParams) => {
      if (!params.apiKey || !params.apiKey.trim()) {
        setErrorBanner(
          'Vui lòng nhập Google AI Studio API Key (AIzaSy...) ở cột Thiết lập trước khi bắt đầu.'
        );
        setPhase('error');
        return;
      }

      await stopSession();
      setErrorBanner(null);
      setPhase('connecting');

      activeCefrRef.current = params.cefrLevel;
      activeApiKeyRef.current = params.apiKey.trim();

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

      tutorHub.on(
        'ReceiveTutorAudio',
        (dto: { turnId: string; base64Pcm24k: string; interactionStatus?: string }) => {
          if (dto?.base64Pcm24k) {
            scheduleAiPcm24kChunk(dto.base64Pcm24k);
          }
        }
      );

      // Normalize Backend LanguageCorrectionDto (originalText, correctedText, errorCategory, explanationVi, cefrTip)
      tutorHub.on('ReceiveLanguageCorrection', (payload: unknown) => {
        const normalized = normalizeGrammarCorrection(payload);
        if (normalized) {
          upsertCorrectionItem(normalized);
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
          if (dto?.turnId && dto.fullTranscriptText) {
            upsertTurnLine1(dto.turnId, dto.speaker, dto.fullTranscriptText, dto.isFinal);
            if (dto.speaker === 'user') {
              scheduleAutoGrammarAnalysisForUserTurn(
                dto.turnId,
                dto.fullTranscriptText,
                Boolean(dto.isFinal)
              );
            }
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

        playbackCtxRef.current = new AudioContext({ sampleRate: 24000 });
        nextPlayTimeRef.current = playbackCtxRef.current.currentTime;

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
          workletNode.port.onmessage = (
            event: MessageEvent<{ pcmBuffer?: ArrayBuffer; pcm16Buffer?: ArrayBuffer }>
          ) => {
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
    [
      flushPlaybackQueue,
      scheduleAiPcm24kChunk,
      scheduleAutoGrammarAnalysisForUserTurn,
      stopSession,
      upsertCorrectionItem,
      upsertTurnLine1,
      upsertTurnLine2,
    ]
  );

  const loadPersistedSessionData = useCallback(
    (loadedTurns: BilingualTurnItem[], loadedCorrections: GrammarCorrectionItem[]) => {
      setTurns(loadedTurns);
      setCorrections(loadedCorrections);
      receivedCorrectionTurnsRef.current = new Set(loadedCorrections.map((c) => c.turnId));
    },
    []
  );

  const resetConversationWorkspace = useCallback(async () => {
    await stopSession();
    setTurns([]);
    setCorrections([]);
    setErrorBanner(null);
    setIsAnalyzingGrammar(false);
    receivedCorrectionTurnsRef.current.clear();
    latestUserTextByTurnRef.current.clear();
  }, [stopSession]);

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
    isAnalyzingGrammar,
    errorBanner,
    micLevel,
    aiAudioLevel,
    startSession,
    stopSession,
    analyzeTurnGrammar,
    loadPersistedSessionData,
    resetConversationWorkspace,
    clearError: () => setErrorBanner(null),
  };
}
