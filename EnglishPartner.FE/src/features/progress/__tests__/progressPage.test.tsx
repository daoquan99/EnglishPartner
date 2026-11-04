import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import '@/shared/i18n';
import { ProgressPage } from '../pages/ProgressPage';
import { progressKeys } from '../hooks/useProgressQueries';
import * as progressQueriesModule from '../hooks/useProgressQueries';
import { useAuthStore } from '@/features/auth/store/authStore';
import type { ProgressOverviewDto } from '@/shared/api/openapi.types';

const mockUser = {
  id: 'u-1',
  username: 'learner1',
  email: 'learner1@lingomint.local',
  fullName: 'Learner One',
  preferredCefrLevel: 'B1' as const,
  preferredVoiceName: 'Aoede',
  timeZoneId: 'Asia/Ho_Chi_Minh',
  isActive: true,
  roles: ['LEARNER'],
  permissions: ['vocabularies.read', 'grammar.read', 'conversations.access'],
};

const mockOverviewActiveData: ProgressOverviewDto = {
  timeZoneId: 'Asia/Ho_Chi_Minh',
  days: 30,
  currentStreakDays: 5,
  longestStreakDays: 12,
  activeDaysInRange: 18,
  activity: Array.from({ length: 30 }, (_, i) => ({
    date: `2026-10-${String(i + 1).padStart(2, '0')}`,
    vocabularyReviews: i % 2 === 0 ? 3 : 0,
    lessonAttempts: i % 3 === 0 ? 1 : 0,
    grammarAttempts: i % 4 === 0 ? 2 : 0,
    pronunciationAssessments: i % 2 === 0 ? 2 : 0,
    tutorSessions: i === 15 ? 1 : 0,
  })),
  vocabulary: {
    studied: 50,
    mastered: 20,
    dueForReviewNow: 5,
    averageMastery: 75.5,
  },
  lessons: {
    completedAttempts: 8,
    averageScore100: 85,
  },
  grammar: {
    levels: [
      { level: 'A1', passedSteps: 15, totalSteps: 15 },
      { level: 'A2', passedSteps: 8, totalSteps: 15 },
    ],
    currentStepCode: 'A2-03',
  },
  speech: {
    assessments: 25,
    averageScore: 82.4,
    averageScoreInRange: 85.0,
  },
  liveTutor: {
    sessions: 4,
    sessionsInRange: 2,
  },
};

const mockOverviewEmptyData: ProgressOverviewDto = {
  timeZoneId: 'Asia/Ho_Chi_Minh',
  days: 30,
  currentStreakDays: 0,
  longestStreakDays: 0,
  activeDaysInRange: 0,
  activity: Array.from({ length: 30 }, (_, i) => ({
    date: `2026-10-${String(i + 1).padStart(2, '0')}`,
    vocabularyReviews: 0,
    lessonAttempts: 0,
    grammarAttempts: 0,
    pronunciationAssessments: 0,
    tutorSessions: 0,
  })),
  vocabulary: {
    studied: 0,
    mastered: 0,
    dueForReviewNow: 0,
    averageMastery: 0,
  },
  lessons: {
    completedAttempts: 0,
    averageScore100: null,
  },
  grammar: {
    levels: [{ level: 'A1', passedSteps: 0, totalSteps: 15 }],
    currentStepCode: 'A1-01',
  },
  speech: {
    assessments: 0,
    averageScore: null,
    averageScoreInRange: null,
  },
  liveTutor: {
    sessions: 0,
    sessionsInRange: 0,
  },
};

describe('ProgressPage Component & States', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    (useAuthStore as any).getInitialState = () => useAuthStore.getState();
    (useAuthStore as any).getServerSnapshot = () => useAuthStore.getState();
    useAuthStore.setState({
      currentUser: mockUser,
      isAuthenticated: true,
      accessToken: 'token-123',
    });

    queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    queryClient.setQueryData(['auth', 'me'], mockUser);
  });

  const renderComponent = () => {
    return renderToStaticMarkup(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <ProgressPage />
        </MemoryRouter>
      </QueryClientProvider>
    );
  };

  it('renders LoadingState when query is fetching without cached data', () => {
    vi.spyOn(progressQueriesModule, 'useProgressOverviewQuery').mockReturnValue({
      data: undefined,
      isLoading: true,
      error: null,
      refetch: vi.fn(),
    } as any);

    const html = renderComponent();

    expect(html).toContain('Đang tải dữ liệu tiến độ học...');
  });

  it('renders FeedbackBanner with retry button when query errors', () => {
    const testError = {
      response: {
        data: {
          detail: 'Lỗi kết nối máy chủ',
        },
      },
    };

    vi.spyOn(progressQueriesModule, 'useProgressOverviewQuery').mockReturnValue({
      data: undefined,
      isLoading: false,
      error: testError as any,
      refetch: vi.fn(),
    } as any);

    const html = renderComponent();

    expect(html).toContain('Lỗi kết nối máy chủ');
    expect(html).toContain('Thử lại');
  });

  it('renders EmptyState with call-to-action when all progress statistics are 0', () => {
    vi.spyOn(progressQueriesModule, 'useProgressOverviewQuery').mockReturnValue({
      data: mockOverviewEmptyData,
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    } as any);

    const html = renderComponent();

    expect(html).toContain('Chưa có hoạt động học nào');
    expect(html).toContain('Bắt đầu học ngay');
    expect(html).toContain('href="/student/vocabulary"');
  });

  it('renders StreakCard, ActivityChart and 5 summary cards when data is available', () => {
    vi.spyOn(progressQueriesModule, 'useProgressOverviewQuery').mockReturnValue({
      data: mockOverviewActiveData,
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    } as any);

    const html = renderComponent();

    // Header & segmented buttons
    expect(html).toContain('Tiến độ học tập');
    expect(html).toContain('7 ngày');
    expect(html).toContain('30 ngày');
    expect(html).toContain('90 ngày');
    expect(html).toContain('aria-pressed="true"');

    // Streak card
    expect(html).toContain('5 ngày');
    expect(html).toContain('12 ngày');
    expect(html).toContain('18 / 30 ngày');

    // Activity chart
    expect(html).toContain('Biểu đồ hoạt động');
    expect(html).toContain('data-testid="activity-column"');

    // 5 Summary cards
    expect(html).toContain('Từ vựng');
    expect(html).toContain('Bài học');
    expect(html).toContain('Ngữ pháp');
    expect(html).toContain('Phát âm');
    expect(html).toContain('Live Tutor');

    // Values in summary cards
    expect(html).toContain('50'); // vocab studied
    expect(html).toContain('85/100'); // lesson average score
    expect(html).toContain('82.4'); // speech average
    expect(html).toContain('Step A2-03'); // grammar step
    expect(html).toContain('4'); // live tutor total
  });

  it('changes query keys when selecting 7, 30, or 90 days', () => {
    expect(progressKeys.overview(7)).toEqual(['progress', 'overview', 7]);
    expect(progressKeys.overview(30)).toEqual(['progress', 'overview', 30]);
    expect(progressKeys.overview(90)).toEqual(['progress', 'overview', 90]);
  });
});
