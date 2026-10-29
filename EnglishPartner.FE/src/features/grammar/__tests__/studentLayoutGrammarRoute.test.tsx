import React from 'react';
import { describe, expect, it, beforeEach } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import '@/shared/i18n';
import { StudentLayout, isStudentGrammarGroupRoute } from '@/app/layouts/StudentLayout';
import { AppRoutes } from '@/app/router';
import { useAuthStore } from '@/features/auth/store/authStore';
import type { UserProfileDto } from '@/shared/api/openapi.types';

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
  permissions: ['grammar.read', 'grammar.attempt', 'grammar.generate', 'conversations.access'],
};

describe('Student Menu "Ngữ pháp" & Real Router Verification', () => {
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

  describe('isStudentGrammarGroupRoute helper function', () => {
    it.each([
      ['/student/grammar'],
      ['/student/grammar/'],
      ['/student/grammar/A1-01'],
      ['/student/grammar/A1-01/'],
      ['/student/grammar/attempts/att-123'],
      ['/student/grammar/attempts/att-123/'],
      ['/student/grammar/my-exercises'],
      ['/student/grammar/my-exercises/'],
    ])('matches grammar group route: %s', (path) => {
      expect(isStudentGrammarGroupRoute(path)).toBe(true);
    });

    it.each([
      ['/student'],
      ['/student/tutor'],
      ['/student/vocabulary'],
      ['/student/lessons'],
      ['/student/grammar-other'],
      ['/admin/grammar'],
      ['/grammar'],
    ])('does NOT match unrelated routes: %s', (path) => {
      expect(isStudentGrammarGroupRoute(path)).toBe(false);
    });
  });

  describe('Real StudentLayout rendering & active states', () => {
    const renderLayout = (initialPath: string) => {
      return renderToStaticMarkup(
        <QueryClientProvider client={queryClient}>
          <MemoryRouter initialEntries={[initialPath]}>
            <StudentLayout />
          </MemoryRouter>
        </QueryClientProvider>
      );
    };

    it('renders real StudentLayout with active styling and aria-current="page" on /student/grammar', () => {
      const html = renderLayout('/student/grammar');
      expect(html).toContain('href="/student/grammar"');
      expect(html).toContain('aria-current="page"');
      expect(html).toContain('bg-emerald-600');
    });

    it('renders real StudentLayout with active styling on /student/grammar/A1-01', () => {
      const html = renderLayout('/student/grammar/A1-01');
      expect(html).toContain('href="/student/grammar"');
      expect(html).toContain('aria-current="page"');
      expect(html).toContain('bg-emerald-600');
    });

    it('renders real StudentLayout with inactive styling on /student/tutor', () => {
      const html = renderLayout('/student/tutor');
      expect(html).toContain('href="/student/grammar"');
      // On /student/tutor, the grammar link should NOT have aria-current="page"
      // Verify that the grammar link does not have aria-current="page"
      const grammarLinkIndex = html.indexOf('href="/student/grammar"');
      expect(grammarLinkIndex).toBeGreaterThan(-1);
      const linkSegment = html.slice(grammarLinkIndex - 100, grammarLinkIndex + 200);
      expect(linkSegment).not.toContain('aria-current="page"');
    });
  });

  describe('Real AppRoutes resolution for /student/grammar', () => {
    const renderRoutes = async (initialPath: string) => {
      // Ensure module is in Node module cache
      await import('@/features/grammar');

      // Trigger lazy component load in React
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

    it('/student/grammar renders roadmap page and does not redirect to /login or /student/tutor', async () => {
      queryClient.setQueryData(['grammar', 'roadmap'], {
        levels: [
          {
            level: 'A1',
            isUnlocked: true,
            isPassed: false,
            canTakeLevelTest: false,
            steps: [],
          },
        ],
      });

      const html = await renderRoutes('/student/grammar');

      // Must be within StudentLayout shell
      expect(html).toContain('href="/student/grammar"');
      // Must not redirect to /login
      expect(html).not.toContain('Đang kiểm tra phiên đăng nhập');
      // Must render grammar roadmap header or roadmap content
      expect(html).toContain('Cấp độ A1');
      expect(html).toContain('Bài tập của tôi');
    });

    it('/student/grammar blocks user without grammar.read permission with 403', async () => {
      const noGrammarUser = {
        ...mockLearner,
        permissions: ['conversations.access'],
      };
      queryClient.setQueryData(['auth', 'me'], noGrammarUser);
      useAuthStore.setState({
        currentUser: noGrammarUser,
      });

      const html = await renderRoutes('/student/grammar');
      expect(html).toContain('Không đủ quyền truy cập (403)');
      expect(html).toContain('grammar.read');
    });

    it('/student/grammar redirects unauthenticated user to /login', async () => {
      queryClient.setQueryData(['auth', 'me'], null);
      useAuthStore.setState({
        isAuthenticated: false,
        currentUser: null,
      });

      const html = await renderRoutes('/student/grammar');
      // RequireAuth renders redirect to /login (which renders LoginPage or Navigate)
      expect(html).not.toContain('href="/student/grammar"');
    });
  });
});
