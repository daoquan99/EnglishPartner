import React from 'react';
import { useTranslation } from 'react-i18next';
import { isAxiosError } from 'axios';
import { AlertCircle, Loader2, Sparkles, Target } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { EmptyState } from '@/shared/components/ui/EmptyState';
import type { UserLearningSettingsDto } from '../types/vocabulary.types';

export interface DailyVocabularyEmptyStateProps {
  is404Uninitialized: boolean;
  planError: unknown;
  learningSettings?: UserLearningSettingsDto;
  isInitializingPlan: boolean;
  onInitializePlan: () => void;
  onOpenLearningSettings: () => void;
  onRefetchPlan: () => void;
  onOpenAiGenerator: () => void;
}

export const DailyVocabularyEmptyState: React.FC<DailyVocabularyEmptyStateProps> = React.memo(
  ({
    is404Uninitialized,
    planError,
    learningSettings,
    isInitializingPlan,
    onInitializePlan,
    onOpenLearningSettings,
    onRefetchPlan,
    onOpenAiGenerator,
  }) => {
    const { t } = useTranslation();

    if (is404Uninitialized) {
      return (
        <div className="bg-white rounded-3xl border border-slate-200/90 p-8 shadow-xs">
          <EmptyState
            illustration="no-plan"
            title={t('vocab.uninitializedPlanTitle')}
            description={t('vocab.uninitializedPlanDesc', {
              target: learningSettings?.dailyVocabularyTarget ?? 20,
              level: learningSettings?.cefrLevel ?? 'B1',
            })}
            action={
              <div className="flex items-center gap-3">
                <Button
                  variant="primary"
                  size="sm"
                  disabled={isInitializingPlan}
                  onClick={onInitializePlan}
                >
                  {isInitializingPlan ? (
                    <>
                      <Loader2 className="w-4 h-4 animate-spin" />
                      {t('common.processing', 'Đang khởi tạo...')}
                    </>
                  ) : (
                    <>
                      <Sparkles className="w-4 h-4" />
                      {t('vocab.startTodayPlan')}
                    </>
                  )}
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={onOpenLearningSettings}
                >
                  <Target className="w-4 h-4 text-emerald-600" />
                  {t('vocab.adjustTarget')}
                </Button>
              </div>
            }
          />
        </div>
      );
    }

    if (planError) {
      return (
        <div className="bg-white rounded-3xl border border-rose-200 p-8 shadow-xs text-center space-y-4">
          <AlertCircle className="w-10 h-10 text-rose-500 mx-auto" />
          <h2 className="text-base font-extrabold text-slate-900">
            {t('vocab.loadErrorTitle', 'Không thể tải kế hoạch học tập')}
          </h2>
          <p className="text-sm text-slate-600 max-w-md mx-auto">
            {isAxiosError(planError)
              ? planError.response?.data?.error || planError.message
              : (planError as any)?.message || 'Có lỗi xảy ra khi kết nối máy chủ.'}
          </p>
          <Button variant="primary" size="sm" onClick={onRefetchPlan}>
            {t('common.retry', 'Thử lại')}
          </Button>
        </div>
      );
    }

    return (
      <div className="bg-white rounded-3xl border border-slate-200/90 p-8 shadow-xs">
        <EmptyState
          illustration="no-plan"
          title={t('vocab.emptyPlanTitle', 'Chưa có kế hoạch từ vựng cho hôm nay')}
          description={t(
            'vocab.emptyPlanDesc',
            'Nhấn "Tạo từ vựng bằng AI" ở góc trên bên phải để hệ thống tự động tạo bộ từ vựng theo chủ đề bạn chọn!'
          )}
          action={
            <Button
              variant="primary"
              size="sm"
              onClick={onOpenAiGenerator}
            >
              <Sparkles className="w-4 h-4" />
              {t('vocab.generateWithAi')}
            </Button>
          }
        />
      </div>
    );
  }
);

DailyVocabularyEmptyState.displayName = 'DailyVocabularyEmptyState';
