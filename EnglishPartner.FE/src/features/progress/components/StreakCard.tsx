import React from 'react';
import { useTranslation } from 'react-i18next';
import { Flame, Trophy, CalendarCheck } from 'lucide-react';
import { cn } from '@/shared/lib/cn';

export interface StreakCardProps {
  currentStreakDays: number;
  longestStreakDays: number;
  activeDaysInRange: number;
  days: number;
}

export const StreakCard: React.FC<StreakCardProps> = ({
  currentStreakDays,
  longestStreakDays,
  activeDaysInRange,
  days,
}) => {
  const { t } = useTranslation();
  const activePercentage = days > 0 ? Math.min(100, Math.round((activeDaysInRange / days) * 100)) : 0;

  return (
    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
      {/* Current Streak */}
      <div className="p-4 sm:p-5 rounded-3xl bg-white border border-slate-200/90 shadow-2xs flex items-center gap-4 transition-all">
        <div
          className={cn(
            'w-12 h-12 rounded-2xl flex items-center justify-center shrink-0 shadow-xs',
            currentStreakDays > 0
              ? 'bg-amber-500/10 text-amber-600 border border-amber-200/80'
              : 'bg-slate-100 text-slate-400 border border-slate-200'
          )}
        >
          <Flame className={cn('w-6 h-6', currentStreakDays > 0 && 'fill-amber-500')} />
        </div>
        <div className="min-w-0 flex-1">
          <span className="text-xs font-semibold text-slate-500 block truncate">
            {t('progress.currentStreak')}
          </span>
          <div className="text-2xl font-black text-slate-900 tracking-tight mt-0.5">
            {t('progress.currentStreakDays', { count: currentStreakDays })}
          </div>
        </div>
      </div>

      {/* Longest Streak */}
      <div className="p-4 sm:p-5 rounded-3xl bg-white border border-slate-200/90 shadow-2xs flex items-center gap-4 transition-all">
        <div className="w-12 h-12 rounded-2xl bg-indigo-50 border border-indigo-200/80 text-indigo-600 flex items-center justify-center shrink-0 shadow-xs">
          <Trophy className="w-6 h-6" />
        </div>
        <div className="min-w-0 flex-1">
          <div className="flex items-center justify-between gap-1">
            <span className="text-xs font-semibold text-slate-500 block truncate">
              {t('progress.longestStreak')}
            </span>
            <span className="text-[10px] text-slate-400 font-medium truncate">
              {t('progress.longestStreakNote')}
            </span>
          </div>
          <div className="text-2xl font-black text-slate-900 tracking-tight mt-0.5">
            {t('progress.longestStreakDays', { count: longestStreakDays })}
          </div>
        </div>
      </div>

      {/* Active Days in Selected Range */}
      <div className="p-4 sm:p-5 rounded-3xl bg-white border border-slate-200/90 shadow-2xs flex flex-col justify-between transition-all">
        <div className="flex items-center gap-4">
          <div className="w-12 h-12 rounded-2xl bg-emerald-50 border border-emerald-200/80 text-emerald-600 flex items-center justify-center shrink-0 shadow-xs">
            <CalendarCheck className="w-6 h-6" />
          </div>
          <div className="min-w-0 flex-1">
            <span className="text-xs font-semibold text-slate-500 block truncate">
              {t('progress.activeDays')}
            </span>
            <div className="text-2xl font-black text-slate-900 tracking-tight mt-0.5">
              {t('progress.activeDaysValue', { active: activeDaysInRange, total: days })}
            </div>
          </div>
        </div>
        {/* Visual progress bar */}
        <div className="w-full bg-slate-100 rounded-full h-1.5 mt-3 overflow-hidden">
          <div
            className="bg-emerald-500 h-1.5 rounded-full transition-all duration-500"
            style={{ width: `${activePercentage}%` }}
          />
        </div>
      </div>
    </div>
  );
};
