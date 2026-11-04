import React, { useState, useMemo } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  CheckCircle2,
  Lock,
  Unlock,
  PlayCircle,
  Award,
  ArrowRight,
  Layers,
  AlertCircle,
  X,
} from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { GrammarLevelRail, levelProgress } from '../components/GrammarLevelRail';
import { GrammarStepPath } from '../components/GrammarStepPath';
import { LoadingState } from '@/shared/components/ui/LoadingState';
import { EmptyState } from '@/shared/components/ui/EmptyState';
import {
  useGrammarRoadmapQuery,
  useStartGrammarAttemptMutation,
} from '../hooks/useGrammarQueries';
import { getGrammarErrorMessage, getLevelTestButtonState } from '../utils/grammarHelpers';
import type { GrammarLevelDto } from '../types/grammar.types';

export const GrammarRoadmapPage: React.FC = () => {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { data: roadmap, isLoading, error, refetch } = useGrammarRoadmapQuery();
  const startAttemptMutation = useStartGrammarAttemptMutation();

  const [userSelectedLevel, setSelectedLevel] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  // Find the first Unlocked step that is not Passed
  const nextStudyStep = useMemo(() => {
    if (!roadmap?.levels) return null;
    for (const lvl of roadmap.levels) {
      for (const step of lvl.steps) {
        if (step.status === 'Unlocked') {
          return { step, level: lvl.level };
        }
      }
    }
    // Fallback: first step of first unlocked level
    for (const lvl of roadmap.levels) {
      if (lvl.isUnlocked && lvl.steps.length > 0) {
        return { step: lvl.steps[0], level: lvl.level };
      }
    }
    return null;
  }, [roadmap]);

  // Selected level tab defaults to the level of the step the learner should continue with.
  const selectedLevel = userSelectedLevel ?? nextStudyStep?.level ?? roadmap?.levels[0]?.level ?? '';

  const handleStartLevelTest = async (level: GrammarLevelDto) => {
    if (!level.canTakeLevelTest || startAttemptMutation.isPending) return;
    setActionError(null);

    try {
      const attempt = await startAttemptMutation.mutateAsync({
        kind: 'LevelTest',
        cefrLevel: level.level,
      });
      navigate(`/student/grammar/attempts/${attempt.id}`);
    } catch (err: any) {
      setActionError(getGrammarErrorMessage(err, t));
    }
  };

  if (isLoading) {
    return <LoadingState message={t('grammar.loadingRoadmap')} />;
  }

  if (error) {
    return (
      <div className="max-w-4xl mx-auto space-y-4">
        <div className="p-4 bg-rose-50 border border-rose-200 text-rose-800 rounded-2xl text-xs space-y-2">
          <div className="font-bold text-sm">{t('grammar.errors.generic')}</div>
          <div>{getGrammarErrorMessage(error, t)}</div>
          <Button variant="outline" size="sm" onClick={() => void refetch()}>
            {t('grammar.retry')}
          </Button>
        </div>
      </div>
    );
  }

  if (!roadmap || !roadmap.levels || roadmap.levels.length === 0) {
    return (
      <div className="max-w-4xl mx-auto">
        <EmptyState
          title={t('grammar.emptyExercises')}
          description={t('grammar.emptyExercisesDesc')}
        />
      </div>
    );
  }

  const selectedLevelData =
    roadmap.levels.find((l) => l.level === selectedLevel) ?? roadmap.levels[0];
  const selected = levelProgress(selectedLevelData);
  const levelTestState = getLevelTestButtonState(selectedLevelData.canTakeLevelTest);
  const totalSteps = roadmap.levels.reduce((sum, l) => sum + l.steps.length, 0);
  const totalPassed = roadmap.levels.reduce((sum, l) => sum + levelProgress(l).passed, 0);
  const overallPercent = totalSteps > 0 ? Math.round((totalPassed / totalSteps) * 100) : 0;

  return (
    <div className="max-w-5xl mx-auto space-y-6">
      {/* Action Error Banner */}
      {actionError && (
        <div className="p-4 rounded-2xl bg-rose-50 border border-rose-200 text-rose-800 text-xs flex items-start justify-between gap-3 shadow-xs">
          <div className="flex items-start gap-2.5">
            <AlertCircle className="w-4 h-4 shrink-0 mt-0.5 text-rose-600" />
            <span className="font-medium">{actionError}</span>
          </div>
          <button
            type="button"
            onClick={() => setActionError(null)}
            className="text-rose-500 hover:text-rose-800 p-0.5 rounded cursor-pointer"
            aria-label={t('grammar.closeNotification')}
          >
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {/* Hero */}
      <section className="relative overflow-hidden rounded-3xl bg-linear-to-br from-emerald-800 via-emerald-700 to-teal-700 text-white p-5 sm:p-8 shadow-sm">
        <div aria-hidden="true" className="pointer-events-none absolute inset-0 overflow-hidden">
          <div className="absolute right-0 -top-16 translate-x-1/4 w-64 h-64 rounded-full bg-white/5" />
          <div className="absolute right-24 bottom-0 translate-y-1/2 w-48 h-48 rounded-full bg-teal-400/10" />
        </div>
        <div className="relative grid gap-6 md:grid-cols-[1fr_auto] md:items-center min-w-0">
          <div className="space-y-2 max-w-xl">
            <div className="flex items-center gap-2">
              <span className="px-2.5 py-0.5 rounded-full bg-white/15 text-emerald-50 text-xs font-semibold">
                {t('grammar.cefrRoadmapBadge')}
              </span>
              <span className="text-emerald-200 text-xs">A1 → C2</span>
            </div>
            <h1 className="text-2xl sm:text-3xl font-extrabold tracking-tight">{t('grammar.title')}</h1>
            <p className="text-emerald-100 text-sm leading-relaxed">{t('grammar.subtitle')}</p>
            <div className="flex flex-wrap items-center gap-2.5 pt-3">
              {nextStudyStep && (
                <Link
                  to={`/student/grammar/${nextStudyStep.step.code}`}
                  className="inline-flex items-center gap-2 rounded-xl bg-white px-4 py-2.5 text-sm font-bold text-emerald-800 no-underline shadow-md transition-colors hover:bg-emerald-50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-white"
                >
                  <PlayCircle className="w-4 h-4 text-emerald-600" aria-hidden="true" />
                  {t('grammar.continueStudyWithCode', { code: nextStudyStep.step.code })}
                  <ArrowRight className="w-4 h-4" aria-hidden="true" />
                </Link>
              )}
              <Link
                to="/student/grammar/my-exercises"
                className="inline-flex items-center gap-2 rounded-xl bg-white/10 px-4 py-2.5 text-sm font-semibold text-white no-underline ring-1 ring-inset ring-white/30 transition-colors hover:bg-white/20 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-white"
              >
                <Layers className="w-4 h-4" aria-hidden="true" />
                {t('grammar.myExercises')}
              </Link>
            </div>
          </div>

          <div className="rounded-2xl bg-white/10 ring-1 ring-inset ring-white/20 p-3 sm:p-4 md:w-60 space-y-2">
            <div className="text-xs font-semibold text-emerald-100">{t('grammar.overallProgress')}</div>
            <div className="flex items-baseline gap-1.5">
              <span className="text-3xl font-extrabold">{totalPassed}</span>
              <span className="text-sm text-emerald-100">/ {totalSteps}</span>
            </div>
            <div
              className="h-2 rounded-full bg-white/20 overflow-hidden"
              role="progressbar"
              aria-valuemin={0}
              aria-valuemax={100}
              aria-valuenow={overallPercent}
              aria-label={t('grammar.overallProgress')}
            >
              <div className="h-full rounded-full bg-amber-300" style={{ width: `${overallPercent}%` }} />
            </div>
            <div className="text-[11px] text-emerald-100">{t('grammar.overallPassed', { percent: overallPercent })}</div>
          </div>
        </div>
      </section>

      {/* Levels */}
      <GrammarLevelRail
        levels={roadmap.levels}
        selectedLevel={selectedLevelData.level}
        currentLevel={nextStudyStep?.level}
        onSelect={setSelectedLevel}
      />

      <section
        id={`level-panel-${selectedLevelData.level}`}
        role="tabpanel"
        aria-labelledby={`level-tab-${selectedLevelData.level}`}
        className="rounded-3xl border border-slate-200 bg-white shadow-xs overflow-hidden"
      >
        <div className="grid gap-4 p-4 sm:p-6 md:grid-cols-[1fr_auto] md:items-center border-b border-slate-100">
          <div className="space-y-2 min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              <h2 className="text-lg sm:text-xl font-extrabold text-slate-900">
                {t('grammar.levelTitle', { level: selectedLevelData.level })}
              </h2>
              <span className="text-sm text-slate-500">
                {t(`grammar.levelNames.${selectedLevelData.level}`)}
              </span>
              {selectedLevelData.isPassed ? (
                <Badge variant="emerald" className="flex items-center gap-1 text-[11px]">
                  <CheckCircle2 className="w-3 h-3" />
                  {t('grammar.completed')}
                </Badge>
              ) : selectedLevelData.isUnlocked ? (
                <Badge variant="sky" className="flex items-center gap-1 text-[11px]">
                  <Unlock className="w-3 h-3" />
                  {t('grammar.inProgress')}
                </Badge>
              ) : (
                <Badge variant="default" className="flex items-center gap-1 text-[11px]">
                  <Lock className="w-3 h-3" />
                  {t('grammar.locked')}
                </Badge>
              )}
            </div>
            <div className="flex items-center gap-3">
              <div className="flex-1 max-w-xs h-2 rounded-full bg-slate-100 overflow-hidden">
                <div className="h-full rounded-full bg-emerald-500 transition-all duration-500" style={{ width: `${selected.percent}%` }} />
              </div>
              <span className="text-xs font-semibold text-slate-600 whitespace-nowrap">
                {t('grammar.progressSteps', { passed: selected.passed, total: selected.total })}
              </span>
            </div>
          </div>

          <div className="rounded-2xl bg-amber-50/70 border border-amber-200/70 p-3 sm:p-4 md:w-72 space-y-2">
            <div className="flex items-start gap-2.5">
              <span className="flex items-center justify-center w-8 h-8 rounded-xl bg-amber-100 text-amber-700 shrink-0">
                <Award className="w-4 h-4" aria-hidden="true" />
              </span>
              <p className="text-xs text-amber-900 leading-relaxed">{t('grammar.levelTestDesc')}</p>
            </div>
            <Button
              variant="secondary"
              size="sm"
              disabled={levelTestState.disabled || startAttemptMutation.isPending}
              onClick={() => void handleStartLevelTest(selectedLevelData)}
              className="w-full"
            >
              <Award className="w-3.5 h-3.5" />
              {t('grammar.levelTestButton', { level: selectedLevelData.level })}
            </Button>
            {levelTestState.disabled && levelTestState.reasonKey && (
              <p className="text-[11px] text-amber-800">{t(levelTestState.reasonKey)}</p>
            )}
          </div>
        </div>

        <div className="px-3 sm:px-6 py-6 bg-linear-to-b from-emerald-50/40 to-white">
          <GrammarStepPath
            level={selectedLevelData.level}
            steps={selectedLevelData.steps}
            currentStepCode={nextStudyStep?.step.code}
          />
        </div>
      </section>
    </div>
  );
};
