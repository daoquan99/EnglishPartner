import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { isAxiosError } from 'axios';
import { extractApiErrorMessage } from '@/shared/api/httpClient';
import { Loader2 } from 'lucide-react';
import { DailyLearningSettingsDialog } from '../components/DailyLearningSettingsDialog';
import { PronunciationHistoryDialog } from '../components/PronunciationHistoryDialog';
import { AiSpeechSettingsDialog } from '@/features/speech';
import { useActiveScoringRubricQuery } from '@/features/catalog';
import { useTutorSessionStore } from '@/features/tutor';
import {
  useDailyVocabularyPlanQuery,
  useInitializeDailyPlanMutation,
  useLearningSettingsQuery,
  useSubmitVocabularyReviewMutation,
  useUpdateLearningSettingsMutation,
} from '../hooks/useDailyVocabulary';
import { usePronunciationStudio } from '../hooks/usePronunciationStudio';
import { DailyVocabularyToolbar } from '../components/DailyVocabularyToolbar';
import { DailyVocabularyAlerts } from '../components/DailyVocabularyAlerts';
import { DailyVocabularyEmptyState } from '../components/DailyVocabularyEmptyState';
import { VocabularyFlashcard } from '../components/VocabularyFlashcard';
import { PronunciationStudio } from '../components/PronunciationStudio';
import { AiVocabularyGeneratorDialog } from '../components/AiVocabularyGeneratorDialog';
import type { ReviewRating, VocabularyFlashcardDto } from '../types/vocabulary.types';

export const DailyVocabularyPage: React.FC = () => {
  const { t } = useTranslation();
  const { apiKey } = useTutorSessionStore();

  const {
    data: plan,
    isLoading: isPlanLoading,
    error: planError,
    refetch: refetchPlan,
  } = useDailyVocabularyPlanQuery();
  const { data: learningSettings } = useLearningSettingsQuery();
  const updateSettingsMutation = useUpdateLearningSettingsMutation();
  const initPlanMutation = useInitializeDailyPlanMutation();
  const reviewMutation = useSubmitVocabularyReviewMutation();
  const { data: activeRubric } = useActiveScoringRubricQuery();

  const [cards, setCards] = useState<VocabularyFlashcardDto[]>([]);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [isFlipped, setIsFlipped] = useState(false);
  const [apiKeyWarning, setApiKeyWarning] = useState<string | null>(null);
  const [reviewError, setReviewError] = useState<string | null>(null);

  // Dialog open states
  const [isLearningSettingsOpen, setIsLearningSettingsOpen] = useState(false);
  const [isTtsSettingsModalOpen, setIsTtsSettingsModalOpen] = useState(false);
  const [isAiGeneratorModalOpen, setIsAiGeneratorModalOpen] = useState(false);
  const [isHistoryModalOpen, setIsHistoryModalOpen] = useState(false);
  const [aiGeneratorInitialMode, setAiGeneratorInitialMode] = useState<'direct' | 'manual' | undefined>(undefined);
  const autoInitAttemptedRef = useRef(false);

  useEffect(() => {
    if (plan?.items) {
      setCards(plan.items);
      setCurrentIndex((prev) => (plan.items.length === 0 ? 0 : Math.min(prev, plan.items.length - 1)));
    } else if (!plan && !isPlanLoading) {
      setCards([]);
      setCurrentIndex(0);
    }
  }, [plan, isPlanLoading]);

  const currentCard = useMemo(() => cards[currentIndex] ?? null, [cards, currentIndex]);

  const pronunciationStudio = usePronunciationStudio({
    currentCard,
    onSetApiKeyWarning: setApiKeyWarning,
  });
  // Pull out the stable callbacks so memoized children are not invalidated by the hook's per-render object.
  const { resetPronunciationAndStopRecordingSafely, startRecording } = pronunciationStudio;

  const switchCardSafely = useCallback(
    (nextIdx: number) => {
      resetPronunciationAndStopRecordingSafely();
      setIsFlipped(false);
      setCurrentIndex(nextIdx);
    },
    [resetPronunciationAndStopRecordingSafely]
  );

  const handlePrevCard = useCallback(() => {
    switchCardSafely(Math.max(0, currentIndex - 1));
  }, [currentIndex, switchCardSafely]);

  const handleNextCard = useCallback(() => {
    switchCardSafely(Math.min(cards.length - 1, currentIndex + 1));
  }, [cards.length, currentIndex, switchCardSafely]);

  const handleToggleFlip = useCallback(() => {
    setIsFlipped((f) => !f);
  }, []);

  const handleFlipToBack = useCallback(() => {
    setIsFlipped(true);
  }, []);

  const handleReview = useCallback(
    async (rating: ReviewRating) => {
      if (!currentCard) return;
      resetPronunciationAndStopRecordingSafely();
      setReviewError(null);

      const clientRequestId =
        typeof crypto !== 'undefined' && crypto.randomUUID
          ? crypto.randomUUID()
          : `req_${Date.now()}_${Math.random().toString(36).slice(2, 9)}`;

      try {
        const updated = await reviewMutation.mutateAsync({
          payload: {
            vocabularyId: currentCard.vocabularyId,
            senseId: currentCard.senseId,
            rating,
            responseTimeMs: 2500,
            clientRequestId,
          },
          dailyItemId: currentCard.dailyItemId,
        });
        setCards((prev) =>
          prev.map((c, idx) =>
            idx === currentIndex ? { ...c, ...updated, isCompletedToday: true } : c
          )
        );
        setIsFlipped(false);
        if (currentIndex < cards.length - 1) {
          setCurrentIndex((i) => i + 1);
        }
      } catch (err: any) {
        setReviewError(
          err?.response?.data?.error ||
            err?.message ||
            'Không thể ghi nhận đánh giá ôn tập. Vui lòng thử lại.'
        );
      }
    },
    [cards.length, currentCard, currentIndex, resetPronunciationAndStopRecordingSafely, reviewMutation]
  );

  const handleInitializeTodayPlan = useCallback(async () => {
    setApiKeyWarning(null);
    try {
      await initPlanMutation.mutateAsync();
    } catch (err: any) {
      setApiKeyWarning(
        err?.response?.data?.error ||
          err?.message ||
          'Không thể khởi tạo kế hoạch học tập hôm nay. Vui lòng thử lại.'
      );
    }
  }, [initPlanMutation]);

  const handleOpenAiGenerator = useCallback(() => {
    // Without a key only manual mode works; with a key keep whatever mode the learner used last.
    setAiGeneratorInitialMode(apiKey.trim() ? undefined : 'manual');
    setIsAiGeneratorModalOpen(true);
  }, [apiKey]);

  const handleOpenLearningSettings = useCallback(() => setIsLearningSettingsOpen(true), []);
  const handleOpenTtsSettings = useCallback(() => setIsTtsSettingsModalOpen(true), []);
  const handleCloseTtsSettings = useCallback(() => setIsTtsSettingsModalOpen(false), []);
  const handleOpenHistory = useCallback(() => setIsHistoryModalOpen(true), []);
  const handleCloseAiGenerator = useCallback(() => setIsAiGeneratorModalOpen(false), []);
  const handleInitializePlanClick = useCallback(() => void handleInitializeTodayPlan(), [handleInitializeTodayPlan]);
  const handleRefetchPlan = useCallback(() => void refetchPlan(), [refetchPlan]);
  const handleStartRecording = useCallback(() => void startRecording(), [startRecording]);

  const handleGeneratedOrImported = useCallback(
    (generated: VocabularyFlashcardDto[]) => {
      resetPronunciationAndStopRecordingSafely();
      setCards((prev) => [...generated, ...prev]);
      setCurrentIndex(0);
      setIsFlipped(false);
    },
    [resetPronunciationAndStopRecordingSafely]
  );

  const handleClearReviewError = useCallback(() => {
    setReviewError(null);
  }, []);

  const handleClearApiKeyWarning = useCallback(() => {
    setApiKeyWarning(null);
  }, []);

  const is404Uninitialized = useMemo(() => {
    if (!planError) return false;
    const status = isAxiosError(planError)
      ? planError.response?.status
      : (planError as any)?.status;
    if (status !== 404) return false;
    const code = isAxiosError(planError)
      ? (planError.response?.data as any)?.code
      : (planError as any)?.response?.data?.code ?? (planError as any)?.code;
    return code === 'daily_plan_not_initialized';
  }, [planError]);

  useEffect(() => {
    if (is404Uninitialized && !autoInitAttemptedRef.current && !initPlanMutation.isPending) {
      autoInitAttemptedRef.current = true;
      initPlanMutation.mutate(undefined, {
        onError: (err: any) => {
          setApiKeyWarning(extractApiErrorMessage(err));
        },
      });
    }
  }, [is404Uninitialized, initPlanMutation]);

  const isInitializing = initPlanMutation.isPending && !initPlanMutation.isError;

  if ((isPlanLoading || isInitializing) && cards.length === 0) {
    return (
      <div className="p-12 text-center text-sm text-slate-500 flex flex-col items-center justify-center gap-3">
        <Loader2 className="w-6 h-6 animate-spin text-emerald-600" />
        <span>
          {isInitializing
            ? t('vocab.initializingPlan', 'Đang khởi tạo kế hoạch học tập hôm nay...')
            : t('vocab.loadingPlan')}
        </span>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Top Toolbar */}
      <DailyVocabularyToolbar
        plan={plan}
        cards={cards}
        currentIndex={currentIndex}
        ttsVoiceName={pronunciationStudio.ttsVoiceName}
        ttsAccent={pronunciationStudio.ttsAccent}
        ttsSpeed={pronunciationStudio.ttsSpeed}
        onOpenLearningSettings={handleOpenLearningSettings}
        onOpenTtsSettings={handleOpenTtsSettings}
        onOpenAiGenerator={handleOpenAiGenerator}
      />

      {/* Alert Banners */}
      <DailyVocabularyAlerts
        plan={plan}
        learningSettings={learningSettings}
        reviewError={reviewError}
        onClearReviewError={handleClearReviewError}
        apiKeyWarning={apiKeyWarning}
        onClearApiKeyWarning={handleClearApiKeyWarning}
      />

      {/* Main Content Area */}
      {cards.length === 0 ? (
        <DailyVocabularyEmptyState
          is404Uninitialized={is404Uninitialized}
          planError={planError}
          learningSettings={learningSettings}
          isInitializingPlan={initPlanMutation.isPending}
          onInitializePlan={handleInitializePlanClick}
          onOpenLearningSettings={handleOpenLearningSettings}
          onRefetchPlan={handleRefetchPlan}
          onOpenAiGenerator={handleOpenAiGenerator}
        />
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-20 gap-5 items-start">
          {/* Left Column (55% -> 11/20 cols on desktop): Sense Flashcard */}
          <div className="lg:col-span-11 space-y-4">
            {currentCard && (
              <VocabularyFlashcard
                currentCard={currentCard}
                currentIndex={currentIndex}
                totalCards={cards.length}
                isFlipped={isFlipped}
                onToggleFlip={handleToggleFlip}
                onFlipToBack={handleFlipToBack}
                onPrevCard={handlePrevCard}
                onNextCard={handleNextCard}
                onSynthesizeTeacherSpeech={pronunciationStudio.handleSynthesizeTeacherSpeech}
                isSynthesizing={pronunciationStudio.isSynthesizing}
                ttsVoiceName={pronunciationStudio.ttsVoiceName}
                ttsModelCode={pronunciationStudio.ttsModelCode}
                ttsAccent={pronunciationStudio.ttsAccent}
                onReview={handleReview}
              />
            )}
          </div>

          {/* Right Column (45% -> 9/20 cols on desktop): Pronunciation Studio */}
          <div className="lg:col-span-9 space-y-4">
            {currentCard && (
              <PronunciationStudio
                currentCard={currentCard}
                targetType={pronunciationStudio.targetType}
                onSelectTargetType={pronunciationStudio.handleSelectTargetType}
                examples={pronunciationStudio.examples}
                selectedExample={pronunciationStudio.selectedExample}
                selectedExampleIndex={pronunciationStudio.selectedExampleIndex}
                hasExamples={pronunciationStudio.hasExamples}
                onSelectExampleIndex={pronunciationStudio.handleSelectExampleIndex}
                onOpenHistory={handleOpenHistory}
                onOpenTtsSettings={handleOpenTtsSettings}
                aiProvider={pronunciationStudio.aiProvider}
                onSetAiProvider={pronunciationStudio.setAiProvider}
                effectiveAssessmentModel={pronunciationStudio.effectiveAssessmentModel}
                onSynthesizeTeacherSpeech={pronunciationStudio.handleSynthesizeTeacherSpeech}
                isSynthesizing={pronunciationStudio.isSynthesizing}
                isRecording={pronunciationStudio.isRecording}
                onStartRecording={handleStartRecording}
                onStopRecording={pronunciationStudio.stopRecording}
                isAssessing={pronunciationStudio.isAssessing}
                activeRubricCode={activeRubric?.code}
                assessmentResult={pronunciationStudio.assessmentResult}
                assessmentError={pronunciationStudio.assessmentError}
                onClearAssessmentError={pronunciationStudio.handleClearAssessmentError}
              />
            )}
          </div>
        </div>
      )}

      {/* MODAL 1: TTS Settings Dialog */}
      <AiSpeechSettingsDialog
        isOpen={isTtsSettingsModalOpen}
        onClose={handleCloseTtsSettings}
        lastSynthesizedAudio={pronunciationStudio.lastSynthesizedAudio}
        showSystemPromptUsed={pronunciationStudio.showSystemPromptUsed}
        onToggleSystemPromptUsed={pronunciationStudio.handleToggleSystemPromptUsed}
      />

      {/* MODAL 2: AI Vocabulary Generator Dialog */}
      <AiVocabularyGeneratorDialog
        isOpen={isAiGeneratorModalOpen}
        onClose={handleCloseAiGenerator}
        onSuccess={handleGeneratedOrImported}
        onSetApiKeyWarning={setApiKeyWarning}
        initialMode={aiGeneratorInitialMode}
      />

      {/* MODAL 3: Pronunciation History Dialog */}
      <PronunciationHistoryDialog
        isOpen={isHistoryModalOpen}
        onClose={() => setIsHistoryModalOpen(false)}
        card={currentCard}
        targetType={pronunciationStudio.targetType}
        selectedExample={pronunciationStudio.selectedExample}
        selectedExampleIndex={pronunciationStudio.selectedExampleIndex}
        recentAssessmentResult={pronunciationStudio.assessmentResult}
      />

      {/* MODAL 4: Daily Learning Settings Dialog */}
      <DailyLearningSettingsDialog
        isOpen={isLearningSettingsOpen}
        onClose={() => setIsLearningSettingsOpen(false)}
        settings={learningSettings}
        todayPlan={plan}
        onSave={async (target, level) => {
          await updateSettingsMutation.mutateAsync({
            dailyVocabularyTarget: target,
            cefrLevel: level,
          });
          setIsLearningSettingsOpen(false);
        }}
        isSaving={updateSettingsMutation.isPending}
      />
    </div>
  );
};
