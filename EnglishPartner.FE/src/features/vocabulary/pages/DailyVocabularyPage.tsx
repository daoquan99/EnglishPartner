import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { isAxiosError } from 'axios';
import {
  BookOpen,
  Volume2,
  RotateCw,
  Sparkles,
  Mic,
  Square,
  ChevronLeft,
  ChevronRight,
  Cpu,
  ShieldCheck,
  Award,
  AlertCircle,
  Loader2,
  Code2,
  Settings,
  Copy,
  Check,
  FileText,
  History,
} from 'lucide-react';
import { PronunciationHistoryDialog } from '../components/PronunciationHistoryDialog';
import {
  useActiveScoringRubricQuery,
  usePronunciationModelsQuery,
  useSpeechSpeedsQuery,
  useTeacherVoicesQuery,
  useTopicsQuery,
  useVocabularyGenerationModelsQuery,
} from '@/features/catalog';
import { useTutorSessionStore } from '@/features/tutor';
import {
  useAssessPronunciationMutation,
  useBuildVocabularyGenerationPromptMutation,
  useDailyVocabularyPlanQuery,
  useGenerateVocabularyMutation,
  useImportVocabularyJsonMutation,
  useSubmitVocabularyReviewMutation,
  useSynthesizePronunciationMutation,
} from '../hooks/useDailyVocabulary';
import type {
  PronunciationAssessmentResultDto,
  ReviewRating,
  VocabularyFlashcardDto,
} from '../types/vocabulary.types';
import type {
  CefrLevel,
  SynthesizePronunciationAudioResponseDto,
  TeacherVoiceName,
} from '@/shared/api/openapi.types';
import { TopicCreatablePicker } from '@/shared/components/ui/TopicCreatablePicker';
import { Dialog } from '@/shared/components/ui/Dialog';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { EmptyState } from '@/shared/components/ui/EmptyState';
import { cn } from '@/shared/lib/cn';

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

export const DailyVocabularyPage: React.FC = () => {
  const { t, i18n } = useTranslation();
  const { apiKey, requestFocusLeftMenuApiKey } = useTutorSessionStore();

  const { data: plan, isLoading } = useDailyVocabularyPlanQuery(10);
  const { data: vocabModels = [] } = useVocabularyGenerationModelsQuery();
  const { data: audioModels = [] } = usePronunciationModelsQuery();
  const { data: allVoices = [] } = useTeacherVoicesQuery(true, false);
  const { data: speechSpeeds = [] } = useSpeechSpeedsQuery();
  const { data: vocabTopics = [] } = useTopicsQuery({
    onlyActive: true,
    supportsVocabulary: true,
  });
  const { data: activeRubric } = useActiveScoringRubricQuery();

  const reviewMutation = useSubmitVocabularyReviewMutation();
  const generateMutation = useGenerateVocabularyMutation();
  const buildPromptMutation = useBuildVocabularyGenerationPromptMutation();
  const importJsonMutation = useImportVocabularyJsonMutation();
  const assessMutation = useAssessPronunciationMutation();
  const synthesizeMutation = useSynthesizePronunciationMutation();

  const [cards, setCards] = useState<VocabularyFlashcardDto[]>([]);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [isFlipped, setIsFlipped] = useState(false);
  const [apiKeyWarning, setApiKeyWarning] = useState<string | null>(null);

  // Dialog Open States (Toolbar Modals)
  const [isTtsSettingsModalOpen, setIsTtsSettingsModalOpen] = useState(false);
  const [isAiGeneratorModalOpen, setIsAiGeneratorModalOpen] = useState(false);
  const [isHistoryModalOpen, setIsHistoryModalOpen] = useState(false);

  // Teacher AI Pronunciation Synthesizer State (Gemini 3.8 Live / Extended Thinking + 30 Voices + US/UK Accent)
  const [ttsModelCode, setTtsModelCode] = useState<
    'gemini-3.8-live' | 'gemini-3.8-live-extended-thinking'
  >('gemini-3.8-live');
  const [ttsVoiceName, setTtsVoiceName] = useState<TeacherVoiceName>('Aoede');
  const [ttsAccent, setTtsAccent] = useState<'US' | 'UK'>('US');
  const [ttsSpeed, setTtsSpeed] = useState<number>(1.0);
  const [lastSynthesizedAudio, setLastSynthesizedAudio] =
    useState<SynthesizePronunciationAudioResponseDto | null>(null);
  const [showSystemPromptUsed, setShowSystemPromptUsed] = useState(false);
  const audioPlayerRef = useRef<HTMLAudioElement | null>(null);

  // AI Vocabulary Generator Modal State
  const [genMode, setGenMode] = useState<'direct' | 'manual'>('direct');
  const [genCefr, setGenCefr] = useState<CefrLevel>('B2');
  const [genTopic, setGenTopic] = useState<string>(
    'Essential Workplace Phrasal Verbs & Executive Idioms'
  );
  const [genCount, setGenCount] = useState<number>(3);
  const [selectedVocabModel, setSelectedVocabModel] = useState<string>('gemini-3.8-flash');
  const [manualPrompt, setManualPrompt] = useState<string>('');
  const [isPromptCopied, setIsPromptCopied] = useState(false);
  const [manualJsonInput, setManualJsonInput] = useState<string>('');
  const [assignToTodayPlan, setAssignToTodayPlan] = useState(true);
  const [manualError, setManualError] = useState<string | null>(null);

  // Audio Pronunciation Studio State (Right Column 45%)
  const [selectedAudioModel, setSelectedAudioModel] = useState<string>('gemini-3.8-flash');
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

  useEffect(() => {
    if (plan?.items && plan.items.length > 0) {
      setCards(plan.items);
    }
  }, [plan]);

  useEffect(() => {
    if (vocabModels.length > 0 && !vocabModels.some((m) => m.modelCode === selectedVocabModel)) {
      setSelectedVocabModel(vocabModels[0].modelCode);
    }
  }, [vocabModels, selectedVocabModel]);

  useEffect(() => {
    if (audioModels.length > 0 && !audioModels.some((m) => m.modelCode === selectedAudioModel)) {
      setSelectedAudioModel(audioModels[0].modelCode);
    }
  }, [audioModels, selectedAudioModel]);

  const currentCard = cards[currentIndex] ?? null;
  const examples = currentCard?.examples ?? [];
  const selectedExample = examples[selectedExampleIndex] ?? null;
  const hasExamples = examples.length > 0;

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
  const stopAllAudioPlayback = () => {
    if (audioPlayerRef.current) {
      audioPlayerRef.current.pause();
      audioPlayerRef.current.currentTime = 0;
    }
    if (typeof window !== 'undefined' && 'speechSynthesis' in window) {
      window.speechSynthesis.cancel();
    }
  };

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
  }, []);

  useEffect(() => {
    return () => {
      resetPronunciationAndStopRecordingSafely();
    };
  }, [resetPronunciationAndStopRecordingSafely]);

  const switchCardSafely = (nextIdx: number) => {
    resetPronunciationAndStopRecordingSafely();
    setIsFlipped(false);
    setCurrentIndex(nextIdx);
  };

  const handleSelectExampleIndex = (nextIdx: number) => {
    resetPronunciationAndStopRecordingSafely();
    setSelectedExampleIndex(nextIdx);
    lastExampleIdRef.current = examples[nextIdx]?.id ?? null;
  };

  /**
   * Calls Backend POST /api/pronunciation/synthesize using "Gemini 3.8 Live" or
   * "Gemini 3.8 Live Extended Thinking", 1 of 30 Teacher Voices, and US/UK accent.
   * If cached audio is available in the persistent cache, it plays without requiring an API Key.
   */
  const isGuid = (val?: string | null): boolean =>
    Boolean(val && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(val.trim()));

  const handleSynthesizeTeacherSpeech = async (
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
    setApiKeyWarning(null);

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
      const isApiKeyMissingError = !apiKey.trim() || backendError.toLowerCase().includes('api key');

      if (isApiKeyMissingError) {
        setApiKeyWarning(
          'Vui lòng nhập Google AI Studio API Key ở Left Menu bên trái để tạo Audio phát âm chuẩn bằng Gemini 3.8 Live (Hệ thống đang phát tạm bằng giọng đọc của trình duyệt vì chưa có bản ghi âm lưu sẵn và thiếu API Key).'
        );
        requestFocusLeftMenuApiKey();
      } else {
        setApiKeyWarning(
          backendError ||
            'Backend chưa kết nối hoặc phát sinh lỗi — hệ thống đã phát tạm bằng giọng đọc trình duyệt.'
        );
      }
    }
  };

  const handleReview = async (rating: ReviewRating) => {
    if (!currentCard) return;
    resetPronunciationAndStopRecordingSafely();

    try {
      const updated = await reviewMutation.mutateAsync({
        vocabularyId: currentCard.vocabularyId,
        senseId: currentCard.senseId,
        rating,
        responseTimeMs: 2500,
      });
      setCards((prev) =>
        prev.map((c, idx) =>
          idx === currentIndex ? { ...c, ...updated, isCompletedToday: true } : c
        )
      );
    } catch {
      setCards((prev) =>
        prev.map((c, idx) => (idx === currentIndex ? { ...c, isCompletedToday: true } : c))
      );
    }

    setIsFlipped(false);
    if (currentIndex < cards.length - 1) {
      setCurrentIndex((i) => i + 1);
    }
  };

  const handleGenerateMoreWords = async () => {
    if (!apiKey.trim()) {
      setIsAiGeneratorModalOpen(false);
      setApiKeyWarning(t('vocab.apiKeyMissingNotice'));
      requestFocusLeftMenuApiKey();
      return;
    }
    setApiKeyWarning(null);
    try {
      const generated = await generateMutation.mutateAsync({
        apiKey: apiKey.trim(),
        cefrLevel: genCefr,
        topic: genTopic || 'Software Engineering & Daily Workplace',
        count: genCount,
        modelCode: selectedVocabModel,
      });
      if (generated && generated.length > 0) {
        resetPronunciationAndStopRecordingSafely();
        setCards((prev) => [...generated, ...prev]);
        setCurrentIndex(0);
        setIsFlipped(false);
        setIsAiGeneratorModalOpen(false);
      }
    } catch (err: any) {
      setApiKeyWarning(
        err?.response?.data?.error ||
          err?.message ||
          t('vocab.genAiFailed')
      );
    }
  };

  const handleBuildPrompt = async () => {
    setManualError(null);
    try {
      const res = await buildPromptMutation.mutateAsync({
        requestedCount: genCount,
        cefrLevel: genCefr,
        topic: genTopic || 'General English',
      });
      setManualPrompt(res.prompt);
    } catch (err: any) {
      setManualError(
        err?.response?.data?.error ||
          err?.message ||
          t('vocab.buildPromptFailed')
      );
    }
  };

  const handleCopyPrompt = async () => {
    if (!manualPrompt) return;
    try {
      await navigator.clipboard.writeText(manualPrompt);
      setIsPromptCopied(true);
      setTimeout(() => setIsPromptCopied(false), 2000);
    } catch {
      // Fallback if clipboard API is not available
    }
  };

  const handleImportManualJson = async () => {
    if (!manualJsonInput.trim()) {
      setManualError(t('vocab.enterJsonNotice'));
      return;
    }
    setManualError(null);
    try {
      const imported = await importJsonMutation.mutateAsync({
        jsonText: manualJsonInput.trim(),
        cefrLevel: genCefr,
        topic: genTopic || 'General English',
        modelCode: 'manual-web-ai',
        assignToTodayPlan,
      });
      if (imported && imported.length > 0) {
        resetPronunciationAndStopRecordingSafely();
        setCards((prev) => [...imported, ...prev]);
        setCurrentIndex(0);
        setIsFlipped(false);
        setIsAiGeneratorModalOpen(false);
        setManualPrompt('');
        setManualJsonInput('');
        setManualError(null);
      }
    } catch (err: any) {
      setManualError(
        err?.response?.data?.error ||
          err?.message ||
          'Không thể nhập từ vựng từ JSON. Hãy kiểm tra định dạng JSON từ AI.'
      );
    }
  };

  const startRecording = async () => {
    if (!currentCard || assessMutation.isPending) return;

    if (targetType === 'example' && (!selectedExample || !hasExamples)) {
      setAssessmentError('Từ này chưa có câu ví dụ để chấm phát âm.');
      return;
    }

    if (!apiKey.trim()) {
      setApiKeyWarning(
        'Vui lòng nhập Google AI Studio API Key ở Left Menu bên trái để chấm điểm phát âm bằng AI.'
      );
      requestFocusLeftMenuApiKey();
      return;
    }
    setApiKeyWarning(null);
    setAssessmentResult(null);
    setAssessmentError(null);
    discardRecordingRef.current = false;

    // Snapshot target parameters with a unique session token
    const currentSessionToken = ++sessionTokenRef.current;
    const capturedCard = currentCard;
    const capturedTargetType = targetType;
    const capturedExample = selectedExample;
    const capturedAccent = ttsAccent;
    const capturedModelCode = selectedAudioModel;

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
            : error instanceof Error ? error.message : 'Không thể chấm phát âm. Vui lòng thử lại.';
          setAssessmentError(message);
        }
      }
    };

    recorder.start();
    setIsRecording(true);
  };

  const stopRecording = () => {
    if (mediaRecorderRef.current && mediaRecorderRef.current.state !== 'inactive') {
      mediaRecorderRef.current.stop();
    }
    setIsRecording(false);
  };

  if (isLoading && cards.length === 0) {
    return (
      <div className="p-8 text-center text-sm text-slate-500">
        {t('vocab.loadingPlan')}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Top Toolbar: Progress Badges + Settings Button (TTS Modal) + "Tạo từ vựng bằng AI" Button */}
      <div className="bg-white rounded-3xl border border-slate-200/90 px-5 py-4 flex flex-wrap items-center justify-between gap-4 shadow-xs">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-emerald-50 border border-emerald-200 flex items-center justify-center shrink-0">
            <img src="/assets/icons/navigation/nav-study.svg" alt="" className="w-5 h-5 object-contain" />
          </div>
          <div>
            <h1 className="text-base sm:text-lg font-extrabold text-slate-900">
              {t('vocab.pageTitle')}
            </h1>
            <div className="flex items-center gap-2 flex-wrap mt-0.5">
              <Badge variant="emerald">
                {t('vocab.completedBadge')}: {cards.filter((c) => c.isCompletedToday).length}/{cards.length} {t('vocab.wordsUnit')}
              </Badge>
              <Badge variant="sky">
                {t('vocab.cardBadge')}: {cards.length > 0 ? currentIndex + 1 : 0}/{cards.length}
              </Badge>
              <Badge variant="violet">
                {t('vocab.teacherVoiceBadge')}: {ttsVoiceName} ({ttsAccent} · {ttsSpeed.toFixed(2)}x)
              </Badge>
            </div>
          </div>
        </div>

        {/* Action Buttons opening Dialogs/Modals */}
        <div className="flex items-center gap-2.5 flex-wrap">
          <Button
            variant="outline"
            size="sm"
            onClick={() => setIsTtsSettingsModalOpen(true)}
          >
            <img src="/assets/icons/navigation/nav-settings.svg" alt="" className="w-4 h-4 object-contain" />
            {t('vocab.configTts')}
          </Button>

          <Button
            variant="primary"
            size="sm"
            onClick={() => {
              if (!apiKey.trim()) {
                setGenMode('manual');
              }
              setIsAiGeneratorModalOpen(true);
            }}
          >
            <Sparkles className="w-4 h-4" />
            {t('vocab.generateWithAi')}
          </Button>
        </div>
      </div>

      {apiKeyWarning && (
        <div className="bg-amber-50 border border-amber-300 rounded-2xl p-3.5 flex items-center justify-between gap-2 text-xs text-amber-900">
          <div className="flex items-center gap-2">
            <AlertCircle className="w-4 h-4 text-amber-600 shrink-0" />
            <span>{apiKeyWarning}</span>
          </div>
          <button
            type="button"
            onClick={() => setApiKeyWarning(null)}
            className="font-bold hover:underline cursor-pointer"
          >
            {t('common.close')}
          </button>
        </div>
      )}

      {/* Main 2-Column Layout: Left 55% (Flashcard) — Right 45% (Phòng Chấm Phát Âm Audio AI) */}
      {cards.length === 0 ? (
        <div className="bg-white rounded-3xl border border-slate-200/90 p-8 shadow-xs">
          <EmptyState
            illustration="no-plan"
            title={t('vocab.emptyPlanTitle', 'Chưa có kế hoạch từ vựng cho hôm nay')}
            description={t(
              'vocab.emptyPlanDesc',
              'Nhấn "Tạo từ vựng bằng AI" ở góc trên bên phải để hệ thống tự động tạo bộ từ vựng theo chủ đề bạn chọn!'
            )}
            action={
              <Button
                variant="primary"
                size="sm"
                onClick={() => {
                  if (!apiKey.trim()) {
                    setGenMode('manual');
                  }
                  setIsAiGeneratorModalOpen(true);
                }}
              >
                <Sparkles className="w-4 h-4" />
                {t('vocab.generateWithAi')}
              </Button>
            }
          />
        </div>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-20 gap-5 items-start">
        {/* Left Column (55% -> 11/20 cols on desktop): Sense Flashcard */}
        <div className="lg:col-span-11 space-y-4">
          {currentCard && (
            <div className="bg-white rounded-3xl border border-slate-200/90 shadow-xs p-6 space-y-5">
              {/* Card Navigation & Flip Bar */}
              <div className="flex items-center justify-between border-b border-slate-100 pb-3">
                <div className="flex items-center gap-2 flex-wrap">
                  <Badge variant="emerald">{currentCard.cefrLevel}</Badge>
                  <Badge variant="amber">{currentCard.partOfSpeech}</Badge>
                  {currentCard.topicCategory && (
                    <span className="text-xs text-slate-500 truncate max-w-xs">
                      {currentCard.topicCategory}
                    </span>
                  )}
                </div>

                <div className="flex items-center gap-1.5">
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={currentIndex === 0}
                    onClick={() => switchCardSafely(Math.max(0, currentIndex - 1))}
                  >
                    <ChevronLeft className="w-4 h-4" />
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setIsFlipped((f) => !f)}
                  >
                    <RotateCw className="w-3.5 h-3.5" />
                    {isFlipped ? t('vocab.cardFront') : t('vocab.cardBack')}
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={currentIndex >= cards.length - 1}
                    onClick={() =>
                      switchCardSafely(Math.min(cards.length - 1, currentIndex + 1))
                    }
                  >
                    <ChevronRight className="w-4 h-4" />
                  </Button>
                </div>
              </div>

              {/* Flashcard Body */}
              {!isFlipped ? (
                <div className="py-6 text-center space-y-4">
                  <h2 className="text-4xl font-extrabold text-slate-900 tracking-tight">
                    {currentCard.word}
                  </h2>

                  {/* UK / US IPA & Gemini 3.8 Live Teacher Voice TTS Buttons */}
                  <div className="flex items-center justify-center gap-3 flex-wrap">
                    {currentCard.pronunciations.map((p) => {
                      const acc = p.accent === 'UK' ? 'UK' : 'US';
                      return (
                        <button
                          key={p.id}
                          type="button"
                          disabled={synthesizeMutation.isPending}
                          onClick={() =>
                            void handleSynthesizeTeacherSpeech(
                              currentCard.word,
                              'word',
                              acc,
                              currentCard.vocabularyId,
                              currentCard.vocabularyId
                            )
                          }
                          title={`Phát âm từ "${currentCard.word}" theo ngữ điệu ${acc} bằng giọng Teacher ${ttsVoiceName} (${ttsModelCode})`}
                          className="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl bg-slate-50 hover:bg-emerald-50 border border-slate-200 hover:border-emerald-300 text-xs font-semibold text-slate-700 transition-all cursor-pointer disabled:opacity-60"
                        >
                          <span className="font-bold text-emerald-700">{acc}</span>
                          <span className="font-mono">{p.ipa}</span>
                          {synthesizeMutation.isPending && ttsAccent === acc ? (
                            <Loader2 className="w-3.5 h-3.5 text-emerald-600 animate-spin" />
                          ) : (
                            <Volume2 className="w-3.5 h-3.5 text-emerald-600" />
                          )}
                          <span className="text-[10px] text-slate-400">({ttsVoiceName})</span>
                        </button>
                      );
                    })}
                  </div>

                  <p className="text-base font-medium text-slate-700 max-w-xl mx-auto">
                    {currentCard.definitionEn}
                  </p>

                  {currentCard.examples[0] && (
                    <div className="max-w-xl mx-auto p-4 rounded-2xl bg-emerald-50/60 border border-emerald-200/80 text-left space-y-2">
                      <div className="flex items-start justify-between gap-3">
                        <p className="text-sm font-semibold text-emerald-950">
                          &ldquo;{currentCard.examples[0].sentenceEn}&rdquo;
                        </p>
                        <div className="flex items-center gap-1 shrink-0">
                          <button
                            type="button"
                            disabled={synthesizeMutation.isPending}
                            onClick={() =>
                              void handleSynthesizeTeacherSpeech(
                                currentCard.examples[0].sentenceEn,
                                'sentence',
                                'US',
                                currentCard.examples[0].id,
                                currentCard.vocabularyId,
                                currentCard.senseId
                              )
                            }
                            title={`Nghe câu mẫu với giọng Teacher ${ttsVoiceName} (US Accent)`}
                            className="px-2 py-1 text-[11px] font-bold text-emerald-800 bg-white hover:bg-emerald-100 border border-emerald-200 rounded-lg inline-flex items-center gap-1 cursor-pointer"
                          >
                            <Volume2 className="w-3.5 h-3.5" /> US
                          </button>
                          <button
                            type="button"
                            disabled={synthesizeMutation.isPending}
                            onClick={() =>
                              void handleSynthesizeTeacherSpeech(
                                currentCard.examples[0].sentenceEn,
                                'sentence',
                                'UK',
                                currentCard.examples[0].id,
                                currentCard.vocabularyId,
                                currentCard.senseId
                              )
                            }
                            title={`Nghe câu mẫu với giọng Teacher ${ttsVoiceName} (UK Accent)`}
                            className="px-2 py-1 text-[11px] font-bold text-sky-800 bg-white hover:bg-sky-100 border border-sky-200 rounded-lg inline-flex items-center gap-1 cursor-pointer"
                          >
                            <Volume2 className="w-3.5 h-3.5" /> UK
                          </button>
                        </div>
                      </div>
                    </div>
                  )}

                  <button
                    type="button"
                    onClick={() => setIsFlipped(true)}
                    className="text-xs font-bold text-emerald-700 hover:underline cursor-pointer"
                  >
                    {t('vocab.clickToSeeBack')}
                  </button>
                </div>
              ) : (
                <div className="space-y-4">
                  <div className="p-4 rounded-2xl bg-amber-50/70 border border-amber-200">
                    <div className="text-xs font-bold uppercase text-amber-800">
                      {t('vocab.vietnameseMeaning')}
                    </div>
                    <div className="text-lg font-extrabold text-slate-900 mt-0.5">
                      {currentCard.meaningVi}
                    </div>
                    <p className="text-xs text-slate-600 mt-1">{currentCard.descriptionEn}</p>
                  </div>

                  {/* Synonyms & Antonyms */}
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                    <div className="p-3 rounded-2xl bg-slate-50 border border-slate-200/80">
                      <div className="text-xs font-bold text-slate-700 mb-1">
                        {t('vocab.synonyms')}
                      </div>
                      <div className="flex flex-wrap gap-1.5">
                        {currentCard.synonyms.length > 0 ? (
                          currentCard.synonyms.map((s) => (
                            <Badge key={s} variant="emerald">
                              {s}
                            </Badge>
                          ))
                        ) : (
                          <span className="text-xs text-slate-400">{t('vocab.none')}</span>
                        )}
                      </div>
                    </div>
                    <div className="p-3 rounded-2xl bg-slate-50 border border-slate-200/80">
                      <div className="text-xs font-bold text-slate-700 mb-1">
                        {t('vocab.antonyms')}
                      </div>
                      <div className="flex flex-wrap gap-1.5">
                        {currentCard.antonyms.length > 0 ? (
                          currentCard.antonyms.map((a) => (
                            <Badge key={a} variant="rose">
                              {a}
                            </Badge>
                          ))
                        ) : (
                          <span className="text-xs text-slate-400">{t('vocab.none')}</span>
                        )}
                      </div>
                    </div>
                  </div>

                  {/* Collocations */}
                  {currentCard.collocations.length > 0 && (
                    <div className="p-3.5 rounded-2xl bg-slate-50 border border-slate-200/80 space-y-1.5">
                      <div className="text-xs font-bold text-slate-700">
                        {t('vocab.collocations')}
                      </div>
                      {currentCard.collocations.map((col) => (
                        <div key={col.id} className="text-xs text-slate-700">
                          <strong className="text-emerald-800">{col.phrase}</strong> —{' '}
                          {col.meaningVi}
                        </div>
                      ))}
                    </div>
                  )}

                  {/* Bilingual Examples with US/UK Teacher Voice Play Buttons */}
                  {currentCard.examples.map((ex) => (
                    <div
                      key={ex.id}
                      className="p-3.5 rounded-2xl bg-emerald-50/50 border border-emerald-200 text-xs space-y-1.5"
                    >
                      <div className="flex items-start justify-between gap-2">
                        <div className="font-bold text-slate-800">{ex.sentenceEn}</div>
                        <div className="flex items-center gap-1 shrink-0">
                          <button
                            type="button"
                            disabled={synthesizeMutation.isPending}
                            onClick={() =>
                              void handleSynthesizeTeacherSpeech(
                                ex.sentenceEn,
                                'sentence',
                                'US',
                                ex.id,
                                currentCard.vocabularyId,
                                currentCard.senseId
                              )
                            }
                            className="px-2 py-0.5 rounded bg-white border border-emerald-200 font-bold text-emerald-700 hover:bg-emerald-100 cursor-pointer"
                          >
                            🔊 US
                          </button>
                          <button
                            type="button"
                            disabled={synthesizeMutation.isPending}
                            onClick={() =>
                              void handleSynthesizeTeacherSpeech(
                                ex.sentenceEn,
                                'sentence',
                                'UK',
                                ex.id,
                                currentCard.vocabularyId,
                                currentCard.senseId
                              )
                            }
                            className="px-2 py-0.5 rounded bg-white border border-sky-200 font-bold text-sky-700 hover:bg-sky-100 cursor-pointer"
                          >
                            🔊 UK
                          </button>
                        </div>
                      </div>
                      <div className="text-slate-600">{ex.translationVi}</div>
                    </div>
                  ))}
                </div>
              )}

              {/* SM-2 Spaced Repetition Rating Buttons */}
              <div className="pt-4 border-t border-slate-100">
                <div className="text-xs font-bold text-slate-500 mb-2 text-center">
                  {t('vocab.sm2RatingTitle')}
                </div>
                <div className="grid grid-cols-4 gap-2">
                  {(['Again', 'Hard', 'Good', 'Easy'] as ReviewRating[]).map((rating) => (
                    <button
                      key={rating}
                      type="button"
                      onClick={() => void handleReview(rating)}
                      className={cn(
                        'py-2.5 px-3 rounded-xl font-bold text-xs border transition-all cursor-pointer',
                        rating === 'Again' &&
                          'bg-rose-50 hover:bg-rose-100 text-rose-700 border-rose-200',
                        rating === 'Hard' &&
                          'bg-amber-50 hover:bg-amber-100 text-amber-800 border-amber-200',
                        rating === 'Good' &&
                          'bg-emerald-50 hover:bg-emerald-100 text-emerald-700 border-emerald-200',
                        rating === 'Easy' &&
                          'bg-sky-50 hover:bg-sky-100 text-sky-700 border-sky-200'
                      )}
                    >
                      {rating === 'Again'
                        ? t('vocab.ratingAgain')
                        : rating === 'Hard'
                        ? t('vocab.ratingHard')
                        : rating === 'Good'
                        ? t('vocab.ratingGood')
                        : t('vocab.ratingEasy')}
                    </button>
                  ))}
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Right Column (45% -> 9/20 cols on desktop): Phòng Chấm Phát Âm Audio AI */}
        <div className="lg:col-span-9 space-y-4">
          {currentCard && (
            <div className="bg-white rounded-3xl border border-slate-200/90 shadow-xs p-6 space-y-4">
              <div className="flex flex-wrap items-center justify-between gap-2 border-b border-slate-100 pb-3">
                <div>
                  <h3 className="text-base font-extrabold text-slate-900 flex items-center gap-2">
                    <img src="/assets/icons/navigation/nav-voices.svg" alt="" className="w-5 h-5 object-contain" />
                    {t('vocab.pronunciationStudio')}
                  </h3>
                  <p className="text-xs text-slate-500">
                    {t('vocab.syncedWithCard')}{' '}
                    <strong className="text-slate-900">{currentCard.word}</strong>
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setIsHistoryModalOpen(true)}
                    title={t('vocab.viewHistory')}
                  >
                    <img src="/assets/icons/navigation/nav-history.svg" alt="" className="w-3.5 h-3.5 object-contain" />
                    {t('vocab.viewHistory')}
                  </Button>
                  <Badge variant="emerald">
                    <ShieldCheck className="w-3 h-3" /> Audio Input ({audioModels.length} Models)
                  </Badge>
                </div>
              </div>

              {/* Audio Pronunciation Model Selector + Target Selector */}
              <div className="space-y-3">
                <div>
                  <label className="block text-xs font-bold text-slate-600 mb-1">
                    {t('vocab.audioModelLabel')}
                  </label>
                  <select
                    value={selectedAudioModel}
                    onChange={(e) => setSelectedAudioModel(e.target.value)}
                    className="w-full px-3 py-2 rounded-xl border border-slate-200 bg-white text-xs font-semibold text-slate-800"
                  >
                    {audioModels.map((m) => (
                      <option key={m.modelCode} value={m.modelCode}>
                        {m.displayName} ({m.modelCode})
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-600 mb-1">
                    {t('vocab.practiceMode')}
                  </label>
                  <div className="grid grid-cols-2 gap-2">
                    <button
                      type="button"
                      onClick={() => {
                        if (targetType !== 'word') {
                          resetPronunciationAndStopRecordingSafely();
                          setTargetType('word');
                        }
                      }}
                      className={cn(
                        'py-2 rounded-xl text-xs font-bold border transition-all cursor-pointer',
                        targetType === 'word'
                          ? 'bg-emerald-600 text-white border-emerald-600 shadow-2xs'
                          : 'bg-slate-50 text-slate-600 border-slate-200 hover:bg-slate-100'
                      )}
                    >
                      {t('vocab.singleWord')} ({currentCard.word})
                    </button>
                    <button
                      type="button"
                      onClick={() => {
                        if (targetType !== 'example') {
                          resetPronunciationAndStopRecordingSafely();
                          setTargetType('example');
                        }
                      }}
                      className={cn(
                        'py-2 rounded-xl text-xs font-bold border transition-all cursor-pointer',
                        targetType === 'example'
                          ? 'bg-emerald-600 text-white border-emerald-600 shadow-2xs'
                          : 'bg-slate-50 text-slate-600 border-slate-200 hover:bg-slate-100'
                      )}
                    >
                      {t('vocab.exampleSentence')} {hasExamples ? `(${examples.length})` : ''}
                    </button>
                  </div>
                </div>
              </div>

              {/* Target Sentence Box + Listen Sample US/UK + Record Button */}
              <div className="p-4 rounded-2xl bg-slate-50 border border-slate-200 space-y-3">
                <div className="flex items-center justify-between gap-2">
                  <div className="text-xs text-slate-500 font-bold uppercase tracking-wider">
                    {t('vocab.sampleContent')}
                  </div>
                  {targetType === 'example' && examples.length > 1 && (
                    <div className="flex items-center gap-1.5">
                      <span className="text-xs font-semibold text-slate-600">
                        {t('vocab.sentenceIndex')} {selectedExampleIndex + 1}/{examples.length}
                      </span>
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        className="h-7 px-2 text-xs"
                        disabled={selectedExampleIndex === 0}
                        onClick={() => handleSelectExampleIndex(selectedExampleIndex - 1)}
                        title={t('vocab.prevSentence')}
                      >
                        <ChevronLeft className="w-3.5 h-3.5" />
                        <span className="hidden sm:inline">{t('vocab.prevSentence')}</span>
                      </Button>
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        className="h-7 px-2 text-xs"
                        disabled={selectedExampleIndex >= examples.length - 1}
                        onClick={() => handleSelectExampleIndex(selectedExampleIndex + 1)}
                        title={t('vocab.nextSentence')}
                      >
                        <span className="hidden sm:inline">{t('vocab.nextSentence')}</span>
                        <ChevronRight className="w-3.5 h-3.5" />
                      </Button>
                    </div>
                  )}
                </div>

                <div className="max-h-36 overflow-y-auto space-y-1.5 pr-1">
                  {targetType === 'word' ? (
                    <div className="text-base sm:text-lg font-extrabold text-slate-900 break-words leading-relaxed">
                      &ldquo;{currentCard.word}&rdquo;
                    </div>
                  ) : hasExamples && selectedExample ? (
                    <>
                      <div className="text-sm sm:text-base font-extrabold text-slate-900 break-words leading-relaxed">
                        &ldquo;{selectedExample.sentenceEn}&rdquo;
                      </div>
                      {selectedExample.translationVi && (
                        <div className="text-xs text-slate-500 italic break-words">
                          {selectedExample.translationVi}
                        </div>
                      )}
                    </>
                  ) : (
                    <div className="p-3 rounded-xl bg-amber-50 border border-amber-200 text-amber-800 text-xs flex items-center gap-2">
                      <AlertCircle className="w-4 h-4 shrink-0 text-amber-600" />
                      <span>{t('vocab.noExamples')}</span>
                    </div>
                  )}
                </div>

                <div className="flex flex-wrap items-center justify-between gap-2 pt-2 border-t border-slate-200/80">
                  <div className="flex items-center gap-1.5">
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={
                        synthesizeMutation.isPending || (targetType === 'example' && !hasExamples)
                      }
                      onClick={() => {
                        const isExample = targetType === 'example';
                        if (isExample && !selectedExample) return;
                        const text = isExample ? selectedExample!.sentenceEn : currentCard.word;
                        const kind = isExample ? 'sentence' : 'word';
                        const targetId = isExample
                          ? selectedExample!.id
                          : currentCard.vocabularyId;
                        const senseId = isExample ? currentCard.senseId : undefined;
                        void handleSynthesizeTeacherSpeech(
                          text,
                          kind,
                          'US',
                          targetId,
                          currentCard.vocabularyId,
                          senseId
                        );
                      }}
                    >
                      <Volume2 className="w-3.5 h-3.5 text-emerald-600" />
                      {t('vocab.sampleUs')}
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={
                        synthesizeMutation.isPending || (targetType === 'example' && !hasExamples)
                      }
                      onClick={() => {
                        const isExample = targetType === 'example';
                        if (isExample && !selectedExample) return;
                        const text = isExample ? selectedExample!.sentenceEn : currentCard.word;
                        const kind = isExample ? 'sentence' : 'word';
                        const targetId = isExample
                          ? selectedExample!.id
                          : currentCard.vocabularyId;
                        const senseId = isExample ? currentCard.senseId : undefined;
                        void handleSynthesizeTeacherSpeech(
                          text,
                          kind,
                          'UK',
                          targetId,
                          currentCard.vocabularyId,
                          senseId
                        );
                      }}
                    >
                      <Volume2 className="w-3.5 h-3.5 text-sky-600" />
                      {t('vocab.sampleUk')}
                    </Button>
                  </div>

                  {!isRecording ? (
                    <Button
                      variant="primary"
                      size="sm"
                      disabled={
                        assessMutation.isPending || (targetType === 'example' && !hasExamples)
                      }
                      onClick={() => void startRecording()}
                    >
                      <Mic className="w-3.5 h-3.5" />
                      {assessMutation.isPending ? t('vocab.scoring') : t('vocab.recordAndScore')}
                    </Button>
                  ) : (
                    <Button variant="danger" size="sm" onClick={stopRecording}>
                      <Square className="w-3.5 h-3.5" />
                      {t('vocab.stopRecording')}
                    </Button>
                  )}
                </div>
              </div>

              {/* Scoring Rubric Breakdown (Accuracy 40% · Clarity 30% · Fluency 30%) */}
              <div className="p-3.5 rounded-2xl bg-slate-50/70 border border-slate-200/80 space-y-2">
                <div className="flex items-center justify-between text-[11px] font-bold text-slate-600">
                  <span>{t('vocab.rubricStandard')} {activeRubric?.code ?? 'ENGLISH_PRONUNCIATION_V1'}</span>
                  <span>Accuracy 40% · Clarity 30% · Fluency 30%</span>
                </div>

                {assessmentError && (
                  <p role="alert" className="text-xs text-red-700 bg-red-50 border border-red-200 rounded-xl p-3">
                    {assessmentError}
                  </p>
                )}

                {assessmentResult ? (
                  <div className="pt-2 space-y-3">
                    <div className="flex items-center justify-between p-3 rounded-xl bg-emerald-600 text-white">
                      <div className="flex items-center gap-2">
                        <Award className="w-5 h-5" />
                        <span className="text-sm font-extrabold">
                          {assessmentResult.overallScore === null
                            ? t('vocab.cannotScore')
                            : `${t('vocab.totalScore')} ${assessmentResult.overallScore}/100`}
                        </span>
                      </div>
                      <span className="text-xs font-bold bg-white/20 px-2.5 py-0.5 rounded-full">
                        {assessmentResult.modelCodeUsed}
                      </span>
                    </div>

                    <div className="grid grid-cols-3 gap-2">
                      {assessmentResult.criterionScores.map((c) => (
                        <div
                          key={c.criterionCode}
                          className="p-2.5 rounded-xl bg-white border border-emerald-200 text-xs"
                        >
                          <div className="font-extrabold text-slate-900">
                            {c.criterionCode}: {c.score}/100
                          </div>
                          <div className="text-[10px] text-emerald-700 font-semibold">
                            Weight: {c.weight}%
                          </div>
                          <div className="text-[11px] text-slate-500 mt-0.5">{c.feedbackVi}</div>
                        </div>
                      ))}
                    </div>

                    <div className="text-xs text-slate-700 bg-white p-3 rounded-xl border border-slate-200 space-y-1">
                      <p>
                        <strong>{t('vocab.phonemeFeedback')}</strong>{' '}
                        {assessmentResult.phonemeAndStressFeedbackVi}
                      </p>
                      <p className="text-emerald-800 font-medium">
                        <strong>{t('vocab.coachingSuggestion')}</strong> {assessmentResult.coachingSuggestionVi}
                      </p>
                    </div>
                  </div>
                ) : (
                  <p className="text-[11px] text-slate-500">
                    {t('vocab.audioTtsGuide')}
                  </p>
                )}
              </div>
            </div>
          )}
        </div>
      </div>
      )}

      {/* MODAL 1: Cấu hình Phát Âm Mẫu AI (Opened via Settings button on toolbar) */}
      <Dialog
        isOpen={isTtsSettingsModalOpen}
        onClose={() => setIsTtsSettingsModalOpen(false)}
        maxWidth="2xl"
        icon={
          <img
            src="/assets/icons/navigation/nav-settings.svg"
            alt=""
            className="w-5 h-5 object-contain"
          />
        }
        title={t('vocab.ttsModalTitle')}
        description={t('vocab.ttsModalDesc')}
        footer={
          <div className="flex items-center justify-end gap-2 w-full">
            <Button size="sm" onClick={() => setIsTtsSettingsModalOpen(false)}>
              {t('vocab.saveAndClose')}
            </Button>
          </div>
        }
      >
        <div className="space-y-4 text-xs">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label className="block font-bold text-slate-700 mb-1.5">
                {t('vocab.liveTtsModelLabel')}
              </label>
              <select
                value={ttsModelCode}
                onChange={(e) =>
                  setTtsModelCode(
                    e.target.value as 'gemini-3.8-live' | 'gemini-3.8-live-extended-thinking'
                  )
                }
                className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 bg-white font-semibold text-slate-800"
              >
                <option value="gemini-3.8-live">{t('vocab.liveSpeedInstant')}</option>
                <option value="gemini-3.8-live-extended-thinking">
                  {t('vocab.liveExtendedThinking')}
                </option>
              </select>
            </div>

            <div>
              <label className="block font-bold text-slate-700 mb-1.5">
                {t('vocab.teacherVoiceCount', { count: allVoices.length })}
              </label>
              <select
                value={ttsVoiceName}
                onChange={(e) => setTtsVoiceName(e.target.value)}
                className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 bg-white font-semibold text-slate-800"
              >
                {allVoices.map((v) => (
                  <option key={v.id} value={v.id}>
                    {v.label} — {v.style} ({v.persona})
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="block font-bold text-slate-700 mb-1.5">
                {t('vocab.defaultAccentLabel')}
              </label>
              <div className="grid grid-cols-2 gap-2">
                {(['US', 'UK'] as const).map((acc) => (
                  <button
                    key={acc}
                    type="button"
                    onClick={() => setTtsAccent(acc)}
                    className={cn(
                      'py-2.5 rounded-xl font-extrabold border transition-all cursor-pointer',
                      ttsAccent === acc
                        ? 'bg-emerald-600 text-white border-emerald-600'
                        : 'bg-slate-50 text-slate-700 border-slate-200 hover:bg-slate-100'
                    )}
                  >
                    {acc === 'US' ? '🇺🇸 US (American)' : '🇬🇧 UK (British)'}
                  </button>
                ))}
              </div>
            </div>

            <div>
              <label className="block font-bold text-slate-700 mb-1.5">
                {t('vocab.sampleSpeedLabel')}
              </label>
              <select
                value={ttsSpeed}
                onChange={(e) => setTtsSpeed(parseFloat(e.target.value))}
                className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 bg-white font-semibold text-slate-800"
              >
                {speechSpeeds.map((sp) => (
                  <option key={sp.code || sp.value} value={sp.value}>
                    {sp.label}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {lastSynthesizedAudio && (
            <div className="p-4 rounded-2xl bg-emerald-50/70 border border-emerald-200 space-y-2.5">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <div className="flex items-center gap-1.5 flex-wrap">
                  <Badge variant="emerald">{lastSynthesizedAudio.modelCodeUsed}</Badge>
                  <Badge variant="violet">Voice: {lastSynthesizedAudio.voiceName}</Badge>
                  <Badge variant="sky">Accent: {lastSynthesizedAudio.accent}</Badge>
                </div>
                <button
                  type="button"
                  onClick={() => setShowSystemPromptUsed((s) => !s)}
                  className="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-white border border-slate-200 text-[11px] font-bold text-slate-600 hover:text-slate-900 cursor-pointer"
                >
                  <Code2 className="w-3.5 h-3.5" />
                  {showSystemPromptUsed ? t('vocab.hideSystemPrompt') : t('vocab.viewSystemPrompt')}
                </button>
              </div>
              <audio
                ref={audioPlayerRef}
                controls
                src={`data:${lastSynthesizedAudio.mimeType};base64,${lastSynthesizedAudio.audioBase64}`}
                className="w-full h-9"
              />
              {showSystemPromptUsed && (
                <pre className="p-3 rounded-xl bg-slate-900 text-emerald-300 text-[11px] font-mono whitespace-pre-wrap">
                  {lastSynthesizedAudio.systemPromptUsed}
                </pre>
              )}
            </div>
          )}
        </div>
      </Dialog>

      {/* MODAL 2: Tạo Từ Vựng Thông Minh bằng AI (Opened via "Tạo từ vựng bằng AI" button on toolbar) */}
      <Dialog
        isOpen={isAiGeneratorModalOpen}
        onClose={() => setIsAiGeneratorModalOpen(false)}
        maxWidth="2xl"
        icon={
          <img
            src="/assets/icons/navigation/nav-study.svg"
            alt=""
            className="w-5 h-5 object-contain"
          />
        }
        title={t('vocab.aiGeneratorTitle')}
        description={t('vocab.aiGeneratorDesc')}
        footer={
          <div className="flex items-center justify-end gap-2.5 w-full">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setIsAiGeneratorModalOpen(false)}
              disabled={generateMutation.isPending || importJsonMutation.isPending}
            >
              {t('vocab.cancelBtn')}
            </Button>
            {genMode === 'direct' ? (
              <Button
                variant="primary"
                size="sm"
                disabled={generateMutation.isPending}
                onClick={() => void handleGenerateMoreWords()}
              >
                <Sparkles className="w-4 h-4" />
                {generateMutation.isPending
                  ? t('vocab.creatingWithModel', { model: selectedVocabModel })
                  : t('vocab.addWordsToDeck')}
              </Button>
            ) : (
              <Button
                variant="primary"
                size="sm"
                disabled={importJsonMutation.isPending || !manualJsonInput.trim()}
                onClick={() => void handleImportManualJson()}
              >
                <Sparkles className="w-4 h-4" />
                {importJsonMutation.isPending ? t('vocab.savingToDb') : t('vocab.importWordsToDeck')}
              </Button>
            )}
          </div>
        }
      >
        <div className="space-y-4 text-xs">
          {/* Mode Switch Tabs */}
          <div className="flex border-b border-slate-200 mb-2 gap-2">
            <button
              type="button"
              onClick={() => setGenMode('direct')}
              className={cn(
                'flex items-center gap-1.5 pb-2.5 px-3 font-bold border-b-2 transition-all cursor-pointer',
                genMode === 'direct'
                  ? 'border-emerald-600 text-emerald-700'
                  : 'border-transparent text-slate-500 hover:text-slate-800'
              )}
            >
              <Cpu className="w-3.5 h-3.5" />
              {t('vocab.genModeDirect')}
            </button>
            <button
              type="button"
              onClick={() => setGenMode('manual')}
              className={cn(
                'flex items-center gap-1.5 pb-2.5 px-3 font-bold border-b-2 transition-all cursor-pointer',
                genMode === 'manual'
                  ? 'border-emerald-600 text-emerald-700'
                  : 'border-transparent text-slate-500 hover:text-slate-800'
              )}
            >
              <FileText className="w-3.5 h-3.5" />
              {t('vocab.genModeManual')}
              <Badge variant="emerald" className="text-[10px] py-0 px-1.5 ml-1">
                {t('vocab.freeBadge')}
              </Badge>
            </button>
          </div>

          {genMode === 'direct' ? (
            <>
              <div className="space-y-1.5">
                <div className="flex items-center justify-between">
                  <label className="font-bold uppercase tracking-wider text-slate-600 flex items-center gap-1.5">
                    <Cpu className="w-3.5 h-3.5 text-emerald-600" />
                    {t('vocab.vocabModelLabel')}
                  </label>
                  <Badge variant="emerald">DB ({vocabModels.length})</Badge>
                </div>
                <select
                  value={selectedVocabModel}
                  onChange={(e) => setSelectedVocabModel(e.target.value)}
                  className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 bg-white font-semibold text-slate-800"
                >
                  {vocabModels.map((m) => (
                    <option key={m.modelCode} value={m.modelCode}>
                      {m.displayName} —{' '}
                      {m.supportsAudioInput ? t('vocab.multimodal') : t('vocab.textOnly')}
                    </option>
                  ))}
                </select>
                {vocabModels.find((m) => m.modelCode === selectedVocabModel) && (
                  <p className="text-[11px] text-slate-500">
                    {vocabModels.find((m) => m.modelCode === selectedVocabModel)?.capabilityNote}
                  </p>
                )}
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block font-bold uppercase tracking-wider text-slate-600 mb-1">
                    {t('vocab.cefrLevelLabel')}
                  </label>
                  <select
                    value={genCefr}
                    onChange={(e) => setGenCefr(e.target.value as CefrLevel)}
                    className="w-full px-3 py-2.5 rounded-xl border border-slate-200 bg-white font-semibold"
                  >
                    {(['A1', 'A2', 'B1', 'B2', 'C1', 'C2'] as CefrLevel[]).map((l) => (
                      <option key={l} value={l}>
                        CEFR {l}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block font-bold uppercase tracking-wider text-slate-600 mb-1">
                    {t('vocab.wordsCountLabel')}
                  </label>
                  <select
                    value={genCount}
                    onChange={(e) => setGenCount(Number(e.target.value))}
                    className="w-full px-3 py-2.5 rounded-xl border border-slate-200 bg-white font-semibold"
                  >
                    <option value={2}>{t('vocab.intensiveWords', { count: 2 })}</option>
                    <option value={3}>{t('vocab.intensiveWords', { count: 3 })}</option>
                    <option value={5}>{t('vocab.intensiveWords', { count: 5 })}</option>
                    <option value={10}>{t('vocab.intensiveWords', { count: 10 })}</option>
                  </select>
                </div>
              </div>

              <TopicCreatablePicker
                label={t('vocab.genTopicLabel')}
                value={genTopic}
                onChange={setGenTopic}
                topics={vocabTopics}
                placeholder={t('vocab.genTopicPlaceholder')}
                helperText={t('vocab.genTopicHelper')}
              />
            </>
          ) : (
            <div className="space-y-3.5">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block font-bold uppercase tracking-wider text-slate-600 mb-1">
                    {t('vocab.cefrLevelLabel')}
                  </label>
                  <select
                    value={genCefr}
                    onChange={(e) => setGenCefr(e.target.value as CefrLevel)}
                    className="w-full px-3 py-2 rounded-xl border border-slate-200 bg-white font-semibold text-xs"
                  >
                    {(['A1', 'A2', 'B1', 'B2', 'C1', 'C2'] as CefrLevel[]).map((l) => (
                      <option key={l} value={l}>
                        CEFR {l}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block font-bold uppercase tracking-wider text-slate-600 mb-1">
                    {t('vocab.wordsCountDesired')}
                  </label>
                  <select
                    value={genCount}
                    onChange={(e) => setGenCount(Number(e.target.value))}
                    className="w-full px-3 py-2 rounded-xl border border-slate-200 bg-white font-semibold text-xs"
                  >
                    <option value={2}>{t('vocab.wordsSimple', { count: 2 })}</option>
                    <option value={3}>{t('vocab.wordsSimple', { count: 3 })}</option>
                    <option value={5}>{t('vocab.wordsSimple', { count: 5 })}</option>
                    <option value={10}>{t('vocab.wordsSimple', { count: 10 })}</option>
                  </select>
                </div>
              </div>

              <TopicCreatablePicker
                label={t('vocab.genTopicLabel')}
                value={genTopic}
                onChange={setGenTopic}
                topics={vocabTopics}
                placeholder={t('vocab.genTopicPlaceholder')}
                helperText={t('vocab.genTopicHelper')}
              />

              <div className="p-3.5 rounded-2xl bg-slate-50 border border-slate-200 space-y-2.5">
                <div className="flex items-center justify-between">
                  <span className="font-bold text-slate-700 flex items-center gap-1.5">
                    <Sparkles className="w-3.5 h-3.5 text-emerald-600" />
                    {t('vocab.step1PromptTitle')}
                  </span>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    disabled={buildPromptMutation.isPending}
                    onClick={() => void handleBuildPrompt()}
                  >
                    {buildPromptMutation.isPending ? (
                      <>
                        <Loader2 className="w-3.5 h-3.5 animate-spin" />
                        {t('vocab.buildingPrompt')}
                      </>
                    ) : (
                      t('vocab.buildPromptBtn')
                    )}
                  </Button>
                </div>

                {manualPrompt && (
                  <div className="space-y-2 pt-1">
                    <div className="relative">
                      <textarea
                        readOnly
                        value={manualPrompt}
                        rows={4}
                        className="w-full p-2.5 rounded-xl bg-slate-900 text-emerald-300 font-mono text-[11px] leading-relaxed resize-none focus:outline-hidden"
                      />
                      <button
                        type="button"
                        onClick={() => void handleCopyPrompt()}
                        className="absolute top-2 right-2 flex items-center gap-1 px-2.5 py-1 rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-[11px] shadow-xs cursor-pointer transition-colors"
                      >
                        {isPromptCopied ? (
                          <>
                            <Check className="w-3.5 h-3.5" />
                            {t('vocab.copiedPrompt')}
                          </>
                        ) : (
                          <>
                            <Copy className="w-3.5 h-3.5" />
                            {t('vocab.copyPrompt')}
                          </>
                        )}
                      </button>
                    </div>
                    <p className="text-[11px] text-slate-500">
                      {t('vocab.copyPromptHint')}
                    </p>
                  </div>
                )}
              </div>

              <div className="p-3.5 rounded-2xl bg-slate-50 border border-slate-200 space-y-2.5">
                <div className="flex items-center justify-between">
                  <span className="font-bold text-slate-700 flex items-center gap-1.5">
                    <FileText className="w-3.5 h-3.5 text-emerald-600" />
                    {t('vocab.step2JsonTitle')}
                  </span>
                  <Badge variant="sky">Nguồn: manual-web-ai</Badge>
                </div>

                <textarea
                  value={manualJsonInput}
                  onChange={(e) => setManualJsonInput(e.target.value)}
                  rows={4}
                  placeholder={t('vocab.jsonInputPlaceholder')}
                  className="w-full p-2.5 rounded-xl border border-slate-300 bg-white font-mono text-[11px] placeholder:text-slate-400 focus:outline-hidden focus:ring-2 focus:ring-emerald-500/20 focus:border-emerald-500"
                />

                <label className="flex items-center gap-2 cursor-pointer select-none text-[11px] font-semibold text-slate-700">
                  <input
                    type="checkbox"
                    checked={assignToTodayPlan}
                    onChange={(e) => setAssignToTodayPlan(e.target.checked)}
                    className="w-4 h-4 rounded-sm border-slate-300 text-emerald-600 focus:ring-emerald-500"
                  />
                  {t('vocab.assignToTodayCheckbox')}
                </label>
              </div>

              {manualError && (
                <div className="p-3 rounded-xl bg-rose-50 border border-rose-200 text-rose-700 text-xs flex items-center gap-2">
                  <AlertCircle className="w-4 h-4 shrink-0 text-rose-500" />
                  <span>{manualError}</span>
                </div>
              )}
            </div>
          )}
        </div>
      </Dialog>
      {/* MODAL 3: Xem Lịch Sử Luyện Tập Phát Âm & Học Từ */}
      <PronunciationHistoryDialog
        isOpen={isHistoryModalOpen}
        onClose={() => setIsHistoryModalOpen(false)}
        card={currentCard}
        targetType={targetType}
        selectedExample={selectedExample}
        selectedExampleIndex={selectedExampleIndex}
        recentAssessmentResult={assessmentResult}
      />
    </div>
  );
};
