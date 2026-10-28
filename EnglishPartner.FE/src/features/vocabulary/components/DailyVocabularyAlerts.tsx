import React from 'react';
import { useTranslation } from 'react-i18next';
import { AlertCircle, Target } from 'lucide-react';
import type {
  DailyVocabularyPlanResponseDto,
  UserLearningSettingsDto,
} from '../types/vocabulary.types';

export interface DailyVocabularyAlertsProps {
  plan?: DailyVocabularyPlanResponseDto;
  learningSettings?: UserLearningSettingsDto;
  reviewError: string | null;
  onClearReviewError: () => void;
  apiKeyWarning: string | null;
  onClearApiKeyWarning: () => void;
}

export const DailyVocabularyAlerts: React.FC<DailyVocabularyAlertsProps> = React.memo(
  ({
    plan,
    learningSettings,
    reviewError,
    onClearReviewError,
    apiKeyWarning,
    onClearApiKeyWarning,
  }) => {
    const { t } = useTranslation();

    const showEffectiveSettingsNote = Boolean(
      plan &&
        learningSettings?.isPersisted &&
        (learningSettings.dailyVocabularyTarget !== plan.targetWordCount ||
          learningSettings.cefrLevel !== plan.targetCefrLevel)
    );

    return (
      <div className="space-y-4">
        {/* Target Setting Effective Note */}
        {showEffectiveSettingsNote && learningSettings && (
          <div className="bg-sky-50 border border-sky-200 rounded-2xl px-4 py-2.5 flex items-center gap-2 text-xs text-sky-900">
            <Target className="w-4 h-4 text-sky-600 shrink-0" />
            <span>
              {t('vocab.effectiveSettingsNote', {
                target: learningSettings.dailyVocabularyTarget,
                level: learningSettings.cefrLevel,
                date: learningSettings.effectiveFromLearningDate || 'ngày mai',
              })}
            </span>
          </div>
        )}

        {/* Shortfall Alert Banner */}
        {plan && plan.shortfallCount > 0 && (
          <div className="bg-amber-50 border border-amber-300 rounded-2xl px-4 py-3 flex items-center justify-between gap-3 text-xs text-amber-900">
            <div className="flex items-center gap-2.5">
              <AlertCircle className="w-4 h-4 text-amber-600 shrink-0" />
              <span>
                {t('vocab.shortfallBanner', {
                  newCount: plan.newWordCount,
                  level: plan.targetCefrLevel ?? 'B1',
                  shortfall: plan.shortfallCount,
                  target: plan.targetWordCount,
                })}
              </span>
            </div>
          </div>
        )}

        {/* Overflow Alert Banner */}
        {plan && plan.overflowCount > 0 && (
          <div className="bg-blue-50 border border-blue-300 rounded-2xl px-4 py-3 flex items-center justify-between gap-3 text-xs text-blue-900">
            <div className="flex items-center gap-2.5">
              <AlertCircle className="w-4 h-4 text-blue-600 shrink-0" />
              <span>
                {t('vocab.overflowBanner', {
                  carryOver: plan.carryOverCount,
                  assigned: plan.firstLearningAssignedCount,
                  overflow: plan.overflowCount,
                })}
              </span>
            </div>
          </div>
        )}

        {/* Review Submission Error Banner */}
        {reviewError && (
          <div className="bg-rose-50 border border-rose-300 rounded-2xl p-3.5 flex items-center justify-between gap-2 text-xs text-rose-900">
            <div className="flex items-center gap-2">
              <AlertCircle className="w-4 h-4 text-rose-600 shrink-0" />
              <span>{reviewError}</span>
            </div>
            <button
              type="button"
              onClick={onClearReviewError}
              className="font-bold hover:underline cursor-pointer"
            >
              {t('common.close', 'Đóng')}
            </button>
          </div>
        )}

        {apiKeyWarning && (
          <div className="bg-amber-50 border border-amber-300 rounded-2xl p-3.5 flex items-center justify-between gap-2 text-xs text-amber-900">
            <div className="flex items-center gap-2">
              <AlertCircle className="w-4 h-4 text-amber-600 shrink-0" />
              <span>{apiKeyWarning}</span>
            </div>
            <button
              type="button"
              onClick={onClearApiKeyWarning}
              className="font-bold hover:underline cursor-pointer"
            >
              {t('common.close')}
            </button>
          </div>
        )}
      </div>
    );
  }
);

DailyVocabularyAlerts.displayName = 'DailyVocabularyAlerts';
