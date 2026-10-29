import React, { useState, useEffect } from 'react';
import { useParams, useSearchParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  BookOpen,
  CheckCircle2,
  Lock,
  PlayCircle,
  Sparkles,
  Award,
  Layers,
  ArrowLeft,
  ChevronLeft,
  ChevronRight,
  AlertCircle,
  User,
  X,
} from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { LoadingState } from '@/shared/components/ui/LoadingState';
import { EmptyState } from '@/shared/components/ui/EmptyState';
import {
  useGrammarStepQuery,
  useGrammarStepExercisesQuery,
  useGrammarRoadmapQuery,
  useStartGrammarAttemptMutation,
} from '../hooks/useGrammarQueries';
import { TheoryRenderer } from '../components/TheoryRenderer';
import { GenerateGrammarExercisesDialog } from '../components/GenerateGrammarExercisesDialog';
import { getGrammarErrorMessage, getLevelTestButtonState } from '../utils/grammarHelpers';
import type { GrammarExerciseLearnerDto } from '../types/grammar.types';

type TabKey = 'theory' | 'exercises' | 'practice';

export const GrammarStepPage: React.FC = () => {
  const { code = '' } = useParams<{ code: string }>();
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();
  const { t } = useTranslation();

  // Validate ?tab= param, invalid fallback to 'theory'
  const rawTab = searchParams.get('tab');
  const currentTab: TabKey =
    rawTab === 'theory' || rawTab === 'exercises' || rawTab === 'practice'
      ? rawTab
      : 'theory';

  const setTab = (tab: TabKey) => {
    setSearchParams({ tab });
  };

  const { data: step, isLoading, error, refetch } = useGrammarStepQuery(code);
  const { data: roadmapData } = useGrammarRoadmapQuery();
  const startAttemptMutation = useStartGrammarAttemptMutation();

  // Practice exercises tab state
  const [practiceSource, setPracticeSource] = useState<'core' | 'community' | 'mine'>('core');
  const [practicePage, setPracticePage] = useState<number>(1);
  const [isGenerateDialogOpen, setIsGenerateDialogOpen] = useState<boolean>(false);
  const [startError, setStartError] = useState<string | null>(null);

  // Reset practice state and errors when step code changes
  useEffect(() => {
    setPracticeSource('core');
    setPracticePage(1);
    setStartError(null);
  }, [code]);

  const {
    data: exercisesData,
    isLoading: isExercisesLoading,
    error: exercisesError,
    refetch: refetchExercises,
  } = useGrammarStepExercisesQuery(
    code,
    { source: practiceSource, page: practicePage, pageSize: 10, paged: true },
    currentTab === 'practice'
  );

  const handleStartCoreAttempt = async () => {
    if (!step || step.status === 'Locked' || startAttemptMutation.isPending) return;
    setStartError(null);

    try {
      const attempt = await startAttemptMutation.mutateAsync({
        kind: 'StepCore',
        stepCode: step.code,
      });
      navigate(`/student/grammar/attempts/${attempt.id}`);
    } catch (err: any) {
      setStartError(getGrammarErrorMessage(err, t));
    }
  };

  const handleStartLevelTest = async () => {
    if (!step || startAttemptMutation.isPending) return;
    setStartError(null);

    try {
      const attempt = await startAttemptMutation.mutateAsync({
        kind: 'LevelTest',
        cefrLevel: step.cefrLevel,
      });
      navigate(`/student/grammar/attempts/${attempt.id}`);
    } catch (err: any) {
      setStartError(getGrammarErrorMessage(err, t));
    }
  };

  const handleStartPracticeAttempt = async () => {
    if (!step || startAttemptMutation.isPending) return;
    setStartError(null);

    try {
      const attempt = await startAttemptMutation.mutateAsync({
        kind: 'Practice',
        stepCode: step.code,
      });
      navigate(`/student/grammar/attempts/${attempt.id}`);
    } catch (err: any) {
      setStartError(getGrammarErrorMessage(err, t));
    }
  };

  if (isLoading) {
    return <LoadingState message={t('grammar.loadingStepContent', { code })} />;
  }

  if (error || !step) {
    const status = (error as any)?.response?.status;
    const isNotFound = status === 404;
    const isForbidden = status === 403;

    return (
      <div className="p-6 max-w-4xl mx-auto space-y-4">
        <div className="p-4 bg-rose-50 border border-rose-200 text-rose-800 rounded-2xl text-xs space-y-3">
          <div className="font-bold text-sm">
            {isNotFound
              ? t('grammar.stepNotFound', { code })
              : isForbidden
              ? t('grammar.stepForbidden')
              : t('grammar.stepLoadError')}
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

  const isStepLocked = step.status === 'Locked';
  const isStepPassed = step.status === 'Passed';

  // Look up level from roadmap query to check canTakeLevelTest
  const levelInfo = roadmapData?.levels?.find((lvl) => lvl.level === step.cefrLevel);
  const levelTestState = getLevelTestButtonState(Boolean(levelInfo?.canTakeLevelTest));

  return (
    <div className="p-4 sm:p-6 lg:p-8 max-w-5xl mx-auto space-y-6">
      {/* Top Header / Breadcrumb */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-slate-200">
        <div className="flex items-center gap-3">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => navigate('/student/grammar')}
            className="flex items-center gap-1.5 text-xs text-slate-600 hover:text-slate-900"
          >
            <ArrowLeft className="w-4 h-4" />
            {t('grammar.backToRoadmap')}
          </Button>

          <div>
            <div className="flex items-center gap-2">
              <span className="font-mono font-bold text-xs px-2 py-0.5 rounded bg-emerald-100 text-emerald-800">
                {step.code}
              </span>
              <span className="text-xs text-slate-500 font-medium">
                {t('grammar.levelTitle', { level: step.cefrLevel })}
              </span>
              {isStepPassed ? (
                <Badge variant="emerald" className="text-[10px]">
                  <CheckCircle2 className="w-3 h-3 mr-1" />
                  {t('grammar.passedBadge')}
                </Badge>
              ) : isStepLocked ? (
                <Badge variant="default" className="text-[10px] text-slate-500">
                  <Lock className="w-2.5 h-2.5 mr-1" />
                  {t('grammar.stepLockedNotice')}
                </Badge>
              ) : (
                <Badge variant="sky" className="text-[10px]">
                  {t('grammar.openBadge')}
                </Badge>
              )}
            </div>
            <h1 className="text-xl sm:text-2xl font-extrabold text-slate-900 mt-1">
              {step.titleVi}
            </h1>
            <p className="text-xs sm:text-sm text-slate-500 italic">
              {step.titleEn}
            </p>
          </div>
        </div>

        {/* Best Score or Review Badge */}
        <div className="flex items-center gap-2 self-start sm:self-center">
          {step.isLevelReview && (
            <Badge variant="amber" className="text-xs">
              {t('grammar.levelReview')}
            </Badge>
          )}

          {step.bestScore100 !== null && step.bestScore100 !== undefined && (
            <div className="px-3 py-1.5 rounded-xl bg-emerald-50 border border-emerald-200 text-emerald-800 text-xs font-semibold">
              {t('grammar.bestScoreWithPercent', { score: Math.round(step.bestScore100) })}
            </div>
          )}
        </div>
      </div>

      {/* Semantic Tabs (role="tablist" & role="tab" with aria-selected) */}
      <div
        role="tablist"
        aria-label={t('grammar.tabsLabel')}
        className="flex items-center gap-2 border-b border-slate-200 overflow-x-auto"
      >
        <button
          type="button"
          role="tab"
          id="grammar-tab-theory"
          aria-controls="grammar-tabpanel-theory"
          aria-selected={currentTab === 'theory'}
          onClick={() => setTab('theory')}
          className={`flex items-center gap-2 pb-3 px-3 sm:px-4 font-bold text-xs sm:text-sm border-b-2 transition-all cursor-pointer whitespace-nowrap ${
            currentTab === 'theory'
              ? 'border-emerald-600 text-emerald-700'
              : 'border-transparent text-slate-500 hover:text-slate-800'
          }`}
        >
          <BookOpen className="w-4 h-4" />
          {t('grammar.theory')}
        </button>

        <button
          type="button"
          role="tab"
          id="grammar-tab-exercises"
          aria-controls="grammar-tabpanel-exercises"
          aria-selected={currentTab === 'exercises'}
          onClick={() => setTab('exercises')}
          className={`flex items-center gap-2 pb-3 px-3 sm:px-4 font-bold text-xs sm:text-sm border-b-2 transition-all cursor-pointer whitespace-nowrap ${
            currentTab === 'exercises'
              ? 'border-emerald-600 text-emerald-700'
              : 'border-transparent text-slate-500 hover:text-slate-800'
          }`}
        >
          <CheckCircle2 className="w-4 h-4" />
          {t('grammar.coreExercisesCount', { count: step.coreExerciseCount })}
        </button>

        <button
          type="button"
          role="tab"
          id="grammar-tab-practice"
          aria-controls="grammar-tabpanel-practice"
          aria-selected={currentTab === 'practice'}
          onClick={() => setTab('practice')}
          className={`flex items-center gap-2 pb-3 px-3 sm:px-4 font-bold text-xs sm:text-sm border-b-2 transition-all cursor-pointer whitespace-nowrap ${
            currentTab === 'practice'
              ? 'border-emerald-600 text-emerald-700'
              : 'border-transparent text-slate-500 hover:text-slate-800'
          }`}
        >
          <Layers className="w-4 h-4" />
          {t('grammar.practiceExercisesCount', { count: step.communityExerciseCount + step.myExerciseCount })}
        </button>
      </div>

      {/* Error alert if start attempt failed */}
      {startError && (
        <div className="p-4 bg-rose-50 border border-rose-200 text-rose-800 rounded-2xl text-xs flex items-center justify-between">
          <div className="flex items-center gap-2">
            <AlertCircle className="w-4 h-4 shrink-0" />
            <span>{startError}</span>
          </div>
          <button
            type="button"
            onClick={() => setStartError(null)}
            className="text-xs font-bold underline ml-2 cursor-pointer"
            aria-label={t('grammar.closeNotification')}
          >
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {/* Tab 1: Theory */}
      {currentTab === 'theory' && (
        <div
          role="tabpanel"
          id="grammar-tabpanel-theory"
          aria-labelledby="grammar-tab-theory"
          className="pt-2"
        >
          {step.theory ? (
            <TheoryRenderer
              theory={step.theory}
              prevCode={step.prevCode}
              nextCode={step.nextCode}
            />
          ) : (
            <EmptyState
              title={t('grammar.emptyExercises')}
              description={t('grammar.emptyExercisesDesc')}
            />
          )}
        </div>
      )}

      {/* Tab 2: Core exercises */}
      {currentTab === 'exercises' && (
        <div
          role="tabpanel"
          id="grammar-tabpanel-exercises"
          aria-labelledby="grammar-tab-exercises"
          className="pt-4 max-w-2xl mx-auto space-y-6"
        >
          <div className="bg-white border border-slate-200/90 rounded-3xl p-6 sm:p-8 shadow-xs space-y-6 text-center">
            <div className="w-16 h-16 rounded-2xl bg-emerald-100 text-emerald-800 flex items-center justify-center mx-auto shadow-2xs">
              <Award className="w-8 h-8" />
            </div>

            <div className="space-y-2">
              <h3 className="text-lg sm:text-xl font-bold text-slate-900">
                {t('grammar.coreInfoTitle', { code: step.code })}
              </h3>
              <p className="text-xs sm:text-sm text-slate-600 max-w-md mx-auto leading-relaxed">
                {t('grammar.coreInfoDesc')}
              </p>
            </div>

            <div className="grid grid-cols-2 sm:grid-cols-3 gap-3 p-4 rounded-2xl bg-slate-50 border border-slate-200/70 text-left">
              <div>
                <div className="text-[11px] text-slate-500 font-medium">{t('grammar.totalExercisesCount', { count: step.coreExerciseCount })}</div>
                <div className="text-base font-extrabold text-slate-900">
                  {step.coreExerciseCount}
                </div>
              </div>
              <div>
                <div className="text-[11px] text-slate-500 font-medium">{t('grammar.passingScoreRule')}</div>
                <div className="text-base font-extrabold text-emerald-700">≥ 70%</div>
              </div>
              <div className="col-span-2 sm:col-span-1">
                <div className="text-[11px] text-slate-500 font-medium">{t('grammar.statusLabel')}</div>
                <div className="text-base font-extrabold text-slate-800">
                  {isStepPassed ? t('grammar.passedBadge') : isStepLocked ? t('grammar.locked') : t('grammar.failed')}
                </div>
              </div>
            </div>

            {isStepLocked && (
              <div className="p-3.5 rounded-2xl bg-amber-50 border border-amber-200 text-amber-900 text-xs flex items-start gap-2 text-left">
                <Lock className="w-4 h-4 shrink-0 mt-0.5 text-amber-700" />
                <div>
                  <strong>{t('grammar.stepLockedNotice')}:</strong> {t('grammar.stepLockedWarning')}
                </div>
              </div>
            )}

            {/* Action buttons: ALWAYS show StepCore button; if isLevelReview, also show secondary Level Test button */}
            <div className="pt-2 flex flex-col sm:flex-row items-center justify-center gap-3 flex-wrap">
              <Button
                variant="primary"
                onClick={() => void handleStartCoreAttempt()}
                disabled={startAttemptMutation.isPending || isStepLocked || step.coreExerciseCount === 0}
                className="bg-emerald-600 hover:bg-emerald-700 text-white font-bold px-6 py-3 rounded-2xl text-sm shadow-md flex items-center gap-2"
              >
                <PlayCircle className="w-5 h-5" />
                {startAttemptMutation.isPending
                  ? t('grammar.initializing')
                  : isStepPassed
                  ? t('grammar.retryAttempt')
                  : t('grammar.startAttempt')}
              </Button>

              {step.isLevelReview && (
                <div className="flex flex-col items-center">
                  <Button
                    variant="outline"
                    onClick={() => void handleStartLevelTest()}
                    disabled={startAttemptMutation.isPending || levelTestState.disabled}
                    className={`font-bold px-6 py-3 rounded-2xl text-sm shadow-xs flex items-center gap-2 ${
                      !levelTestState.disabled
                        ? 'border-amber-500 text-amber-800 hover:bg-amber-50'
                        : 'text-slate-400 border-slate-200 cursor-not-allowed'
                    }`}
                  >
                    <Award className="w-5 h-5" />
                    {startAttemptMutation.isPending ? t('grammar.initializing') : t('grammar.levelTestButton', { level: step.cefrLevel })}
                  </Button>

                  {levelTestState.disabled && levelTestState.reasonKey && (
                    <p className="text-[11px] text-amber-800 bg-amber-50/90 border border-amber-200/80 rounded-xl px-2.5 py-1.5 mt-2 max-w-xs text-center">
                      {t(levelTestState.reasonKey)}
                    </p>
                  )}
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Tab 3: Practice exercises */}
      {currentTab === 'practice' && (
        <div
          role="tabpanel"
          id="grammar-tabpanel-practice"
          aria-labelledby="grammar-tab-practice"
          className="pt-2 space-y-6"
        >
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 flex-wrap">
            {/* Filter by source with flex-wrap */}
            <div className="flex flex-wrap items-center gap-2 p-1 rounded-xl bg-slate-100/90 text-xs font-semibold">
              <button
                type="button"
                onClick={() => {
                  setPracticeSource('core');
                  setPracticePage(1);
                }}
                className={`px-3 py-1.5 rounded-lg transition-all cursor-pointer ${
                  practiceSource === 'core'
                    ? 'bg-white text-emerald-800 shadow-2xs font-bold'
                    : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                {t('grammar.filterCore', { count: step.coreExerciseCount })}
              </button>
              <button
                type="button"
                onClick={() => {
                  setPracticeSource('community');
                  setPracticePage(1);
                }}
                className={`px-3 py-1.5 rounded-lg transition-all cursor-pointer ${
                  practiceSource === 'community'
                    ? 'bg-white text-emerald-800 shadow-2xs font-bold'
                    : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                {t('grammar.filterCommunity', { count: step.communityExerciseCount })}
              </button>
              <button
                type="button"
                onClick={() => {
                  setPracticeSource('mine');
                  setPracticePage(1);
                }}
                className={`px-3 py-1.5 rounded-lg transition-all cursor-pointer ${
                  practiceSource === 'mine'
                    ? 'bg-white text-emerald-800 shadow-2xs font-bold'
                    : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                {t('grammar.filterMine', { count: step.myExerciseCount })}
              </button>
            </div>

            {/* Action buttons */}
            <div className="flex items-center gap-3 flex-wrap">
              <Button
                variant="outline"
                size="sm"
                onClick={() => void handleStartPracticeAttempt()}
                disabled={startAttemptMutation.isPending}
                className="text-xs flex items-center gap-1.5"
              >
                <PlayCircle className="w-4 h-4 text-emerald-600" />
                {t('grammar.practiceRandom10')}
              </Button>

              <Button
                variant="primary"
                size="sm"
                onClick={() => setIsGenerateDialogOpen(true)}
                className="bg-emerald-600 hover:bg-emerald-700 text-white text-xs flex items-center gap-1.5"
              >
                <Sparkles className="w-4 h-4" />
                {t('grammar.generateWithAi')}
              </Button>
            </div>
          </div>

          {/* Exercises List / Loading / Error */}
          {isExercisesLoading ? (
            <LoadingState message={t('grammar.loadingExercises')} />
          ) : exercisesError ? (
            <div className="p-6 rounded-2xl bg-rose-50 border border-rose-200 text-rose-800 text-xs space-y-2">
              <div className="font-bold text-sm">{t('grammar.loadPracticeExercisesError')}</div>
              <div>{getGrammarErrorMessage(exercisesError, t)}</div>
              <Button variant="outline" size="sm" onClick={() => void refetchExercises()}>
                {t('grammar.retry')}
              </Button>
            </div>
          ) : !exercisesData || exercisesData.items.length === 0 ? (
            <div className="p-8 bg-slate-50 rounded-2xl text-center border border-slate-200">
              <div className="text-slate-500 text-xs">
                {practiceSource === 'mine'
                  ? t('grammar.noMyExercisesInStep')
                  : t('grammar.emptyExercises')}
              </div>
            </div>
          ) : (
            <div className="space-y-3">
              <div className="grid gap-3 sm:grid-cols-2">
                {exercisesData.items.map((ex: GrammarExerciseLearnerDto) => (
                  <div
                    key={ex.id}
                    className="p-4 rounded-2xl border border-slate-200 bg-white shadow-2xs space-y-2 flex flex-col justify-between"
                  >
                    <div className="space-y-1.5">
                      <div className="flex items-center justify-between gap-2">
                        <span className="font-mono text-[11px] font-bold px-2 py-0.5 rounded bg-slate-100 text-slate-800">
                          {t(`grammar.exerciseTypes.${ex.type}` as any)}
                        </span>

                        <div className="flex items-center gap-1.5">
                          {ex.difficulty ? (
                            <span className="text-[10px] px-1.5 py-0.5 rounded bg-slate-100 text-slate-600 font-semibold">
                              {t('grammar.difficultyLabel')} {ex.difficulty}
                            </span>
                          ) : null}

                          {practiceSource === 'mine' && (
                            <Badge
                              variant={
                                ex.moderation === 'Approved'
                                  ? 'emerald'
                                  : ex.moderation === 'Pending'
                                  ? 'sky'
                                  : ex.moderation === 'Rejected'
                                  ? 'rose'
                                  : 'default'
                              }
                              className="text-[10px]"
                            >
                              {ex.moderation === 'Approved'
                                ? t('grammar.modApproved')
                                : ex.moderation === 'Pending'
                                ? t('grammar.modPending')
                                : ex.moderation === 'Rejected'
                                ? t('grammar.modRejected')
                                : t('grammar.privateOption')}
                            </Badge>
                          )}
                        </div>
                      </div>

                      <div className="font-bold text-slate-900 text-xs sm:text-sm">
                        {ex.title || t('grammar.exerciseFallbackTitle', { type: t(`grammar.exerciseTypes.${ex.type}` as any) })}
                      </div>

                      {ex.instructions && (
                        <div className="text-slate-600 text-xs line-clamp-2">
                          {ex.instructions}
                        </div>
                      )}
                    </div>

                    <div className="pt-2 border-t border-slate-100 flex items-center justify-between text-[11px] text-slate-400">
                      <span>{t('grammar.maxPointsLabel', { points: ex.maxPoints })}</span>
                      {ex.authorName && (
                        <span className="flex items-center gap-1">
                          <User className="w-3 h-3" />
                          {ex.authorName}
                        </span>
                      )}
                    </div>
                  </div>
                ))}
              </div>

              {/* Pagination controls with flex-wrap */}
              {exercisesData.totalPages > 1 && (
                <div className="flex flex-wrap items-center justify-between gap-3 pt-4 border-t border-slate-200 text-xs">
                  <span className="text-slate-500">
                    {t('grammar.paginationSummary', {
                      page: exercisesData.page,
                      totalPages: exercisesData.totalPages,
                      totalCount: exercisesData.totalCount,
                    })}
                  </span>
                  <div className="flex items-center gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={exercisesData.page <= 1}
                      onClick={() => setPracticePage((p) => Math.max(1, p - 1))}
                    >
                      <ChevronLeft className="w-4 h-4 mr-1" />
                      {t('grammar.prevPage')}
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={exercisesData.page >= exercisesData.totalPages}
                      onClick={() => setPracticePage((p) => p + 1)}
                    >
                      {t('grammar.nextPage')}
                      <ChevronRight className="w-4 h-4 ml-1" />
                    </Button>
                  </div>
                </div>
              )}
            </div>
          )}
        </div>
      )}

      {/* AI Exercise Generation Dialog */}
      <GenerateGrammarExercisesDialog
        isOpen={isGenerateDialogOpen}
        onClose={() => setIsGenerateDialogOpen(false)}
        stepCode={step.code}
        stepCefrLevel={step.cefrLevel}
        onSuccess={() => {
          refetch();
        }}
      />
    </div>
  );
};
