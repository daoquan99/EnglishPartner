import React from 'react';
import { describe, expect, it, beforeEach, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import '@/shared/i18n';
import { StudentLayout } from '../StudentLayout';
import { AdminLayout } from '../AdminLayout';
import { useAuthStore } from '@/features/auth/store/authStore';
import type { UserProfileDto } from '@/shared/api/openapi.types';

vi.mock('@/shared/config/features', () => ({ isAgyLocalEnabled: false }));

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
  permissions: ['conversations.access', 'vocabularies.read', 'grammar.read'],
};

const mockAdmin: UserProfileDto = {
  ...mockLearner,
  id: 'admin-1',
  username: 'admin1',
  roles: ['ADMIN'],
  permissions: ['users.read', 'roles.manage', 'prompts.manage'],
};

describe('Mobile Responsive Layouts (StudentLayout & AdminLayout)', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    (useAuthStore as any).getInitialState = () => useAuthStore.getState();
    (useAuthStore as any).getServerSnapshot = () => useAuthStore.getState();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
  });

  describe('StudentLayout Mobile Responsive Shell', () => {
    beforeEach(() => {
      queryClient.setQueryData(['auth', 'me'], mockLearner);
      useAuthStore.setState({
        isAuthenticated: true,
        isInitialized: true,
        currentUser: mockLearner,
        accessToken: 'test-token',
      });
    });

    const renderStudentLayout = (initialPath = '/student/tutor') => {
      return renderToStaticMarkup(
        <QueryClientProvider client={queryClient}>
          <MemoryRouter initialEntries={[initialPath]}>
            <StudentLayout />
          </MemoryRouter>
        </QueryClientProvider>
      );
    };

    it('renders mobile menu trigger button with aria-controls, aria-expanded, and min 44x44 target', () => {
      const html = renderStudentLayout();

      expect(html).toContain('aria-controls="student-sidebar"');
      expect(html).toContain('aria-expanded="false"');
      expect(html).toContain('aria-label="Mở menu điều hướng"');
      expect(html).toContain('min-h-[44px]');
      expect(html).toContain('min-w-[44px]');
    });

    it('renders mobile top bar with brand logo without brightness-0 or invert', () => {
      const html = renderStudentLayout();

      expect(html).toContain('src="/assets/brand/logo-mark.svg"');
      // Must not invert or filter the brand logo
      expect(html).not.toContain('brightness-0');
      expect(html).not.toContain('invert');
    });

    it('renders sidebar with id="student-sidebar" and responsive drawer off-canvas classes', () => {
      const html = renderStudentLayout();

      expect(html).toContain('id="student-sidebar"');
      expect(html).toContain('fixed inset-y-0');
      expect(html).toContain('-translate-x-full');
      expect(html).toContain('w-72 max-w-[85vw]');
      expect(html).toContain('lg:static');
      expect(html).toContain('lg:translate-x-0');
    });

    it('renders desktop collapse button hidden on mobile, and mobile close button hidden on desktop', () => {
      const html = renderStudentLayout();

      expect(html).toContain('hidden lg:flex');
      expect(html).toContain('flex lg:hidden');
      expect(html).toContain('aria-label="Đóng menu điều hướng"');
    });

    it('keeps main content padding p-4 sm:p-6', () => {
      const html = renderStudentLayout();

      expect(html).toContain('p-4 sm:p-6');
    });
  });

  describe('AdminLayout Mobile Responsive Shell', () => {
    beforeEach(() => {
      queryClient.setQueryData(['auth', 'me'], mockAdmin);
      useAuthStore.setState({
        isAuthenticated: true,
        isInitialized: true,
        currentUser: mockAdmin,
        accessToken: 'test-admin-token',
      });
    });

    const renderAdminLayout = (initialPath = '/admin') => {
      return renderToStaticMarkup(
        <QueryClientProvider client={queryClient}>
          <MemoryRouter initialEntries={[initialPath]}>
            <AdminLayout />
          </MemoryRouter>
        </QueryClientProvider>
      );
    };

    it('renders mobile menu trigger button with aria-controls, aria-expanded, and min 44x44 target', () => {
      const html = renderAdminLayout();

      expect(html).toContain('aria-controls="admin-sidebar"');
      expect(html).toContain('aria-expanded="false"');
      expect(html).toContain('aria-label="Mở menu điều hướng"');
      expect(html).toContain('min-h-[44px]');
      expect(html).toContain('min-w-[44px]');
    });

    it('renders mobile top bar with brand logo in admin dark theme without invert', () => {
      const html = renderAdminLayout();

      expect(html).toContain('src="/assets/brand/logo-mark.svg"');
      expect(html).not.toContain('brightness-0');
      expect(html).not.toContain('invert');
      // Admin top bar uses dark theme slate-900
      expect(html).toContain('bg-slate-900/95');
    });

    it('renders sidebar with id="admin-sidebar" and responsive drawer off-canvas classes', () => {
      const html = renderAdminLayout();

      expect(html).toContain('id="admin-sidebar"');
      expect(html).toContain('fixed inset-y-0');
      expect(html).toContain('-translate-x-full');
      expect(html).toContain('w-72 max-w-[85vw]');
      expect(html).toContain('lg:static');
      expect(html).toContain('lg:translate-x-0');
    });

    it('renders desktop collapse button hidden on mobile, and mobile close button hidden on desktop', () => {
      const html = renderAdminLayout();

      expect(html).toContain('hidden lg:flex');
      expect(html).toContain('flex lg:hidden');
      expect(html).toContain('aria-label="Đóng menu điều hướng"');
    });

    it('keeps main content padding p-4 sm:p-6', () => {
      const html = renderAdminLayout();

      expect(html).toContain('p-4 sm:p-6');
    });
  });
});
