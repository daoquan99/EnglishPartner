import React, { useState, useMemo, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  CheckCircle2,
  AlertTriangle,
  ArrowLeft,
  Award,
  ChevronLeft,
  ChevronRight,
  RotateCcw,
  Sparkles,
  AlertCircle,
  Unlock,
  X,
} from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { Dialog } from '@/shared/components/ui/Dialog';
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog';
import { LoadingState } from '@/shared/components/ui/LoadingState';
import { EmptyState } from '@/shared/components/ui/EmptyState';
import { useTutorSessionStore } from '@/features/tutor';
import { ExerciseRenderer } from '@/features/lessons/components/player/ExerciseRenderer';
import {
  useGrammarAttemptQuery,
  useSubmitGrammarAnswerMutation,
  useCompleteGrammarAttemptMutation,
  useStartGrammarAttemptMutation,
} from '../hooks/useGrammarQueries';
import { useGrammarAttemptShortcuts } from '../hooks/useGrammarAttemptShortcuts';
import {
  mapGrammarExerciseToLearnerDto,
  mapGrammarAnswerToExerciseResult,
} from '../utils/grammarExerciseAdapter';
import {
  getGrammarErrorMessage,
  getAnswerIndicatorTone,
  type AnswerIndicatorTone,
  buildGrammarSubmitPayload,
  shouldConfirmGrammarSubmit,
  canRedoGrammarExercise,
  isGrammarAnswerLocked,
} from '../utils/grammarHelpers';
import type { GrammarAttemptResultDto } from '../types/grammar.types';

const INDICATOR_TONE_CLASSES: Record<AnswerIndicatorTone, string> = {
  unanswered: 'bg-slate-100 text-slate-600 border-transparent hover:bg-slate-200',
  correct: 'bg-emerald-500 text-white border-emerald-500',
  partial: 'bg-amber-400 text-amber-950 border-amber-400',
  wrong: 'bg-rose-500 text-white border-rose-500',
  ungraded: 'bg-sky-100 text-sky-800 border-sky-300',
  failed: 'bg-white text-rose-600 border-rose-400 border-dashed',
};

export const GrammarAttemptPage: React.FC = () => {
  const { id = '' } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();

  const { apiKey, assessmentModelCode, aiProvider, agyModelCode } = useTutorSessionStore();
  const hasRealKey = Boolean(apiKey.trim());

  const { data: attempt, isLoading, error, refetch } = useGrammarAttemptQuery(id);
  const submitAnswerMutation = useSubmitGrammarAnswerMutation(id);
  const completeAttemptMutation = useCompleteGrammarAttemptMutation(id);
  const startAttemptMutation = useStartGrammarAttemptMutation();

  const [currentIndex, setCurrentIndex] = useState<number>(0);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isConfirmCompleteOpen, setIsConfirmCompleteOpen] = useState<boolean>(false);
  const [completionResult, setCompletionResult] = useState<GrammarAttemptResultDto | null>(null);

  // Redo state for Practice mode (kind === 'Practice')
  const [redoExerciseIds, setRedoExerciseIds] = useState<Record<string, boolean>>({});

  // Single-submission confirm state for StepCore / LevelTest
  const [pendingSubmitPayload, setPendingSubmitPayload] = useState<Record<string, unknown> | null>(null);
  const [isConfirmSubmitOpen, setIsConfirmSubmitOpen] = useState<boolean>(false);
  const [isConfirmRestartOpen, setIsConfirmRestartOpen] = useState<boolean>(false);

  const exercises = attempt?.exercises || [];
  const answers = attempt?.answers || [];

  const currentExercise = exercises[currentIndex];
  const isCompleted = Boolean(attempt?.completedAtUtc);

  // Map answers to exercise ID lookup
  const answersMap = useMemo(() => {
    const map = new Map<string, any>();
    for (const a of answers) {
      map.set(a.exerciseId, a);
    }
    return map;
  }, [answers]);

  const currentAnswer = currentExercise ? answersMap.get(currentExercise.id) : null;
  const answeredCount = answers.length;
  const totalCount = exercises.length;

  const isLockedKind = attempt?.kind === 'StepCore' || attempt?.kind === 'LevelTest';
  const isRedoMode = Boolean(currentExercise && redoExerciseIds[currentExercise.id]);

  const handleStartRedo = () => {
    if (!currentExercise || !attempt || !canRedoGrammarExercise(attempt.kind, currentAnswer?.status)) return;
    setRedoExerciseIds((prev) => ({
      ...prev,
      [currentExercise.id]: true,
    }));
  };

  const handleCancelRedo = () => {
    if (!currentExercise) return;
    setRedoExerciseIds((prev) => {
      const next = { ...prev };
      delete next[currentExercise.id];
      return next;
    });
  };

  const executeSubmit = async (answerPayload: Record<string, unknown>) => {
    if (!currentExercise) return;
    setSubmitError(null);

    // Build payload using correct BE LessonAiExecutionConfigDto: { apiKey, assessmentModelCode, provider }
    // If no real key, do NOT send aiConfig even for AGY
    const payload = buildGrammarSubmitPayload(
      currentExercise.id,
      answerPayload,
      aiProvider,
      apiKey,
      assessmentModelCode,
      agyModelCode
    );

    try {
      await submitAnswerMutation.mutateAsync(payload);
      // Clear redo state on success
      setRedoExerciseIds((prev) => {
        const next = { ...prev };
        delete next[currentExercise.id];
        return next;
      });
    } catch (err: any) {
      setSubmitError(getGrammarErrorMessage(err, t));
    }
  };

  const handleSubmit = (answerPayload: Record<string, unknown>) => {
    if (!currentExercise || !attempt) return;
    setSubmitError(null);

    // Locked check: StepCore and LevelTest only allow 1 submission, unless FailedGrading
    if (isGrammarAnswerLocked(attempt.kind, currentAnswer?.status)) {
      setSubmitError(t('grammar.errors.answerLocked'));
      return;
    }

    if (shouldConfirmGrammarSubmit(attempt.kind, currentAnswer?.status)) {
      setPendingSubmitPayload(answerPayload);
      setIsConfirmSubmitOpen(true);
      return;
    }

    void executeSubmit(answerPayload);
  };

  const handleConfirmedSubmit = () => {
    setIsConfirmSubmitOpen(false);
    if (pendingSubmitPayload) {
      void executeSubmit(pendingSubmitPayload);
      setPendingSubmitPayload(null);
    }
  };

  const handleComplete = async () => {
    setIsConfirmCompleteOpen(false);
    setSubmitError(null);

    try {
      const result = await completeAttemptMutation.mutateAsync();
      setCompletionResult(result);
    } catch (err: any) {
      setSubmitError(getGrammarErrorMessage(err, t));
    }
  };

  const handleRestartAttempt = async () => {
    if (!attempt) return;
    setSubmitError(null);

    try {
      const newAttempt = await startAttemptMutation.mutateAsync({
        kind: attempt.kind,
        stepCode: attempt.stepCode || undefined,
        cefrLevel: attempt.cefrLevel,
        forceNew: true,
      });
      setIsConfirmRestartOpen(false);
      navigate(`/student/grammar/attempts/${newAttempt.id}`);
    } catch (err: any) {
      setSubmitError(getGrammarErrorMessage(err, t));
    }
  };

  const exerciseContainerRef = useRef<HTMLDivElement>(null);
  const goToQuestion = (delta: number) => {
    setCurrentIndex((i) => Math.min(Math.max(0, i + delta), Math.max(0, totalCount - 1)));
    setSubmitError(null);
  };
  useGrammarAttemptShortcuts({
    containerRef: exerciseContainerRef,
    onNext: () => goToQuestion(1),
    onPrevious: () => goToQuestion(-1),
    enabled: !completionResult,
  });

  if (isLoading) {
    return <LoadingState message={t('grammar.loadingAttempt')} />;
  }

  if (error || !attempt) {
    const status = (error as any)?.response?.status;
    const isNotFound = status === 404;
    const isForbidden = status === 403;

    return (
      <div className="max-w-4xl mx-auto space-y-4">
        <div className="p-4 bg-rose-50 border border-rose-200 text-rose-800 rounded-2xl text-xs space-y-3">
          <div className="font-bold text-sm">
            {isNotFound
              ? t('grammar.attemptNotFound')
              : isForbidden
              ? t('grammar.attemptForbidden')
              : t('grammar.attemptLoadError')}
          </div>
          {!isNotFound && !isForbidden && <div>{getGrammarErrorMessage(error, t)}</div>}
          <div className="flex items-center gap-2 pt-1">
            <Button variant="outline" size="sm" onClick={() => navigate('/student/grammar')}>
              {t('grammar.backToRoadmap')}
            </Button>
            {!isNotFound && !isForbidden && (
              <Button variant="primary" size="sm" onClick={() => void refetch()}>
                {t('grammar.retry')}
              </Button>
            )}
          </div>
        </div>
      </div>
    );
  }

  // Completion Result View (if freshly completed or attempt is completed)
  if (completionResult || (isCompleted && attempt.score100 !== null && attempt.score100 !== undefined)) {
    const score = completionResult ? completionResult.score100 : (attempt.score100 ?? 0);
    const isPracticeKind = attempt.kind === 'Practice';
    const isLevelTestKind = attempt.kind === 'LevelTest';
    const hasCompletionResult = Boolean(completionResult);
    const passed = completionResult?.passed;
    const unlocked = completionResult?.unlockedStepCodes || [];
    const levelPassed = completionResult?.levelPassed;

    return (
      <div className="max-w-2xl mx-auto space-y-6">
        <div className="bg-white border border-slate-200 rounded-3xl p-5 sm:p-8 shadow-sm text-center space-y-6">
          <div
            className={`w-20 h-20 rounded-3xl flex items-center justify-center mx-auto shadow-sm ${
              !hasCompletionResult || isPracticeKind || passed ? 'bg-emerald-100 text-emerald-700' : 'bg-rose-100 text-rose-700'
            }`}
          >
            {!hasCompletionResult || isPracticeKind || passed ? <Award className="w-10 h-10" /> : <RotateCcw className="w-10 h-10" />}
          </div>

          <div className="space-y-2">
            {/* If no completionResult (reloaded completed attempt), do NOT show Passed/Failed badge */}
            {hasCompletionResult && !isPracticeKind && (
              <Badge variant={passed ? 'emerald' : 'rose'} className="text-xs px-3 py-1 font-bold">
                {passed ? t('grammar.passed') : t('grammar.failed')}
              </Badge>
            )}

            <h2 className="text-2xl font-extrabold text-slate-900">
              {!hasCompletionResult
                ? t('grammar.attemptCompleted')
                : isPracticeKind || passed
                ? t('grammar.congratsCompleted')
                : t('grammar.needMoreEffort')}
            </h2>

            <div className="text-4xl font-extrabold text-slate-900 pt-2">
              <span className={!hasCompletionResult || isPracticeKind || passed ? 'text-emerald-700' : 'text-rose-700'}>
                {Math.round(score)}%
              </span>
            </div>

            <p className="text-xs sm:text-sm text-slate-500 max-w-md mx-auto">
              {isLevelTestKind
                ? t('grammar.levelTestResultDesc', { level: attempt.cefrLevel })
                : attempt.kind === 'StepCore'
                ? t('grammar.stepCoreResultDesc')
                : t('grammar.practiceCompleted')}
            </p>
          </div>

          {/* LevelTest: Display passed message if passed, and do NOT list individual unlocked steps */}
          {hasCompletionResult && isLevelTestKind && (passed || levelPassed) && (
            <div className="p-4 rounded-2xl bg-amber-50 border border-amber-200 text-amber-950 text-xs sm:text-sm font-bold flex items-center justify-center gap-2">
              <Sparkles className="w-5 h-5 text-amber-600 shrink-0" />
              <span>{t('grammar.levelPassedAll', { level: attempt.cefrLevel })}</span>
            </div>
          )}

          {/* StepCore: Display unlocked steps list */}
          {hasCompletionResult && !isLevelTestKind && unlocked.length > 0 && (
            <div className="p-4 rounded-2xl bg-emerald-50 border border-emerald-200 text-left space-y-2">
              <div className="font-bold text-xs text-emerald-950 flex items-center gap-1.5">
                <Unlock className="w-4 h-4 text-emerald-700" />
                {t('grammar.stepUnlockedCodes')}
              </div>
              <div className="flex flex-wrap gap-2">
                {unlocked.map((code) => (
                  <button
                    key={code}
                    type="button"
                    onClick={() => navigate(`/student/grammar/${code}`)}
                    className="font-mono font-bold text-xs px-2.5 py-1 rounded-lg bg-white border border-emerald-300 text-emerald-800 hover:bg-emerald-100 transition-colors cursor-pointer"
                  >
                    {code} →
                  </button>
                ))}
              </div>
            </div>
          )}

          <div className="pt-4 flex flex-wrap items-center justify-center gap-3">
            {/* Restart attempt button (starts new attempt with same kind/step/level) */}
            <Button
              variant="outline"
              onClick={() => void handleRestartAttempt()}
              disabled={startAttemptMutation.isPending}
              className="text-xs sm:text-sm flex items-center gap-1.5"
            >
              <RotateCcw className="w-4 h-4" />
              {t('grammar.restartAttempt')}
            </Button>

            <Button
              variant="outline"
              onClick={() => navigate('/student/grammar')}
              className="text-xs sm:text-sm"
            >
              {t('grammar.backToRoadmap')}
            </Button>

            {attempt.stepCode && (
              <Button
                variant="primary"
                onClick={() => navigate(`/student/grammar/${attempt.stepCode}`)}
                className="bg-emerald-600 hover:bg-emerald-700 text-white text-xs sm:text-sm"
              >
                {t('grammar.backToStep')} ({attempt.stepCode})
              </Button>
            )}
          </div>
        </div>
      </div>
    );
  }

  const mappedExercise = currentExercise ? mapGrammarExerciseToLearnerDto(currentExercise) : null;
  const mappedResult = currentAnswer && currentExercise
    ? mapGrammarAnswerToExerciseResult(currentAnswer, currentExercise)
    : null;

  const isSelfAssessed = currentAnswer?.status === 'SelfAssessed';
  const suggestedAnswerText =
    currentAnswer?.suggestedAnswer ||
    mappedResult?.suggestedAnswer ||
    (typeof currentAnswer?.feedback === 'object' && currentAnswer?.feedback !== null
      ? (currentAnswer.feedback as any).suggestedAnswer
      : null);

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* Attempt Top Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-slate-200">
        <div className="flex items-start gap-3 min-w-0">
          <Button
            variant="ghost"
            size="sm"
            onClick={() =>
              attempt.stepCode
                ? navigate(`/student/grammar/${attempt.stepCode}`)
                : navigate('/student/grammar')
            }
            className="p-2 text-slate-500 hover:text-slate-800 shrink-0 mt-0.5"
            title={t('grammar.back')}
          >
            <ArrowLeft className="w-5 h-5 shrink-0" />
          </Button>

          <div className="min-w-0 flex-1 space-y-1">
            <div className="flex flex-col sm:flex-row sm:items-center gap-1.5 sm:gap-2">
              <Badge variant="default" className="text-[10px] font-mono whitespace-nowrap self-start">
                {attempt.kind === 'StepCore'
                  ? `${t('grammar.coreExercises')}: ${attempt.stepCode}`
                  : attempt.kind === 'LevelTest'
                  ? `${t('grammar.levelTest')}: ${attempt.cefrLevel}`
                  : `${t('grammar.practiceExercises')}: ${attempt.stepCode || attempt.cefrLevel}`}
              </Badge>

              {isLockedKind && (
                <span className="text-[10px] text-amber-700 font-semibold bg-amber-50 px-2 py-0.5 rounded-full border border-amber-200 self-start leading-tight">
                  {t('grammar.oneTimeSubmitNotice')}
                </span>
              )}
            </div>

            <div className="text-xs text-slate-500">
              {t('grammar.attemptProgressText', { current: currentIndex + 1, total: totalCount, answered: answeredCount })}
            </div>
          </div>
        </div>

        {/* Restart (new attempt, previous results kept) + Complete Attempt */}
        <div className="flex items-center gap-2 flex-wrap">
          {answeredCount > 0 && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => setIsConfirmRestartOpen(true)}
              disabled={startAttemptMutation.isPending || completeAttemptMutation.isPending}
            >
              <RotateCcw className="w-4 h-4" />
              {t('grammar.restartFromScratch')}
            </Button>
          )}
          <Button
            variant="primary"
            size="sm"
            onClick={() => setIsConfirmCompleteOpen(true)}
            disabled={completeAttemptMutation.isPending}
            className="bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-xs flex items-center gap-1.5"
          >
            <CheckCircle2 className="w-4 h-4" />
            {completeAttemptMutation.isPending ? t('grammar.gradingAttempt') : t('grammar.completeAttempt')}
          </Button>
        </div>
      </div>

      {/* Question Indicators Bar */}
      <div className="flex items-center gap-1.5 overflow-x-auto px-1.5 py-1.5 pb-2 scrollbar-none">
        {exercises.map((ex, idx) => {
          const ans = answersMap.get(ex.id);
          const isCurrent = idx === currentIndex;
          const tone = getAnswerIndicatorTone(ans);

          return (
            <button
              key={ex.id}
              type="button"
              onClick={() => {
                setCurrentIndex(idx);
                setSubmitError(null);
              }}
              aria-current={isCurrent ? 'step' : undefined}
              aria-label={t('grammar.indicatorLabel', { index: idx + 1, status: t(`grammar.indicator.${tone}`) })}
              title={t(`grammar.indicator.${tone}`)}
              className={`w-8 h-8 rounded-xl font-mono text-xs font-bold transition-all shrink-0 cursor-pointer flex items-center justify-center border ${
                INDICATOR_TONE_CLASSES[tone]
              } ${isCurrent ? 'ring-2 ring-offset-1 ring-slate-900 shadow-xs' : ''}`}
            >
              {idx + 1}
            </button>
          );
        })}
      </div>

      {/* Error alert */}
      {submitError && (
        <div className="p-3.5 bg-rose-50 border border-rose-200 text-rose-800 rounded-2xl text-xs flex items-center justify-between">
          <div className="flex items-center gap-2">
            <AlertCircle className="w-4 h-4 shrink-0" />
            <span>{submitError}</span>
          </div>
          <button
            type="button"
            onClick={() => setSubmitError(null)}
            className="text-xs font-bold underline ml-2 cursor-pointer"
            aria-label={t('grammar.closeNotification')}
          >
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {/* Active Question Player */}
      <div className="bg-white border border-slate-200 rounded-3xl p-4 sm:p-6 shadow-xs space-y-6">
        {currentExercise?.unavailable ? (
          <div className="p-6 text-center space-y-2">
            <AlertTriangle className="w-8 h-8 text-amber-500 mx-auto" />
            <div className="font-bold text-slate-900 text-sm">{t('grammar.exerciseUnavailable')}</div>
          </div>
        ) : mappedExercise ? (
          <div
            ref={exerciseContainerRef}
            key={`${attempt.id}:${currentExercise.id}:${isRedoMode ? 'redo' : 'normal'}`}
            className="space-y-6"
          >
            {/* AiRubric without key notice */}
            {(currentExercise.type === 'Translation' ||
              currentExercise.type === 'WritingPractice') &&
              !hasRealKey && (
                <div className="p-3 rounded-xl bg-indigo-50 border border-indigo-200 text-indigo-900 text-xs flex items-center gap-2">
                  <Sparkles className="w-4 h-4 text-indigo-600 shrink-0" />
                  <span>{t('grammar.selfAssessedNotice')}</span>
                </div>
              )}

            <ExerciseRenderer
              attemptId={attempt.id}
              exercise={mappedExercise}
              onSubmit={handleSubmit}
              result={mappedResult}
              isSubmitting={submitAnswerMutation.isPending}
              isAttemptInProgress={!isCompleted}
              onStartRedo={attempt.kind === 'Practice' ? handleStartRedo : undefined}
              onCancelRedo={attempt.kind === 'Practice' ? handleCancelRedo : undefined}
              isRedoMode={isRedoMode}
            />

            {/* SelfAssessed result display: badge and suggested answer */}
            {isSelfAssessed && (
              <div className="p-3 sm:p-4 rounded-2xl bg-amber-50/90 border border-amber-200/80 text-amber-950 space-y-2.5">
                <div className="flex items-center gap-2">
                  <Badge variant="amber">{t('grammar.selfAssessed')}</Badge>
                  <span className="text-xs font-bold text-amber-900">{t('grammar.selfAssessedTitle')}</span>
                </div>
                <p className="text-xs text-amber-800 leading-relaxed">{t('grammar.selfAssessedNotice')}</p>
                {suggestedAnswerText && (
                  <div className="p-2.5 sm:p-3 bg-white rounded-xl border border-amber-200/70 text-xs">
                    <span className="font-bold text-slate-700 block mb-1">{t('grammar.referenceAnswer')}:</span>
                    <p className="font-mono text-slate-900 whitespace-pre-wrap">{suggestedAnswerText}</p>
                  </div>
                )}
              </div>
            )}
          </div>
        ) : (
          <EmptyState title={t('grammar.noQuestionsTitle')} description={t('grammar.noQuestionsDesc')} />
        )}

        {/* Prev / Next Question Navigation */}
        <div className="flex items-center justify-between pt-6 border-t border-slate-100 flex-wrap gap-2">
          <Button
            variant="outline"
            size="sm"
            disabled={currentIndex <= 0}
            onClick={() => {
              setCurrentIndex((i) => Math.max(0, i - 1));
              setSubmitError(null);
            }}
            className="text-xs flex items-center gap-1"
          >
            <ChevronLeft className="w-4 h-4" />
            {t('grammar.prevQuestion')}
          </Button>

          <Button
            variant="outline"
            size="sm"
            disabled={currentIndex >= totalCount - 1}
            onClick={() => {
              setCurrentIndex((i) => Math.min(totalCount - 1, i + 1));
              setSubmitError(null);
            }}
            className="text-xs flex items-center gap-1"
          >
            {t('grammar.nextQuestion')}
            <ChevronRight className="w-4 h-4" />
          </Button>
        </div>
        <p className="hidden sm:block text-center text-[11px] text-slate-400">
          <kbd className="rounded border border-slate-200 bg-slate-50 px-1 font-mono">Ctrl</kbd>+<kbd className="rounded border border-slate-200 bg-slate-50 px-1 font-mono">Enter</kbd>{' '}
          {t('grammar.shortcutSubmit')} ·{' '}
          <kbd className="rounded border border-slate-200 bg-slate-50 px-1 font-mono">Ctrl</kbd>+<kbd className="rounded border border-slate-200 bg-slate-50 px-1 font-mono">←</kbd>/<kbd className="rounded border border-slate-200 bg-slate-50 px-1 font-mono">→</kbd>{' '}
          {t('grammar.shortcutNavigate')}
        </p>
      </div>

      {/* Confirmation Dialog on Single Submit for StepCore / LevelTest */}
      <ConfirmDialog
        isOpen={isConfirmSubmitOpen}
        onClose={() => {
          setIsConfirmSubmitOpen(false);
          setPendingSubmitPayload(null);
        }}
        onConfirm={handleConfirmedSubmit}
        variant="primary"
        title={t('grammar.confirmSubmitTitle')}
        targetName={currentExercise?.title || t('grammar.exercise')}
        targetLabel={t('grammar.exerciseLabel')}
        description={t('grammar.oneTimeSubmitNotice')}
        confirmLabel={t('grammar.submitConfirm')}
        confirmOnEnter
        isLoading={submitAnswerMutation.isPending}
      />

      <ConfirmDialog
        isOpen={isConfirmRestartOpen}
        onClose={() => setIsConfirmRestartOpen(false)}
        onConfirm={() => void handleRestartAttempt()}
        variant="primary"
        title={t('grammar.restartAttemptTitle')}
        targetName={attempt.stepCode || attempt.cefrLevel}
        targetLabel={t('grammar.restartAttemptTarget')}
        description={t('grammar.restartAttemptDesc')}
        confirmLabel={t('grammar.restartFromScratch')}
        isLoading={startAttemptMutation.isPending}
      />

      {/* Confirmation Dialog on Complete */}
      <Dialog
        isOpen={isConfirmCompleteOpen}
        onClose={() => setIsConfirmCompleteOpen(false)}
        title={t('grammar.confirmComplete')}
        footer={
          <div className="flex items-center justify-end gap-2 w-full">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setIsConfirmCompleteOpen(false)}
            >
              {t('grammar.continueDoing')}
            </Button>
            <Button
              variant="primary"
              size="sm"
              onClick={() => void handleComplete()}
              className="bg-emerald-600 hover:bg-emerald-700 text-white"
            >
              {t('grammar.completeNow')}
            </Button>
          </div>
        }
      >
        <div className="space-y-3 text-xs text-slate-700">
          {answeredCount < totalCount ? (
            <div className="p-3 bg-amber-50 border border-amber-200 rounded-xl text-amber-900 flex items-start gap-2">
              <AlertTriangle className="w-4 h-4 shrink-0 mt-0.5 text-amber-600" />
              <div>
                {t('grammar.confirmCompleteDesc', { unanswered: totalCount - answeredCount })}
              </div>
            </div>
          ) : (
            <div>{t('grammar.confirmCompleteAllAnswered', { total: totalCount })}</div>
          )}
        </div>
      </Dialog>
    </div>
  );
};
