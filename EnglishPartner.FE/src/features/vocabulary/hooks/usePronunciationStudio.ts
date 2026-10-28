import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { isAxiosError } from 'axios';
import { useTutorSessionStore } from '@/features/tutor';
import {
  useAssessPronunciationMutation,
  useSynthesizePronunciationMutation,
} from './useDailyVocabulary';
import type {
  PronunciationAssessmentResultDto,
  VocabularyFlashcardDto,
} from '../types/vocabulary.types';
import type { SynthesizePronunciationAudioResponseDto } from '@/shared/api/openapi.types';
import { toast } from '@/shared/lib/toast';

function blobToBase64(blob: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onloadend = () => {
      const res = reader.result as string;
      const commaIdx = res.indexOf(',');
      resolve(commaIdx >= 0 ? res.slice(commaIdx + 1) : res);
    };
    reader.onerror = reject;
    reader.readAsDataURL(blob);
  });
}

const isGuid = (val?: string | null): boolean =>
  Boolean(val && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(val.trim()));

export interface UsePronunciationStudioOptions {
  currentCard: VocabularyFlashcardDto | null;
  onSetApiKeyWarning: (msg: string | null) => void;
}

export function usePronunciationStudio({
  currentCard,
  onSetApiKeyWarning,
}: UsePronunciationStudioOptions) {
  const { apiKey, requestFocusLeftMenuApiKey } = useTutorSessionStore();

  const ttsModelCode = useTutorSessionStore((s) => s.ttsModelCode);
  const setTtsModelCode = useTutorSessionStore((s) => s.setTtsModelCode);
  const ttsVoiceName = useTutorSessionStore((s) => s.voiceName);
  const setTtsVoiceName = useTutorSessionStore((s) => s.setVoiceName);
  const ttsAccent = useTutorSessionStore((s) => s.ttsAccent);
  const setTtsAccent = useTutorSessionStore((s) => s.setTtsAccent);
  const ttsSpeed = useTutorSessionStore((s) => s.speechSpeed);
  const setTtsSpeed = useTutorSessionStore((s) => s.setSpeechSpeed);
  const aiProvider = useTutorSessionStore((s) => s.aiProvider);
  const setAiProvider = useTutorSessionStore((s) => s.setAiProvider);
  const agyModelCode = useTutorSessionStore((s) => s.agyModelCode);
  const assessmentModelCode = useTutorSessionStore((s) => s.assessmentModelCode);

  const effectiveAssessmentModel = useMemo(
    () => (aiProvider === 'agy' ? agyModelCode : assessmentModelCode || 'gemini-3.8-flash'),
    [aiProvider, agyModelCode, assessmentModelCode]
  );

  const synthesizeMutation = useSynthesizePronunciationMutation();
  const assessMutation = useAssessPronunciationMutation();

  const [lastSynthesizedAudio, setLastSynthesizedAudio] =
    useState<SynthesizePronunciationAudioResponseDto | null>(null);
  const [showSystemPromptUsed, setShowSystemPromptUsed] = useState(false);
  const audioPlayerRef = useRef<HTMLAudioElement | null>(null);

  // Audio Pronunciation Studio State
  const [targetType, setTargetType] = useState<'word' | 'example'>('word');
  const [selectedExampleIndex, setSelectedExampleIndex] = useState<number>(0);
  const [isRecording, setIsRecording] = useState(false);
  const [assessmentResult, setAssessmentResult] =
    useState<PronunciationAssessmentResultDto | null>(null);
  const [assessmentError, setAssessmentError] = useState<string | null>(null);

  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const mediaStreamRef = useRef<MediaStream | null>(null);
  const discardRecordingRef = useRef<boolean>(false);
  const chunksRef = useRef<BlobPart[]>([]);
  const recordStartRef = useRef<number>(0);
  const activeCardVocabIdRef = useRef<string | null>(null);

  // Request & Session cancellation tokens to prevent stale responses (e.g. A -> B -> A)
  const sessionTokenRef = useRef<number>(0);
  const ttsRequestIdRef = useRef<number>(0);
  const lastCardKeyRef = useRef<string | null>(null);
  const lastExampleIdRef = useRef<string | null>(null);

  const examples = useMemo(() => currentCard?.examples ?? [], [currentCard]);
  const selectedExample = useMemo(
    () => examples[selectedExampleIndex] ?? null,
    [examples, selectedExampleIndex]
  );
  const hasExamples = useMemo(() => examples.length > 0, [examples]);

  useEffect(() => {
    activeCardVocabIdRef.current = currentCard?.vocabularyId ?? null;
  }, [currentCard?.vocabularyId]);

  /**
   * Reset selected example index on card/sense change.
   * If same card/sense is refetched, preserves example by ID if it still exists.
   */
  useEffect(() => {
    const cardKey = currentCard ? `${currentCard.vocabularyId}_${currentCard.senseId}` : null;
    if (!cardKey) {
      setSelectedExampleIndex(0);
      lastCardKeyRef.current = null;
      lastExampleIdRef.current = null;
      return;
    }

    if (lastCardKeyRef.current !== cardKey) {
      lastCardKeyRef.current = cardKey;
      setSelectedExampleIndex(0);
      lastExampleIdRef.current = examples[0]?.id ?? null;
    } else {
      if (lastExampleIdRef.current) {
        const foundIdx = examples.findIndex((ex) => ex.id === lastExampleIdRef.current);
        if (foundIdx >= 0) {
          setSelectedExampleIndex(foundIdx);
        } else {
          setSelectedExampleIndex(0);
          lastExampleIdRef.current = examples[0]?.id ?? null;
        }
      } else {
        setSelectedExampleIndex(0);
        lastExampleIdRef.current = examples[0]?.id ?? null;
      }
    }
  }, [currentCard?.vocabularyId, currentCard?.senseId, examples]);

  /**
   * Helper to stop any ongoing audio playback immediately.
   */
  const stopAllAudioPlayback = useCallback(() => {
    if (audioPlayerRef.current) {
      audioPlayerRef.current.pause();
      audioPlayerRef.current.currentTime = 0;
    }
    if (typeof window !== 'undefined' && 'speechSynthesis' in window) {
      window.speechSynthesis.cancel();
    }
  }, []);

  /**
   * Safely abort any ongoing microphone recording, cancel pending async session tokens,
   * stop audio playback, and reset pronunciation state when switching targets or unmounting.
   */
  const resetPronunciationAndStopRecordingSafely = useCallback(() => {
    sessionTokenRef.current += 1;
    ttsRequestIdRef.current += 1;
    discardRecordingRef.current = true;
    if (mediaRecorderRef.current && mediaRecorderRef.current.state !== 'inactive') {
      try {
        mediaRecorderRef.current.stop();
      } catch {
        // ignore
      }
    }
    mediaRecorderRef.current = null;
    if (mediaStreamRef.current) {
      mediaStreamRef.current.getTracks().forEach((t) => t.stop());
      mediaStreamRef.current = null;
    }
    stopAllAudioPlayback();
    setIsRecording(false);
    setAssessmentResult(null);
    setAssessmentError(null);
  }, [stopAllAudioPlayback]);

  useEffect(() => {
    return () => {
      resetPronunciationAndStopRecordingSafely();
    };
  }, [resetPronunciationAndStopRecordingSafely]);

  const handleSelectExampleIndex = useCallback(
    (nextIdx: number) => {
      resetPronunciationAndStopRecordingSafely();
      setSelectedExampleIndex(nextIdx);
      lastExampleIdRef.current = examples[nextIdx]?.id ?? null;
    },
    [examples, resetPronunciationAndStopRecordingSafely]
  );

  const handleSelectTargetType = useCallback(
    (type: 'word' | 'example') => {
      if (targetType !== type) {
        resetPronunciationAndStopRecordingSafely();
        setTargetType(type);
      }
    },
    [targetType, resetPronunciationAndStopRecordingSafely]
  );

  const handleSynthesizeTeacherSpeech = useCallback(
    async (
      textToSpeak: string,
      kind: 'word' | 'sentence',
      overrideAccent?: 'US' | 'UK',
      targetId?: string,
      vocabularyId?: string,
      senseId?: string
    ) => {
      if (!textToSpeak.trim()) return;
      const currentTtsRequestId = ++ttsRequestIdRef.current;
      const currentSessionToken = sessionTokenRef.current;
      const requestedCardVocabId = currentCard?.vocabularyId;
      const effectiveAccent = overrideAccent ?? ttsAccent;
      setTtsAccent(effectiveAccent);
      onSetApiKeyWarning(null);

      const validTargetId = isGuid(targetId) ? targetId?.trim() : undefined;
      const validVocabularyId = isGuid(vocabularyId) ? vocabularyId?.trim() : undefined;
      const validSenseId = isGuid(senseId) ? senseId?.trim() : undefined;

      try {
        const res = await synthesizeMutation.mutateAsync({
          apiKey: apiKey.trim(),
          text: textToSpeak.trim(),
          targetType: kind,
          accent: effectiveAccent,
          voiceName: ttsVoiceName,
          speechSpeed: ttsSpeed,
          modelCode: ttsModelCode,
          targetId: validTargetId,
          vocabularyId: validVocabularyId,
          senseId: validSenseId,
        });

        // Discard response if target changed during synthesis (e.g. card switch, example switch, A -> B -> A)
        if (
          ttsRequestIdRef.current !== currentTtsRequestId ||
          sessionTokenRef.current !== currentSessionToken ||
          activeCardVocabIdRef.current !== requestedCardVocabId
        ) {
          return;
        }

        setLastSynthesizedAudio(res);
        const dataUri = `data:${res.mimeType || 'audio/wav'};base64,${res.audioBase64}`;
        if (!audioPlayerRef.current && typeof Audio !== 'undefined') {
          audioPlayerRef.current = new Audio();
        }
        if (audioPlayerRef.current) {
          audioPlayerRef.current.src = dataUri;
          void audioPlayerRef.current.play();
        } else {
          const audio = new Audio(dataUri);
          void audio.play();
        }
      } catch (err: any) {
        if (
          ttsRequestIdRef.current !== currentTtsRequestId ||
          sessionTokenRef.current !== currentSessionToken ||
          activeCardVocabIdRef.current !== requestedCardVocabId
        ) {
          return;
        }

        if ('speechSynthesis' in window) {
          window.speechSynthesis.cancel();
          const u = new SpeechSynthesisUtterance(textToSpeak);
          u.lang = effectiveAccent === 'UK' ? 'en-GB' : 'en-US';
          u.rate = ttsSpeed;
          window.speechSynthesis.speak(u);
        }

        const backendError = err?.response?.data?.error || err?.message || '';
        const isApiKeyMissingError =
          !apiKey.trim() || backendError.toLowerCase().includes('api key');

        if (isApiKeyMissingError) {
          onSetApiKeyWarning(
            'Vui lòng nhập Google AI Studio API Key ở Left Menu bên trái để tạo Audio phát âm chuẩn bằng Gemini 3.8 Live (Hệ thống đang phát tạm bằng giọng đọc của trình duyệt vì chưa có bản ghi âm lưu sẵn và thiếu API Key).'
          );
          requestFocusLeftMenuApiKey();
        } else {
          onSetApiKeyWarning(
            backendError ||
              'Backend chưa kết nối hoặc phát sinh lỗi — hệ thống đã phát tạm bằng giọng đọc trình duyệt.'
          );
        }
      }
    },
    [
      apiKey,
      currentCard?.vocabularyId,
      onSetApiKeyWarning,
      requestFocusLeftMenuApiKey,
      setTtsAccent,
      synthesizeMutation,
      ttsAccent,
      ttsModelCode,
      ttsSpeed,
      ttsVoiceName,
    ]
  );

  const startRecording = useCallback(async () => {
    if (!currentCard || assessMutation.isPending) return;

    if (targetType === 'example' && (!selectedExample || !hasExamples)) {
      setAssessmentError('Từ này chưa có câu ví dụ để chấm phát âm.');
      return;
    }

    if (aiProvider === 'agy') {
      setAiProvider('gemini');
      toast.info('Đã tự động chuyển sang cổng Google Gemini API (Cloud) để chấm phát âm Audio.');
    }

    if (!apiKey.trim()) {
      onSetApiKeyWarning(
        'Vui lòng nhập Google AI Studio API Key ở Left Menu bên trái để chấm điểm phát âm bằng AI.'
      );
      requestFocusLeftMenuApiKey();
      return;
    }
    onSetApiKeyWarning(null);
    setAssessmentResult(null);
    setAssessmentError(null);
    discardRecordingRef.current = false;

    // Snapshot target parameters with a unique session token
    const currentSessionToken = ++sessionTokenRef.current;
    const capturedCard = currentCard;
    const capturedTargetType = targetType;
    const capturedExample = selectedExample;
    const capturedAccent = ttsAccent;
    const capturedModelCode = effectiveAssessmentModel;

    const referenceText =
      capturedTargetType === 'example'
        ? capturedExample?.sentenceEn ?? ''
        : capturedCard.word;

    if (!referenceText.trim()) {
      setAssessmentError('Không có nội dung mẫu hợp lệ để chấm phát âm.');
      return;
    }

    let stream: MediaStream;
    try {
      stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    } catch {
      if (sessionTokenRef.current === currentSessionToken) {
        setAssessmentError('Không thể truy cập microphone. Vui lòng cấp quyền micro cho trình duyệt.');
        setIsRecording(false);
      }
      return;
    }

    // If target changed while waiting for userMedia permission, release stream immediately
    if (sessionTokenRef.current !== currentSessionToken) {
      stream.getTracks().forEach((t) => t.stop());
      return;
    }

    mediaStreamRef.current = stream;
    const recorder = new MediaRecorder(stream);
    mediaRecorderRef.current = recorder;
    chunksRef.current = [];
    recordStartRef.current = Date.now();

    recorder.ondataavailable = (e) => {
      if (e.data.size > 0) chunksRef.current.push(e.data);
    };

    recorder.onstop = async () => {
      stream.getTracks().forEach((t) => t.stop());
      mediaStreamRef.current = null;

      if (discardRecordingRef.current || sessionTokenRef.current !== currentSessionToken) {
        return;
      }

      const durationMs = Math.max(800, Date.now() - recordStartRef.current);
      const blob = new Blob(chunksRef.current, { type: recorder.mimeType || 'audio/webm' });
      const audioBase64 = await blobToBase64(blob);

      try {
        const res = await assessMutation.mutateAsync({
          apiKey: apiKey.trim(),
          vocabularyId: capturedCard.vocabularyId,
          senseId: capturedCard.senseId,
          exampleId: capturedTargetType === 'example' ? capturedExample?.id : undefined,
          referenceText,
          audioBase64,
          mimeType: blob.type || 'audio/webm',
          durationMs,
          modelCode: capturedModelCode,
          accent: capturedAccent,
        });
        if (sessionTokenRef.current === currentSessionToken) {
          setAssessmentResult(res);
        }
      } catch (error: unknown) {
        if (sessionTokenRef.current === currentSessionToken) {
          setAssessmentResult(null);
          const message = isAxiosError<{ error?: string }>(error)
            ? error.response?.data?.error || error.message
            : error instanceof Error
            ? error.message
            : 'Không thể chấm phát âm. Vui lòng thử lại.';
          setAssessmentError(message);
        }
      }
    };

    recorder.start();
    setIsRecording(true);
  }, [
    apiKey,
    assessMutation,
    currentCard,
    effectiveAssessmentModel,
    hasExamples,
    onSetApiKeyWarning,
    requestFocusLeftMenuApiKey,
    selectedExample,
    setAiProvider,
    aiProvider,
    targetType,
    ttsAccent,
  ]);

  const stopRecording = useCallback(() => {
    if (mediaRecorderRef.current && mediaRecorderRef.current.state !== 'inactive') {
      mediaRecorderRef.current.stop();
    }
    setIsRecording(false);
  }, []);

  const handleClearAssessmentError = useCallback(() => {
    setAssessmentError(null);
  }, []);

  const handleToggleSystemPromptUsed = useCallback(() => {
    setShowSystemPromptUsed((s) => !s);
  }, []);

  return {
    // TTS configuration
    ttsModelCode,
    setTtsModelCode,
    ttsVoiceName,
    setTtsVoiceName,
    ttsAccent,
    setTtsAccent,
    ttsSpeed,
    setTtsSpeed,
    aiProvider,
    setAiProvider,
    effectiveAssessmentModel,
    lastSynthesizedAudio,
    showSystemPromptUsed,
    handleToggleSystemPromptUsed,

    // Studio selection
    targetType,
    handleSelectTargetType,
    examples,
    selectedExample,
    selectedExampleIndex,
    hasExamples,
    handleSelectExampleIndex,

    // Recording and Assessment
    isRecording,
    startRecording,
    stopRecording,
    assessmentResult,
    assessmentError,
    handleClearAssessmentError,
    isAssessing: assessMutation.isPending,
    isSynthesizing: synthesizeMutation.isPending,

    // Speech synthesis & teardown
    handleSynthesizeTeacherSpeech,
    resetPronunciationAndStopRecordingSafely,
    stopAllAudioPlayback,
  };
}
