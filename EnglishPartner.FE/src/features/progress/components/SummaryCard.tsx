import React from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ArrowRight } from 'lucide-react';
import { useCurrentUser } from '@/features/auth';
import type { ProgressGrammarLevelDto } from '@/shared/api/openapi.types';

export interface SummaryStatItem {
  label: string;
  value: React.ReactNode;
}

export interface SummaryCardProps {
  title: string;
  icon: React.ReactNode;
  href: string;
  permission?: string;
  stats: SummaryStatItem[];
  grammarData?: {
    levels: ProgressGrammarLevelDto[];
    currentStepCode: string | null;
  };
}

export const SummaryCard: React.FC<SummaryCardProps> = ({
  title,
  icon,
  href,
  permission,
  stats,
  grammarData,
}) => {
  const { t } = useTranslation();
  const { hasPermission } = useCurrentUser();

  // Route permission check: if permission is specified, user must have it
  const canAccessLink = permission ? hasPermission(permission) : true;

  return (
    <div className="p-4 sm:p-6 rounded-3xl bg-white border border-slate-200/90 shadow-2xs flex flex-col justify-between transition-all space-y-4">
      {/* Card Header: Icon, Title, and conditional Link */}
      <div>
        <div className="flex items-center justify-between gap-3 border-b border-slate-100 pb-3.5">
          <div className="flex items-center gap-3 min-w-0">
            <div className="w-10 h-10 rounded-2xl bg-slate-100 border border-slate-200 flex items-center justify-center text-slate-700 shrink-0">
              {icon}
            </div>
            <h4 className="text-sm font-extrabold text-slate-900 truncate">{title}</h4>
          </div>

          {canAccessLink && (
            <Link
              to={href}
              className="inline-flex items-center gap-1 text-xs font-bold text-emerald-600 hover:text-emerald-700 hover:underline shrink-0 cursor-pointer"
            >
              <span>{t('progress.viewModule')}</span>
              <ArrowRight className="w-3.5 h-3.5" />
            </Link>
          )}
        </div>

        {/* Stats Grid */}
        <div className="grid grid-cols-2 gap-3 mt-4">
          {stats.map((stat, idx) => (
            <div key={idx} className="p-3 rounded-2xl bg-slate-50 border border-slate-100">
              <span className="text-[11px] font-medium text-slate-500 block truncate">
                {stat.label}
              </span>
              <div className="text-base font-bold text-slate-900 mt-0.5 truncate">
                {stat.value ?? '—'}
              </div>
            </div>
          ))}
        </div>

        {/* Special Grammar Levels & Current Step display */}
        {grammarData && (
          <div className="mt-4 pt-3.5 border-t border-slate-100 space-y-3">
            <div className="flex items-center justify-between text-xs">
              <span className="font-semibold text-slate-500">
                {t('progress.grammarCurrentStep')}:
              </span>
              <span className="font-bold text-slate-900">
                {grammarData.currentStepCode
                  ? t('progress.stepCodeValue', { code: grammarData.currentStepCode })
                  : '—'}
              </span>
            </div>

            {grammarData.levels && grammarData.levels.length > 0 && (
              <div className="space-y-2">
                <span className="text-[11px] font-semibold text-slate-400 uppercase tracking-wider block">
                  {t('progress.grammarLevels')}
                </span>
                <div className="space-y-2">
                  {grammarData.levels.map((lvl) => {
                    const pct =
                      lvl.totalSteps > 0
                        ? Math.min(100, Math.round((lvl.passedSteps / lvl.totalSteps) * 100))
                        : 0;
                    return (
                      <div key={lvl.level} className="space-y-1">
                        <div className="flex items-center justify-between text-xs font-medium">
                          <span className="font-bold text-slate-700">{lvl.level}</span>
                          <span className="text-slate-500 font-mono text-[11px]">
                            {lvl.passedSteps} / {lvl.totalSteps}
                          </span>
                        </div>
                        <div className="w-full bg-slate-100 rounded-full h-1.5 overflow-hidden">
                          <div
                            className="bg-amber-500 h-1.5 rounded-full transition-all duration-300"
                            style={{ width: `${pct}%` }}
                          />
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
};
