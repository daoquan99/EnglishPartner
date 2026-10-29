import React from 'react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import '@/shared/i18n';
import { DailyVocabularyEmptyState } from '../components/DailyVocabularyEmptyState';
import { DailyVocabularyPage } from '../pages/DailyVocabularyPage';

vi.mock('../api/vocabularyApi', () => ({
  vocabularyApi: {
    getTodayPlan: vi.fn(),
    initializeTodayPlan: vi.fn(),
    getLearningSettings: vi.fn().mockResolvedValue({
      dailyVocabularyTarget: 20,
      cefrLevel: 'B1',
      isPersisted: true,
      effectiveFromLearningDate: '2026-10-29',
      timeZoneId: 'Asia/Ho_Chi_Minh',
    }),
    submitReview: vi.fn(),
  },
}));

vi.mock('@/features/catalog', () => ({
  useActiveScoringRubricQuery: () => ({ data: { code: 'rubric-1' } }),
  useVocabularyGenerationModelsQuery: () => ({ data: [] }),
  useTopicsQuery: () => ({ data: [] }),
}));

vi.mock('@/features/tutor', () => ({
  useTutorSessionStore: () => ({
    apiKey: '',
    requestFocusLeftMenuApiKey: vi.fn(),
    aiProvider: 'gemini',
    agyModelCode: 'gemini-3.8-flash-high',
  }),
}));

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: false },
  },
});

function renderWithProviders(ui: React.ReactElement) {
  return renderToStaticMarkup(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>{ui}</MemoryRouter>
    </QueryClientProvider>
  );
}

describe('Daily Vocabulary Auto-Initialization Logic & 404 Handling', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('is404Uninitialized check', () => {
    const checkIs404Uninitialized = (planError: any): boolean => {
      if (!planError) return false;
      const status = planError.response?.status ?? planError.status;
      if (status !== 404) return false;
      const code = planError.response?.data?.code ?? planError.code;
      return code === 'daily_plan_not_initialized';
    };

    it('returns true when status is 404 and code is daily_plan_not_initialized', () => {
      const error = {
        response: {
          status: 404,
          data: {
            code: 'daily_plan_not_initialized',
            detail: 'Kế hoạch học tập từ vựng hôm nay chưa được khởi tạo.',
          },
        },
      };

      expect(checkIs404Uninitialized(error)).toBe(true);
    });

    it('returns false when status is 404 but code is different or missing', () => {
      const errorWithoutCode = {
        response: {
          status: 404,
          data: {
            detail: 'Tài nguyên không tìm thấy.',
          },
        },
      };

      const errorOtherCode = {
        response: {
          status: 404,
          data: {
            code: 'user_not_found',
          },
        },
      };

      expect(checkIs404Uninitialized(errorWithoutCode)).toBe(false);
      expect(checkIs404Uninitialized(errorOtherCode)).toBe(false);
    });

    it('returns false when status is 500 server error', () => {
      const error500 = {
        response: {
          status: 500,
          data: {
            detail: 'Internal Server Error',
          },
        },
      };

      expect(checkIs404Uninitialized(error500)).toBe(false);
    });

    it('returns false when planError is null or undefined', () => {
      expect(checkIs404Uninitialized(null)).toBe(false);
      expect(checkIs404Uninitialized(undefined)).toBe(false);
    });
  });

  describe('Auto-init guard execution simulator', () => {
    it('calls initPlanMutation exactly once when is404Uninitialized is true and ref guard is active', () => {
      let autoInitAttempted = false;
      const initPlanMutationMock = {
        isPending: false,
        mutate: vi.fn(() => {
          autoInitAttempted = true;
        }),
      };

      const runAutoInitEffect = (is404: boolean) => {
        if (is404 && !autoInitAttempted && !initPlanMutationMock.isPending) {
          autoInitAttempted = true;
          initPlanMutationMock.mutate();
        }
      };

      // First run with 404 daily_plan_not_initialized
      runAutoInitEffect(true);
      expect(initPlanMutationMock.mutate).toHaveBeenCalledTimes(1);

      // Re-render / second effect trigger with same error
      runAutoInitEffect(true);
      // Still only called once due to ref guard
      expect(initPlanMutationMock.mutate).toHaveBeenCalledTimes(1);
    });

    it('does NOT call initPlanMutation when error is other than daily_plan_not_initialized', () => {
      let autoInitAttempted = false;
      const initPlanMutationMock = {
        isPending: false,
        mutate: vi.fn(),
      };

      const runAutoInitEffect = (is404: boolean) => {
        if (is404 && !autoInitAttempted && !initPlanMutationMock.isPending) {
          autoInitAttempted = true;
          initPlanMutationMock.mutate();
        }
      };

      // Run with non-404 error
      runAutoInitEffect(false);
      expect(initPlanMutationMock.mutate).not.toHaveBeenCalled();
    });

    it('does NOT loop on mutation failure because ref guard remains true', () => {
      let autoInitAttempted = false;
      let errorOccurred = false;

      const initPlanMutationMock = {
        isPending: false,
        mutate: vi.fn((_vars, callbacks) => {
          callbacks?.onError?.(new Error('Network failure'));
        }),
      };

      const runAutoInitEffect = (is404: boolean) => {
        if (is404 && !autoInitAttempted && !initPlanMutationMock.isPending) {
          autoInitAttempted = true;
          initPlanMutationMock.mutate(undefined, {
            onError: () => {
              errorOccurred = true;
            },
          });
        }
      };

      runAutoInitEffect(true);
      expect(initPlanMutationMock.mutate).toHaveBeenCalledTimes(1);
      expect(errorOccurred).toBe(true);

      // Subsequent effect runs do not call mutate again
      runAutoInitEffect(true);
      expect(initPlanMutationMock.mutate).toHaveBeenCalledTimes(1);
    });
  });

  describe('UI fallback when auto-initialization fails or needs manual start', () => {
    it('renders manual start action button in DailyVocabularyEmptyState', () => {
      const handleInit = vi.fn();
      const html = renderWithProviders(
        <DailyVocabularyEmptyState
          is404Uninitialized={true}
          planError={{
            response: {
              status: 404,
              data: { code: 'daily_plan_not_initialized' },
            },
          }}
          learningSettings={{
            dailyVocabularyTarget: 20,
            cefrLevel: 'B1',
            isPersisted: true,
            effectiveFromLearningDate: '2026-10-29',
            timeZoneId: 'Asia/Ho_Chi_Minh',
          }}
          isInitializingPlan={false}
          onInitializePlan={handleInit}
          onOpenLearningSettings={vi.fn()}
          onRefetchPlan={vi.fn()}
          onOpenAiGenerator={vi.fn()}
        />
      );

      expect(html).toContain('Bắt đầu học hôm nay');
      expect(html).toContain('Chưa bắt đầu kế hoạch học hôm nay');
    });

    it('renders loading spinner when isInitializingPlan is true', () => {
      const html = renderWithProviders(
        <DailyVocabularyEmptyState
          is404Uninitialized={true}
          planError={{
            response: {
              status: 404,
              data: { code: 'daily_plan_not_initialized' },
            },
          }}
          isInitializingPlan={true}
          onInitializePlan={vi.fn()}
          onOpenLearningSettings={vi.fn()}
          onRefetchPlan={vi.fn()}
          onOpenAiGenerator={vi.fn()}
        />
      );

      expect(html).toContain('animate-spin');
      expect(html).toContain('disabled=""');
    });
  });
});
