import React from 'react';
import { describe, it, expect, beforeEach } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import '@/shared/i18n';
import { SummaryCard } from '../components/SummaryCard';
import { useAuthStore } from '@/features/auth/store/authStore';
import type { UserProfileDto } from '@/shared/api/openapi.types';

const baseUser: UserProfileDto = {
  id: 'u-1',
  username: 'learner1',
  email: 'learner1@lingomint.local',
  fullName: 'Learner One',
  preferredCefrLevel: 'B1',
  preferredVoiceName: 'Aoede',
  timeZoneId: 'Asia/Ho_Chi_Minh',
  isActive: true,
  roles: ['LEARNER'],
  permissions: ['vocabularies.read'],
};

describe('SummaryCard & Permission Visibility', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    (useAuthStore as any).getInitialState = () => useAuthStore.getState();
    (useAuthStore as any).getServerSnapshot = () => useAuthStore.getState();
    queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    queryClient.setQueryData(['auth', 'me'], baseUser);
    useAuthStore.setState({
      currentUser: baseUser,
      isAuthenticated: true,
      accessToken: 'token-123',
    });
  });

  const renderWithProviders = (ui: React.ReactElement) => {
    return renderToStaticMarkup(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>{ui}</MemoryRouter>
      </QueryClientProvider>
    );
  };

  it('renders link when user HAS the required permission', () => {
    // baseUser has 'vocabularies.read'
    const html = renderWithProviders(
      <SummaryCard
        title="Từ vựng"
        icon={<span data-testid="icon">icon</span>}
        href="/student/vocabulary"
        permission="vocabularies.read"
        stats={[
          { label: 'Từ đã học', value: 45 },
          { label: 'Thành thạo', value: 20 },
        ]}
      />
    );

    expect(html).toContain('href="/student/vocabulary"');
    expect(html).toContain('Đi tới trang');
    expect(html).toContain('45');
    expect(html).toContain('20');
  });

  it('HIDES link when user LACKS the required permission, but still renders card and stats', () => {
    // baseUser does NOT have 'conversations.access'
    const html = renderWithProviders(
      <SummaryCard
        title="Live Tutor"
        icon={<span data-testid="icon">icon</span>}
        href="/student/tutor"
        permission="conversations.access"
        stats={[
          { label: 'Tổng phiên', value: 8 },
          { label: 'Trong khoảng', value: 3 },
        ]}
      />
    );

    // Link must NOT be present
    expect(html).not.toContain('href="/student/tutor"');
    expect(html).not.toContain('Đi tới trang');
    // Card title & stats must still be rendered
    expect(html).toContain('Live Tutor');
    expect(html).toContain('8');
    expect(html).toContain('3');
  });

  it('displays "—" for null or undefined stats', () => {
    const html = renderWithProviders(
      <SummaryCard
        title="Bài học"
        icon={<span data-testid="icon">icon</span>}
        href="/student/lessons"
        stats={[
          { label: 'Lượt hoàn thành', value: 0 },
          { label: 'Điểm TB', value: null },
        ]}
      />
    );

    expect(html).toContain('0');
    expect(html).toContain('—');
  });

  it('renders grammar level progress bars and current step code', () => {
    const html = renderWithProviders(
      <SummaryCard
        title="Ngữ pháp"
        icon={<span data-testid="icon">icon</span>}
        href="/student/grammar"
        permission="vocabularies.read" // has permission
        stats={[
          { label: 'Tiến độ cấp độ', value: '15 / 30' },
          { label: 'Step hiện tại', value: 'Step A1-02' },
        ]}
        grammarData={{
          levels: [
            { level: 'A1', passedSteps: 10, totalSteps: 15 },
            { level: 'A2', passedSteps: 5, totalSteps: 15 },
          ],
          currentStepCode: 'A1-02',
        }}
      />
    );

    expect(html).toContain('A1');
    expect(html).toContain('10 / 15');
    expect(html).toContain('A2');
    expect(html).toContain('5 / 15');
    expect(html).toContain('Step A1-02');
  });
});
