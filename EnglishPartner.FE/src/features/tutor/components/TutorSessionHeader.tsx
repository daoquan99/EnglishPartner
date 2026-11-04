import React, { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ChevronDown, ChevronUp } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { cn } from '@/shared/lib/cn';
import { useIsDesktop } from '@/shared/hooks/useMobileDrawer';
import type { ThreeHubStates } from '../types/tutor.types';

export interface TutorSessionHeaderProps {
  cefrLevel: string;
  conversationMode: string;
  voiceName: string;
  speechSpeed: number;
  topic?: string | null;
  hubStates: ThreeHubStates;
  historyCount: number;
  onOpenSetup: () => void;
  onNewConversation: () => void;
  onOpenHistory: () => void;
}

export const TutorSessionHeader: React.FC<TutorSessionHeaderProps> = ({
  cefrLevel,
  conversationMode,
  voiceName,
  speechSpeed,
  topic,
  hubStates,
  historyCount,
  onOpenSetup,
  onNewConversation,
  onOpenHistory,
}) => {
  const { t } = useTranslation();
  const isDesktop = useIsDesktop();

  const [userExpandedPref, setUserExpandedPref] = useState<boolean | null>(() => {
    try {
      const saved = localStorage.getItem('lingomint_tutor_header_expanded');
      if (saved !== null) {
        return saved === 'true';
      }
    } catch {
      // Ignore localStorage read errors (e.g. storage disabled)
    }
    return null;
  });

  const isExpanded = userExpandedPref !== null ? userExpandedPref : isDesktop;

  const handleToggle = () => {
    const next = !isExpanded;
    setUserExpandedPref(next);
    try {
      localStorage.setItem('lingomint_tutor_header_expanded', String(next));
    } catch {
      // Ignore localStorage write errors
    }
  };

  const aggregateHub = useMemo(() => {
    const { tutorHub, transcribeHub, translateHub, grammarHub = 'disconnected' } = hubStates;
    const states = [tutorHub, transcribeHub, translateHub, grammarHub];
    if (states.some((s) => s === 'error')) {
      return {
        colorClass: 'bg-rose-500',
        label: t('tutor.hubStatusError'),
      };
    }
    if (states.every((s) => s === 'connected')) {
      return {
        colorClass: 'bg-emerald-500',
        label: t('tutor.hubStatusConnected'),
      };
    }
    if (states.some((s) => s === 'connecting' || s === 'reconnecting')) {
      return {
        colorClass: 'bg-amber-500 animate-pulse',
        label: t('tutor.hubStatusConnecting'),
      };
    }
    return {
      colorClass: 'bg-slate-300',
      label: t('tutor.hubStatusDisconnected'),
    };
  }, [hubStates, t]);

  const grammarHubState = hubStates.grammarHub ?? 'disconnected';
  const hubsSummary = `Hub 1: ${hubStates.tutorHub} · Hub 2: ${hubStates.transcribeHub} · Hub 3: ${hubStates.translateHub} · Hub 4: ${grammarHubState}`;

  return (
    <div className="bg-white rounded-3xl border border-slate-200/90 px-4 sm:px-5 py-3 shadow-xs space-y-3">
      {/* Summary row: always rendered so the single toggle keeps keyboard focus across expand/collapse. */}
      <div className="flex items-center justify-between gap-2.5 min-w-0">
        <div className="flex items-center gap-2 sm:gap-2.5 min-w-0">
          <div
            className={cn('w-2.5 h-2.5 rounded-full shrink-0', aggregateHub.colorClass)}
            title={hubsSummary}
          >
            <span className="sr-only">
              {aggregateHub.label}: {hubsSummary}
            </span>
          </div>
          <Badge variant="emerald" className="shrink-0">
            CEFR {cefrLevel}
          </Badge>
          <Badge variant="sky" className="truncate max-w-[130px] sm:max-w-[260px]" title={conversationMode}>
            {conversationMode}
          </Badge>
        </div>

        <div className="flex items-center gap-1.5 shrink-0">
          {!isExpanded && (
            <>
              <button
                type="button"
                onClick={onOpenSetup}
                aria-label={t('tutor.setupModal')}
                title={t('tutor.setupModal')}
                className="min-h-[44px] min-w-[44px] p-2 flex items-center justify-center rounded-xl border border-emerald-300 bg-emerald-50/70 hover:bg-emerald-100/80 text-emerald-900 transition-colors cursor-pointer"
              >
                <img src="/assets/icons/navigation/nav-settings.svg" alt="" className="w-4 h-4 object-contain" />
              </button>
              <button
                type="button"
                onClick={onOpenHistory}
                aria-label={`${t('tutor.historyModal')} (${historyCount})`}
                title={`${t('tutor.historyModal')} (${historyCount})`}
                className="min-h-[44px] min-w-[44px] p-2 flex items-center justify-center rounded-xl border border-slate-200 bg-white hover:bg-slate-50 text-slate-700 transition-colors relative cursor-pointer"
              >
                <img src="/assets/icons/navigation/nav-history.svg" alt="" className="w-3.5 h-3.5 object-contain" />
                {historyCount > 0 && (
                  <span className="absolute -top-1 -right-1 bg-emerald-600 text-white text-[10px] font-bold rounded-full h-4 min-w-[16px] px-1 flex items-center justify-center">
                    {historyCount}
                  </span>
                )}
              </button>
            </>
          )}
          <button
            type="button"
            onClick={handleToggle}
            aria-expanded={isExpanded}
            aria-controls="tutor-header-details"
            aria-label={isExpanded ? t('tutor.collapseDetails') : t('tutor.expandDetails')}
            title={isExpanded ? t('tutor.collapseDetails') : t('tutor.expandDetails')}
            className="min-h-[44px] min-w-[44px] lg:min-h-9 lg:min-w-9 p-2 flex items-center justify-center rounded-xl border border-slate-200 bg-slate-50 hover:bg-slate-100 text-slate-600 transition-colors cursor-pointer"
          >
            {isExpanded ? <ChevronUp className="w-4 h-4" /> : <ChevronDown className="w-4 h-4" />}
          </button>
        </div>
      </div>

      {/* Details: always in the DOM (aria-controls target), hidden when collapsed. */}
      <div
        id="tutor-header-details"
        hidden={!isExpanded}
        className="pt-3 border-t border-slate-100 flex-wrap items-center justify-between gap-3 [&:not([hidden])]:flex"
      >
        <div className="flex items-center gap-2.5 flex-wrap">
          <Button
            variant="outline"
            size="sm"
            onClick={onOpenSetup}
            className="border-emerald-300 bg-emerald-50/70 hover:bg-emerald-100/80 text-emerald-900 font-bold"
          >
            <img src="/assets/icons/navigation/nav-settings.svg" alt="" className="w-4 h-4 object-contain" />
            {t('tutor.setupModal')}
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={onNewConversation}
            title={t('tutor.newConversation')}
            className="border-slate-300 bg-white hover:bg-emerald-50 hover:border-emerald-300 text-slate-800 hover:text-emerald-900 font-semibold"
          >
            <img src="/assets/icons/navigation/nav-conversations.svg" alt="" className="w-4 h-4 object-contain" />
            {t('tutor.newConversation')}
          </Button>
          <Badge variant="violet">Voice: {voiceName}</Badge>
          <Badge variant="amber">Speed: {speechSpeed.toFixed(2)}x</Badge>
          {topic && (
            <span className="text-xs font-semibold text-slate-700 bg-slate-100 px-3 py-1 rounded-full truncate max-w-full sm:max-w-xs">
              {t('conversationTopic')}: {topic}
            </span>
          )}
        </div>

        <div className="flex items-center gap-2 flex-wrap">
          <div className="flex items-center gap-1.5 text-[11px] font-semibold text-slate-600 bg-slate-50 px-3 py-1.5 rounded-xl border border-slate-200 flex-wrap">
            <span>Hub 1: {hubStates.tutorHub}</span>
            <span aria-hidden="true">·</span>
            <span>Hub 2: {hubStates.transcribeHub}</span>
            <span aria-hidden="true">·</span>
            <span>Hub 3: {hubStates.translateHub}</span>
            <span aria-hidden="true">·</span>
            <span>Hub 4: {grammarHubState}</span>
          </div>
          <Button variant="outline" size="sm" onClick={onOpenHistory}>
            <img src="/assets/icons/navigation/nav-history.svg" alt="" className="w-3.5 h-3.5 object-contain" />
            {t('tutor.historyModal')} ({historyCount})
          </Button>
        </div>
      </div>
    </div>
  );
};
