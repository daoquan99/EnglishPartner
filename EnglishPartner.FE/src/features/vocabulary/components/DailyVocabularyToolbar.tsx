import React from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { Sparkles, Target } from 'lucide-react';
import { Badge } from '@/shared/components/ui/Badge';
import { Button } from '@/shared/components/ui/Button';
import type { DailyVocabularyPlanResponseDto, VocabularyFlashcardDto } from '../types/vocabulary.types';

export interface DailyVocabularyToolbarProps {
  plan?: DailyVocabularyPlanResponseDto;
  cards: VocabularyFlashcardDto[];
  currentIndex: number;
  ttsVoiceName: string;
  ttsAccent: string;
  ttsSpeed: number;
  onOpenLearningSettings: () => void;
  onOpenTtsSettings: () => void;
  onOpenAiGenerator: () => void;
}

export const DailyVocabularyToolbar: React.FC<DailyVocabularyToolbarProps> = React.memo(
  ({
    plan,
    cards,
    currentIndex,
    ttsVoiceName,
    ttsAccent,
    ttsSpeed,
    onOpenLearningSettings,
    onOpenTtsSettings,
    onOpenAiGenerator,
  }) => {
    const { t } = useTranslation();
    const navigate = useNavigate();

    const completedCount = React.useMemo(
      () => cards.filter((c) => c.isCompletedToday).length,
      [cards]
    );

    return (
      <div className="bg-white rounded-3xl border border-slate-200/90 px-5 py-4 flex flex-wrap items-center justify-between gap-4 shadow-xs">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-emerald-50 border border-emerald-200 flex items-center justify-center shrink-0">
            <img src="/assets/icons/navigation/nav-study.svg" alt="" className="w-5 h-5 object-contain" />
          </div>
          <div>
            <h1 className="text-base sm:text-lg font-extrabold text-slate-900">
              {t('vocab.pageTitle')}
            </h1>
            <div className="flex items-center gap-2 flex-wrap mt-0.5">
              {plan ? (
                <>
                  <Badge variant="emerald">
                    {t('vocab.firstLearningLabel')}: {plan.firstLearningCompletedCount}/{plan.firstLearningAssignedCount} (Chuyển tiếp: {plan.carryOverCount} · Mới: {plan.newWordCount})
                  </Badge>
                  {plan.dueReviewCount > 0 && (
                    <Badge variant="sky">
                      {t('vocab.dueReviewLabel')}: {plan.dueReviewCount - plan.remainingReviewCount}/{plan.dueReviewCount}
                    </Badge>
                  )}
                  <Badge variant="amber">
                    {t('vocab.todayPlanSummary', { target: plan.targetWordCount, level: plan.targetCefrLevel ?? 'B1' })}
                  </Badge>
                </>
              ) : (
                <Badge variant="emerald">
                  {t('vocab.completedBadge')}: {completedCount}/{cards.length} {t('vocab.wordsUnit')}
                </Badge>
              )}
              {cards.length > 0 && (
                <Badge variant="sky">
                  {t('vocab.cardBadge')}: {currentIndex + 1}/{cards.length}
                </Badge>
              )}
              <Badge variant="violet">
                {t('vocab.teacherVoiceBadge')}: {ttsVoiceName} ({ttsAccent} · {ttsSpeed.toFixed(2)}x)
              </Badge>
            </div>
          </div>
        </div>

        {/* Action Buttons opening Dialogs/Modals */}
        <div className="flex items-center gap-2.5 flex-wrap">
          <Button
            variant="outline"
            size="sm"
            onClick={onOpenLearningSettings}
          >
            <Target className="w-4 h-4 text-emerald-600" />
            {t('vocab.targetSettingsBtn')}
          </Button>

          <Button
            variant="outline"
            size="sm"
            onClick={onOpenTtsSettings}
          >
            <img src="/assets/icons/navigation/nav-settings.svg" alt="" className="w-4 h-4 object-contain" />
            {t('vocab.configTts')}
          </Button>

          <Button
            variant="outline"
            size="sm"
            onClick={() => navigate('/student/vocabulary-games/falling-words?source=DailyVocabulary')}
            title="Chơi Từ vựng rơi với danh sách từ hôm nay"
          >
            <span className="text-sm">🎮</span>
            Chơi Từ vựng rơi
          </Button>

          <Button
            variant="primary"
            size="sm"
            onClick={onOpenAiGenerator}
          >
            <Sparkles className="w-4 h-4" />
            {t('vocab.generateWithAi')}
          </Button>
        </div>
      </div>
    );
  }
);

DailyVocabularyToolbar.displayName = 'DailyVocabularyToolbar';
