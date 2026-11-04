import React from 'react';
import { describe, expect, it, beforeEach } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import '@/shared/i18n';
import { StudentLayout } from '@/app/layouts/StudentLayout';
import { AppRoutes } from '@/app/router';
import { useAuthStore } from '@/features/auth/store/authStore';
import { progressKeys } from '../hooks/useProgressQueries';
import type { UserProfileDto, ProgressOverviewDto } from '@/shared/api/openapi.types';

const mockLearner: UserProfileDto = {
  id: 'learner-1',
  username: 'student1',
  email: 'student1@lingomint.local',
  fullName: 'Student One',
  preferredCefrLevel: 'B1',
  preferredVoiceName: 'Aoede',
  timeZoneId: 'Asia/Ho_Chi_Minh',
  isActive: true,
  roles: ['LEARNER'],
  permissions: ['grammar.read', 'conversations.access', 'vocabularies.read'],
};

const mockOverview: ProgressOverviewDto = {
  timeZoneId: 'Asia/Ho_Chi_Minh',
  days: 30,
  currentStreakDays: 3,
  longestStreakDays: 7,
  activeDaysInRange: 10,
  activity: Array.from({ length: 30 }, (_, i) => ({
    date: `2026-10-${String(i + 1).padStart(2, '0')}`,
    vocabularyReviews: 2,
    lessonAttempts: 1,
    grammarAttempts: 1,
    pronunciationAssessments: 1,
    tutorSessions: 0,
  })),
  vocabulary: { studied: 30, mastered: 10, dueForReviewNow: 2, averageMastery: 70 },
  lessons: { completedAttempts: 5, averageScore100: 90 },
  grammar: { levels: [{ level: 'A1', passedSteps: 10, totalSteps: 15 }], currentStepCode: 'A1-03' },
  speech: { assessments: 15, averageScore: 80, averageScoreInRange: 80 },
  liveTutor: { sessions: 2, sessionsInRange: 1 },
};

describe('Student Menu "Tiến độ" & Progress Route Verification', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    (useAuthStore as any).getInitialState = () => useAuthStore.getState();
    (useAuthStore as any).getServerSnapshot = () => useAuthStore.getState();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
    queryClient.setQueryData(['auth', 'me'], mockLearner);
    useAuthStore.setState({
      isAuthenticated: true,
      isInitialized: true,
      currentUser: mockLearner,
      accessToken: 'test-token',
    });
  });

  describe('Real StudentLayout rendering & active states for Progress', () => {
    const renderLayout = (initialPath: string) => {
      return renderToStaticMarkup(
        <QueryClientProvider client={queryClient}>
          <MemoryRouter initialEntries={[initialPath]}>
            <StudentLayout />
          </MemoryRouter>
        </QueryClientProvider>
      );
    };

    it('renders real StudentLayout with active styling and aria-current="page" on /student/progress', () => {
      const html = renderLayout('/student/progress');
      expect(html).toContain('href="/student/progress"');
      expect(html).toContain('aria-current="page"');
      expect(html).toContain('bg-emerald-600');
    });

    it('renders real StudentLayout with inactive styling on /student/tutor', () => {
      const html = renderLayout('/student/tutor');
      expect(html).toContain('href="/student/progress"');
      const progressLinkIndex = html.indexOf('href="/student/progress"');
      expect(progressLinkIndex).toBeGreaterThan(-1);
      const linkSegment = html.slice(progressLinkIndex - 100, progressLinkIndex + 200);
      expect(linkSegment).not.toContain('aria-current="page"');
    });
  });

  describe('Real AppRoutes resolution for /student/progress', () => {
    const renderRoutes = async (initialPath: string) => {
      await import('@/features/progress');

      try {
        renderToStaticMarkup(
          <QueryClientProvider client={queryClient}>
            <MemoryRouter initialEntries={[initialPath]}>
              <AppRoutes />
            </MemoryRouter>
          </QueryClientProvider>
        );
      } catch {
        // ignore first render suspense
      }

      await new Promise((resolve) => setTimeout(resolve, 150));

      return renderToStaticMarkup(
        <QueryClientProvider client={queryClient}>
          <MemoryRouter initialEntries={[initialPath]}>
            <AppRoutes />
          </MemoryRouter>
        </QueryClientProvider>
      );
    };

    it('/student/progress renders ProgressPage and does not redirect to /login', async () => {
      queryClient.setQueryData(progressKeys.overview(30), mockOverview);

      const html = await renderRoutes('/student/progress');

      // Shell
      expect(html).toContain('href="/student/progress"');
      // Must not redirect to /login
      expect(html).not.toContain('Đang kiểm tra phiên đăng nhập');
      // Progress content
      expect(html).toContain('Tiến độ học tập');
      expect(html).toContain('Biểu đồ hoạt động');
      expect(html).toContain('Streak hiện tại');
    });

    it('/student/progress redirects unauthenticated user to /login', async () => {
      queryClient.setQueryData(['auth', 'me'], null);
      useAuthStore.setState({
        isAuthenticated: false,
        currentUser: null,
      });

      const html = await renderRoutes('/student/progress');
      expect(html).not.toContain('href="/student/progress"');
    });
  });
});
