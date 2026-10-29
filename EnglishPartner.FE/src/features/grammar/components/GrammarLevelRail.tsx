import React from 'react';
import { useTranslation } from 'react-i18next';
import { Check, Lock } from 'lucide-react';
import type { GrammarLevelDto } from '../types/grammar.types';

export function levelProgress(level: GrammarLevelDto): { passed: number; total: number; percent: number } {
  const total = level.steps.length;
  const passed = level.steps.filter((s) => s.status === 'Passed').length;
  return { passed, total, percent: total > 0 ? Math.round((passed / total) * 100) : 0 };
}

const RING_RADIUS = 22;
const RING_LENGTH = 2 * Math.PI * RING_RADIUS;

interface GrammarLevelRailProps {
  levels: GrammarLevelDto[];
  selectedLevel: string;
  currentLevel?: string | null;
  onSelect: (level: string) => void;
}

/** CEFR level selector: a connected rail of six level tiles with progress rings (ARIA tabs). */
export const GrammarLevelRail: React.FC<GrammarLevelRailProps> = ({ levels, selectedLevel, currentLevel, onSelect }) => {
  const { t } = useTranslation();

  const handleKeyDown = (e: React.KeyboardEvent, index: number) => {
    const delta = e.key === 'ArrowRight' ? 1 : e.key === 'ArrowLeft' ? -1 : 0;
    if (!delta) return;
    e.preventDefault();
    const next = levels[(index + delta + levels.length) % levels.length];
    onSelect(next.level);
    document.getElementById(`level-tab-${next.level}`)?.focus();
  };

  return (
    <div className="relative">
      <div aria-hidden="true" className="hidden sm:block absolute left-[8%] right-[8%] top-9 h-1 rounded-full bg-slate-200" />
      <div
        role="tablist"
        aria-label={t('grammar.levelsLabel')}
        className="relative grid grid-cols-3 sm:grid-cols-6 gap-2 sm:gap-3"
      >
        {levels.map((level, index) => {
          const { percent } = levelProgress(level);
          const isSelected = level.level === selectedLevel;
          const isCurrent = level.level === currentLevel;
          const offset = RING_LENGTH * (1 - percent / 100);

          return (
            <button
              key={level.level}
              id={`level-tab-${level.level}`}
              type="button"
              role="tab"
              aria-selected={isSelected}
              aria-controls={`level-panel-${level.level}`}
              tabIndex={isSelected ? 0 : -1}
              onClick={() => onSelect(level.level)}
              onKeyDown={(e) => handleKeyDown(e, index)}
              className={`group flex flex-col items-center gap-1.5 rounded-2xl px-2 pt-2 pb-3 cursor-pointer transition-all focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-500 ${
                isSelected ? 'bg-white shadow-md ring-1 ring-emerald-200' : 'hover:bg-white/70'
              }`}
            >
              <span className="relative flex items-center justify-center w-14 h-14">
                <svg aria-hidden="true" viewBox="0 0 52 52" className="absolute inset-0 w-full h-full -rotate-90">
                  <circle cx="26" cy="26" r={RING_RADIUS} fill="white" strokeWidth="4" className="stroke-slate-200" />
                  {percent > 0 && (
                    <circle
                      cx="26"
                      cy="26"
                      r={RING_RADIUS}
                      fill="none"
                      strokeWidth="4"
                      strokeLinecap="round"
                      strokeDasharray={RING_LENGTH}
                      strokeDashoffset={offset}
                      className="stroke-emerald-500 transition-[stroke-dashoffset] duration-500"
                    />
                  )}
                </svg>
                <span
                  className={`relative flex items-center justify-center w-10 h-10 rounded-full text-sm font-extrabold ${
                    level.isPassed
                      ? 'bg-emerald-600 text-white'
                      : level.isUnlocked
                      ? 'bg-emerald-50 text-emerald-800'
                      : 'bg-slate-100 text-slate-400'
                  }`}
                >
                  {level.isPassed ? <Check className="w-5 h-5" strokeWidth={3} aria-hidden="true" /> : level.level}
                </span>
                {!level.isUnlocked && (
                  <span className="absolute -bottom-0.5 -right-0.5 flex items-center justify-center w-5 h-5 rounded-full bg-white border border-slate-200 text-slate-400">
                    <Lock className="w-2.5 h-2.5" aria-hidden="true" />
                  </span>
                )}
              </span>
              <span className={`text-xs font-bold ${isSelected ? 'text-emerald-800' : 'text-slate-700'}`}>
                {level.level}
              </span>
              <span
                className={`text-[10px] font-semibold leading-none ${
                  level.isPassed ? 'text-emerald-600' : isCurrent ? 'text-amber-600' : 'text-slate-400'
                }`}
              >
                {level.isPassed
                  ? t('grammar.completed')
                  : isCurrent
                  ? t('grammar.inProgress')
                  : level.isUnlocked
                  ? `${percent}%`
                  : t('grammar.locked')}
              </span>
            </button>
          );
        })}
      </div>
    </div>
  );
};
