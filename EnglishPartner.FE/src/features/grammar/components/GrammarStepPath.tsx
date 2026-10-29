import React from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Check, Lock, Play, Trophy } from 'lucide-react';
import type { GrammarStepRoadmapItemDto } from '../types/grammar.types';

const ROW_HEIGHT = 136;
// Horizontal offsets (fraction of the amplitude) repeated along the path to make it wind left and right.
const WAVE = [0, 0.6, 1, 0.6, 0, -0.6, -1, -0.6];
const AMPLITUDE = 27;

type CardSide = 'left' | 'right';

export interface StepPathPoint {
  x: number;
  y: number;
  side: CardSide;
}

/** Computes node centres (x in %, y in px) and which side each step card sits on. */
export function computeStepPathPoints(count: number): StepPathPoint[] {
  return Array.from({ length: count }, (_, i) => {
    const offset = WAVE[i % WAVE.length];
    const nextOffset = WAVE[(i + 1) % WAVE.length];
    const direction = offset !== 0 ? offset : -nextOffset;
    return {
      x: 50 + offset * AMPLITUDE,
      y: i * ROW_HEIGHT + ROW_HEIGHT / 2,
      side: direction > 0 ? 'left' : 'right',
    };
  });
}

function segmentPath(a: StepPathPoint, b: StepPathPoint): string {
  const midY = (a.y + b.y) / 2;
  return `M ${a.x} ${a.y} C ${a.x} ${midY}, ${b.x} ${midY}, ${b.x} ${b.y}`;
}

interface GrammarStepPathProps {
  level: string;
  steps: GrammarStepRoadmapItemDto[];
  currentStepCode?: string | null;
}

export const GrammarStepPath: React.FC<GrammarStepPathProps> = ({ level, steps, currentStepCode }) => {
  const { t } = useTranslation();
  const points = computeStepPathPoints(steps.length);
  const height = steps.length * ROW_HEIGHT;

  return (
    <ol
      aria-label={t('grammar.stepsPathLabel', { level })}
      className="relative list-none m-0 p-0"
      style={{ height }}
    >
      {/* Desktop: winding SVG path. Coordinates use x in % (viewBox width 100) and y in px. */}
      <svg
        aria-hidden="true"
        className="absolute inset-0 hidden sm:block w-full h-full pointer-events-none"
        viewBox={`0 0 100 ${height}`}
        preserveAspectRatio="none"
      >
        {points.slice(1).map((p, i) => {
          const reached = steps[i + 1].status !== 'Locked';
          return (
            <path
              key={steps[i + 1].code}
              d={segmentPath(points[i], p)}
              fill="none"
              vectorEffect="non-scaling-stroke"
              strokeLinecap="round"
              strokeWidth={reached ? 6 : 4}
              strokeDasharray={reached ? undefined : '2 10'}
              className={reached ? 'stroke-emerald-400' : 'stroke-slate-300'}
            />
          );
        })}
      </svg>

      {/* Mobile: straight vertical line on the left. */}
      <div aria-hidden="true" className="sm:hidden absolute left-7 top-8 bottom-8 w-1 -translate-x-1/2 rounded-full bg-slate-200" />

      {steps.map((step, i) => {
        const point = points[i];
        const isPassed = step.status === 'Passed';
        const isLocked = step.status === 'Locked';
        const isCurrent = step.code === currentStepCode;
        const cardPosition =
          point.side === 'right'
            ? 'sm:left-[calc(var(--x)_+_48px)] sm:right-auto sm:w-[min(320px,calc(100%_-_var(--x)_-_60px))]'
            : 'sm:left-auto sm:right-[calc(100%_-_var(--x)_+_48px)] sm:w-[min(320px,calc(var(--x)_-_60px))]';

        return (
          <li
            key={step.code}
            className="absolute inset-x-0"
            style={{ top: i * ROW_HEIGHT, height: ROW_HEIGHT, ['--x' as string]: `${point.x}%` }}
          >
            {/* Node (decorative; the card is the interactive element). */}
            <div
              aria-hidden="true"
              className="absolute top-1/2 left-7 sm:left-[var(--x)] -translate-x-1/2 -translate-y-1/2"
            >
              {isCurrent && (
                <span className="absolute inset-0 rounded-full bg-emerald-400/40 animate-ping motion-reduce:hidden" />
              )}
              <div
                className={`relative flex items-center justify-center rounded-full font-extrabold shadow-sm transition-transform ${
                  isCurrent
                    ? 'w-14 h-14 bg-emerald-500 text-white ring-4 ring-emerald-200'
                    : isPassed
                    ? 'w-12 h-12 bg-emerald-600 text-white ring-4 ring-emerald-100'
                    : isLocked
                    ? 'w-11 h-11 bg-slate-100 text-slate-400 ring-4 ring-white border border-slate-200'
                    : 'w-12 h-12 bg-white text-emerald-700 ring-4 ring-emerald-50 border-2 border-emerald-400'
                }`}
              >
                {step.isLevelReview ? (
                  <Trophy className={isCurrent ? 'w-6 h-6' : 'w-5 h-5'} />
                ) : isCurrent ? (
                  <Play className="w-6 h-6 ml-0.5" fill="currentColor" />
                ) : isPassed ? (
                  <Check className="w-6 h-6" strokeWidth={3} />
                ) : isLocked ? (
                  <Lock className="w-4 h-4" />
                ) : (
                  <span className="text-sm">{step.order}</span>
                )}
              </div>
            </div>

            <Link
              to={`/student/grammar/${step.code}`}
              aria-current={isCurrent ? 'step' : undefined}
              className={`absolute top-1/2 -translate-y-1/2 left-16 right-0 block rounded-2xl border p-3.5 no-underline text-inherit transition-all hover:-translate-y-[calc(50%_+_2px)] hover:shadow-md focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-500 ${cardPosition} ${
                isCurrent
                  ? 'bg-white border-emerald-300 shadow-md shadow-emerald-100'
                  : isPassed
                  ? 'bg-white border-emerald-100 shadow-xs'
                  : isLocked
                  ? 'bg-slate-50/90 border-slate-200'
                  : 'bg-white border-slate-200 shadow-xs'
              }`}
            >
              <div className="flex items-center justify-between gap-2">
                <span className="font-mono text-[11px] font-bold text-slate-500">{step.code}</span>
                {isCurrent ? (
                  <span className="text-[10px] font-bold uppercase tracking-wide text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-full">
                    {t('grammar.currentBadge')}
                  </span>
                ) : isPassed ? (
                  <span className="text-[10px] font-bold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-full">
                    {step.bestScore100 != null
                      ? t('grammar.bestScoreWithPercent', { score: Math.round(step.bestScore100) })
                      : t('grammar.passedBadge')}
                  </span>
                ) : isLocked ? (
                  <span className="text-[10px] font-semibold text-slate-400 flex items-center gap-1">
                    <Lock className="w-2.5 h-2.5" aria-hidden="true" />
                    {t('grammar.stepLockedNotice')}
                  </span>
                ) : (
                  <span className="text-[10px] font-bold text-sky-700 bg-sky-50 px-2 py-0.5 rounded-full">
                    {t('grammar.openBadge')}
                  </span>
                )}
              </div>
              <div className={`mt-1 font-bold text-sm leading-snug line-clamp-2 ${isLocked ? 'text-slate-500' : 'text-slate-900'}`}>
                {step.titleVi}
              </div>
              <div className="text-xs text-slate-500 italic line-clamp-1">{step.titleEn}</div>
              {step.isLevelReview && (
                <span className="mt-1.5 inline-block text-[10px] font-bold text-amber-800 bg-amber-100 px-1.5 py-0.5 rounded">
                  {t('grammar.levelReview')}
                </span>
              )}
            </Link>
          </li>
        );
      })}
    </ol>
  );
};
