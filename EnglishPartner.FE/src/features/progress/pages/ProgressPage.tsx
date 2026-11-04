import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  TrendingUp,
  BookOpen,
  GraduationCap,
  Sparkles,
  Volume2,
  MessageSquare,
} from 'lucide-react';
import { useProgressOverviewQuery } from '../hooks/useProgressQueries';
import { StreakCard } from '../components/StreakCard';
import { ActivityChart } from '../components/ActivityChart';
import { SummaryCard } from '../components/SummaryCard';
import { LoadingState } from '@/shared/components/ui/LoadingState';
import { EmptyState } from '@/shared/components/ui/EmptyState';
import { FeedbackBanner } from '@/shared/components/ui/FeedbackBanner';
import { Button } from '@/shared/components/ui/Button';
import { extractApiErrorMessage } from '@/shared/api/httpClient';
import { cn } from '@/shared/lib/cn';
import type { ProgressOverviewDto } from '@/shared/api/openapi.types';

const DAY_OPTIONS = [7, 30, 90] as const;
type DayOption = (typeof DAY_OPTIONS)[number];

function isOverviewEmpty(data: ProgressOverviewDto): boolean {
  const hasNoStreak =
    data.currentStreakDays === 0 &&
    data.longestStreakDays === 0 &&
    data.activeDaysInRange === 0;

  const hasNoActivity = data.activity.every(
    (a) =>
      a.vocabularyReviews === 0 &&
      a.lessonAttempts === 0 &&
      a.grammarAttempts === 0 &&
      a.pronunciationAssessments === 0 &&
      a.tutorSessions === 0
  );

  const hasNoVocab =
    data.vocabulary.studied === 0 &&
    data.vocabulary.mastered === 0 &&
    data.vocabulary.dueForReviewNow === 0;

  const hasNoLessons = data.lessons.completedAttempts === 0;

  const hasNoGrammar =
    // currentStepCode is always set once steps are published (first step unlocked), so it is not a signal.
    !data.grammar.levels || data.grammar.levels.every((l) => l.passedSteps === 0);

  const hasNoSpeech = data.speech.assessments === 0;

  const hasNoTutor = data.liveTutor.sessions === 0;

  return (
    hasNoStreak &&
    hasNoActivity &&
    hasNoVocab &&
    hasNoLessons &&
    hasNoGrammar &&
    hasNoSpeech &&
    hasNoTutor
  );
}

export const ProgressPage: React.FC = () => {
  const { t } = useTranslation();
  const [days, setDays] = useState<DayOption>(30);

  const { data, isLoading, error, refetch } = useProgressOverviewQuery(days);

  return (
    <div className="space-y-6 max-w-6xl mx-auto py-2">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 border-b border-slate-200/80 pb-5">
        <div>
          <h1 className="text-xl sm:text-2xl font-black text-slate-900 tracking-tight flex items-center gap-2.5">
            <TrendingUp className="w-6 h-6 text-emerald-600" />
            {t('progress.title')}
          </h1>
          <p className="text-xs sm:text-sm text-slate-500 mt-1">
            {t('progress.subtitle')}
          </p>
        </div>

        {/* 7 / 30 / 90 Days Segmented Button */}
        <div
          role="group"
          aria-label={t('progress.rangeSelector')}
          className="inline-flex items-center p-1 rounded-2xl bg-slate-100 border border-slate-200/80 self-start sm:self-auto shrink-0 shadow-2xs"
        >
          {DAY_OPTIONS.map((opt) => (
            <button
              key={opt}
              type="button"
              aria-pressed={days === opt}
              onClick={() => setDays(opt)}
              className={cn(
                'px-3.5 py-1.5 text-xs font-bold rounded-xl transition-all cursor-pointer',
                days === opt
                  ? 'bg-white text-emerald-700 shadow-xs'
                  : 'text-slate-600 hover:text-slate-900'
              )}
            >
              {t(`progress.days${opt}`)}
            </button>
          ))}
        </div>
      </div>

      {/* States: Loading, Error, Empty, or Data */}
      {isLoading && (
        <LoadingState message={t('progress.loading')} />
      )}

      {!isLoading && error && (
        <div className="p-4 sm:p-6 rounded-3xl bg-white border border-slate-200/90 shadow-2xs space-y-4">
          <FeedbackBanner
            type="error"
            message={extractApiErrorMessage(error as any) || t('progress.errorTitle')}
          />
          <div className="flex justify-end">
            <Button variant="outline" size="sm" onClick={() => void refetch()}>
              {t('progress.retry')}
            </Button>
          </div>
        </div>
      )}

      {!isLoading && !error && data && isOverviewEmpty(data) && (
        <div className="p-4 sm:p-6 rounded-3xl bg-white border border-slate-200/90 shadow-2xs">
          <EmptyState
            illustration="no-plan"
            title={t('progress.emptyTitle')}
            description={t('progress.emptyDesc')}
            action={
              <div className="flex flex-wrap items-center justify-center gap-3 pt-2">
                <Link
                  to="/student/vocabulary"
                  className="px-4 py-2 rounded-xl bg-emerald-600 text-white font-bold text-xs hover:bg-emerald-700 transition-colors shadow-xs"
                >
                  {t('progress.startLearning')}
                </Link>
              </div>
            }
          />
        </div>
      )}

      {!isLoading && !error && data && !isOverviewEmpty(data) && (
        <div className="space-y-6">
          {/* 1. Streak Cards */}
          <StreakCard
            currentStreakDays={data.currentStreakDays}
            longestStreakDays={data.longestStreakDays}
            activeDaysInRange={data.activeDaysInRange}
            days={data.days}
          />

          {/* 2. Activity Chart */}
          <ActivityChart activity={data.activity} days={data.days} />

          {/* 3. Module Summaries */}
          <div className="space-y-4">
            <div>
              <h3 className="text-base font-black text-slate-900 tracking-tight">
                {t('progress.summaryTitle')}
              </h3>
              <p className="text-xs text-slate-500 mt-0.5">
                {t('progress.summarySubtitle')}
              </p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {/* Thẻ 1: Từ vựng */}
              <SummaryCard
                title={t('progress.vocabulary')}
                icon={<BookOpen className="w-5 h-5 text-emerald-600" />}
                href="/student/vocabulary"
                permission="vocabularies.read"
                stats={[
                  { label: t('progress.vocabStudied'), value: data.vocabulary.studied },
                  { label: t('progress.vocabMastered'), value: data.vocabulary.mastered },
                  { label: t('progress.vocabDue'), value: data.vocabulary.dueForReviewNow },
                  {
                    label: t('progress.vocabAverageMastery'),
                    value: `${Math.round(data.vocabulary.averageMastery)}%`,
                  },
                ]}
              />

              {/* Thẻ 2: Bài học */}
              <SummaryCard
                title={t('progress.lessons')}
                icon={<GraduationCap className="w-5 h-5 text-indigo-600" />}
                href="/student/lessons"
                stats={[
                  {
                    label: t('progress.lessonCompleted'),
                    value: data.lessons.completedAttempts,
                  },
                  {
                    label: t('progress.lessonAverageScore'),
                    value:
                      data.lessons.averageScore100 != null
                        ? t('progress.pointsOutOfHundred', {
                            score: data.lessons.averageScore100,
                          })
                        : '—',
                  },
                ]}
              />

              {/* Thẻ 3: Ngữ pháp */}
              <SummaryCard
                title={t('progress.grammar')}
                icon={<Sparkles className="w-5 h-5 text-amber-600" />}
                href="/student/grammar"
                permission="grammar.read"
                stats={[
                  {
                    label: t('progress.grammarLevels'),
                    value: `${data.grammar.levels.reduce(
                      (acc, l) => acc + l.passedSteps,
                      0
                    )} / ${data.grammar.levels.reduce((acc, l) => acc + l.totalSteps, 0)}`,
                  },
                  {
                    label: t('progress.grammarCurrentStep'),
                    value: data.grammar.currentStepCode
                      ? t('progress.stepCodeValue', { code: data.grammar.currentStepCode })
                      : '—',
                  },
                ]}
                grammarData={{
                  levels: data.grammar.levels,
                  currentStepCode: data.grammar.currentStepCode,
                }}
              />

              {/* Thẻ 4: Phát âm */}
              <SummaryCard
                title={t('progress.pronunciation')}
                icon={<Volume2 className="w-5 h-5 text-rose-600" />}
                href="/student/vocabulary"
                permission="vocabularies.read"
                stats={[
                  {
                    label: t('progress.speechAssessments'),
                    value: data.speech.assessments,
                  },
                  {
                    label: t('progress.speechAverageScore'),
                    value:
                      data.speech.averageScore != null
                        ? Number(data.speech.averageScore).toFixed(1)
                        : '—',
                  },
                  {
                    label: t('progress.speechAverageScoreInRange'),
                    value:
                      data.speech.averageScoreInRange != null
                        ? Number(data.speech.averageScoreInRange).toFixed(1)
                        : '—',
                  },
                ]}
              />

              {/* Thẻ 5: Live Tutor */}
              <SummaryCard
                title={t('progress.liveTutor')}
                icon={<MessageSquare className="w-5 h-5 text-violet-600" />}
                href="/student/tutor"
                permission="conversations.access"
                stats={[
                  {
                    label: t('progress.tutorSessions'),
                    value: data.liveTutor.sessions,
                  },
                  {
                    label: t('progress.tutorSessionsInRange'),
                    value: data.liveTutor.sessionsInRange,
                  },
                ]}
              />
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
