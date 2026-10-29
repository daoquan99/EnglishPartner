import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import '@/shared/i18n';
import { GrammarRoadmapPage } from '../pages/GrammarRoadmapPage';
import { GrammarStepPage } from '../pages/GrammarStepPage';
import { grammarApi } from '../api/grammarApi';
import { getLevelTestButtonState, getGrammarErrorMessage } from '../utils/grammarHelpers';
import type { GrammarRoadmapDto, GrammarStepDetailDto } from '../types/grammar.types';

vi.mock('../api/grammarApi', () => ({
  grammarApi: {
    getRoadmap: vi.fn(),
    getStep: vi.fn(),
    listStepExercises: vi.fn(),
    startAttempt: vi.fn(),
  },
}));

vi.mock('@/features/catalog', () => ({
  useVocabularyGenerationModelsQuery: () => ({ data: [] }),
}));

vi.mock('@/features/tutor', () => ({
  useTutorSessionStore: () => ({
    apiKey: 'mock-key',
    aiProvider: 'gemini',
    agyModelCode: 'gemini-3.8-flash-high',
    requestFocusLeftMenuApiKey: vi.fn(),
  }),
}));

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: false },
  },
});

function renderWithProviders(ui: React.ReactElement, initialEntry = '/student/grammar') {
  return renderToStaticMarkup(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialEntry]}>
        {ui}
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('GrammarRoadmapPage & GrammarStepPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Pure helpers: getLevelTestButtonState & getGrammarErrorMessage', () => {
    it('evaluates level test button state correctly based on canTakeLevelTest', () => {
      const locked = getLevelTestButtonState(false);
      expect(locked.disabled).toBe(true);
      expect(locked.reasonKey).toBe('grammar.levelTestLockedReason');

      const unlocked = getLevelTestButtonState(true);
      expect(unlocked.disabled).toBe(false);
      expect(unlocked.reasonKey).toBeUndefined();
    });

    it('maps BE error codes to localized messages and prevents raw code leak', () => {
      const mockT = ((key: string) => {
        const dict: Record<string, string> = {
          'grammar.errors.stepLocked': 'Bài học này hiện đang bị khóa.',
          'grammar.errors.levelTestLocked': 'Bài kiểm tra cấp độ hiện đang bị khóa.',
          'grammar.errors.pendingLimit': 'Số lượng bài tập công khai chờ duyệt đã đạt giới hạn tối đa.',
          'grammar.errors.dailyQuotaExceeded': 'Đã vượt quá giới hạn tạo bài tập trong ngày.',
          'grammar.errors.aiTimeout': 'Quá thời gian phản hồi từ dịch vụ AI.',
          'grammar.errors.aiProviderError': 'Lỗi từ nhà cung cấp AI: {{detail}}',
          'grammar.errors.generic': 'Đã có lỗi xảy ra. Vui lòng thử lại.',
        };
        return dict[key] || key;
      }) as any;

      expect(getGrammarErrorMessage('grammar_pending_limit', mockT)).toBe(
        'Số lượng bài tập công khai chờ duyệt đã đạt giới hạn tối đa.'
      );
      expect(getGrammarErrorMessage('grammar_step_locked', mockT)).toBe(
        'Bài học này hiện đang bị khóa.'
      );
      expect(getGrammarErrorMessage('grammar_level_test_locked', mockT)).toBe(
        'Bài kiểm tra cấp độ hiện đang bị khóa.'
      );
      expect(getGrammarErrorMessage('grammar_daily_quota_exceeded', mockT)).toBe(
        'Đã vượt quá giới hạn tạo bài tập trong ngày.'
      );
      expect(getGrammarErrorMessage('grammar_ai_timeout', mockT)).toBe(
        'Quá thời gian phản hồi từ dịch vụ AI.'
      );
      expect(getGrammarErrorMessage('grammar_ai_provider_error_quota', mockT)).toContain(
        'Lỗi từ nhà cung cấp AI'
      );
      expect(getGrammarErrorMessage('unknown_internal_code', mockT)).toBe(
        'Đã có lỗi xảy ra. Vui lòng thử lại.'
      );
    });
  });

  describe('GrammarRoadmapPage UI Attributes & Keyboard Accessibility', () => {
    const mockRoadmap: GrammarRoadmapDto = {
      levels: [
        {
          level: 'A1',
          isUnlocked: true,
          isPassed: false,
          canTakeLevelTest: true,
          steps: [
            {
              code: 'A1-01',
              order: 1,
              titleVi: 'Đại từ nhân xưng & to be',
              titleEn: 'Personal Pronouns and Be',
              summaryVi: 'Cách dùng I, you, he, she, it, we, they với am, is, are.',
              isLevelReview: false,
              status: 'Passed',
              bestScore100: 95,
            },
            {
              code: 'A1-02',
              order: 2,
              titleVi: 'Thì hiện tại đơn',
              titleEn: 'Present Simple',
              summaryVi: 'Thì hiện tại đơn với động từ thường.',
              isLevelReview: false,
              status: 'Unlocked',
              bestScore100: null,
            },
            {
              code: 'A1-03',
              order: 3,
              titleVi: 'Trạng từ chỉ tần suất',
              titleEn: 'Adverbs of Frequency',
              summaryVi: 'Always, usually, never...',
              isLevelReview: false,
              status: 'Locked',
              bestScore100: null,
            },
          ],
        },
        {
          level: 'A2',
          isUnlocked: false,
          isPassed: false,
          canTakeLevelTest: false,
          steps: [
            {
              code: 'A2-01',
              order: 1,
              titleVi: 'Thì quá khứ đơn',
              titleEn: 'Past Simple',
              summaryVi: 'Ôn tập và mở rộng thì quá khứ đơn.',
              isLevelReview: false,
              status: 'Locked',
              bestScore100: null,
            },
          ],
        },
      ],
    };

    it('renders level tabs, the winding step path with real links and visible hero actions', () => {
      queryClient.setQueryData(['grammar', 'roadmap'], mockRoadmap);

      const html = renderWithProviders(<GrammarRoadmapPage />);

      expect(html).toContain('Lộ trình Ngữ pháp Tiếng Anh');
      expect(html).toContain('Cấp độ A1');

      // Level selector is an ARIA tablist; A1 (level of the next step) is selected by default
      expect(html).toContain('role="tablist"');
      expect(html).toMatch(/id="level-tab-A1"[^>]*aria-selected="true"/);
      expect(html).toMatch(/id="level-tab-A2"[^>]*aria-selected="false"/);
      expect(html).toContain('id="level-panel-A1"');
      expect(html).toContain('aria-labelledby="level-tab-A1"');

      // Step path: one link per step, current step marked
      expect(html).toContain('href="/student/grammar/A1-01"');
      expect(html).toContain('href="/student/grammar/A1-02"');
      expect(html).toMatch(/<a aria-current="step"[^>]*href="\/student\/grammar\/A1-02"/);

      // Hero actions are links with explicit readable colours (no white-on-white button variants)
      expect(html).toMatch(/<a class="[^"]*bg-white [^"]*text-emerald-800[^"]*" href="\/student\/grammar\/A1-02"/);
      expect(html).toMatch(/<a class="[^"]*bg-white\/10[^"]*text-white[^"]*" href="\/student\/grammar\/my-exercises"/);
      expect(html).toContain('Tiếp tục học: A1-02');
    });

    it('enables the level test only when canTakeLevelTest is true, with an inline reason otherwise', () => {
      queryClient.setQueryData(['grammar', 'roadmap'], mockRoadmap);
      const enabledHtml = renderWithProviders(<GrammarRoadmapPage />);
      const a1Index = enabledHtml.indexOf('Kiểm tra level A1');
      expect(a1Index).toBeGreaterThan(-1);
      expect(enabledHtml.slice(enabledHtml.lastIndexOf('<button', a1Index), a1Index)).not.toContain('disabled=""');

      const lockedRoadmap = {
        levels: mockRoadmap.levels.map((l) => (l.level === 'A1' ? { ...l, canTakeLevelTest: false } : l)),
      };
      queryClient.setQueryData(['grammar', 'roadmap'], lockedRoadmap);
      const disabledHtml = renderWithProviders(<GrammarRoadmapPage />);
      const lockedIndex = disabledHtml.indexOf('Kiểm tra level A1');
      expect(disabledHtml.slice(disabledHtml.lastIndexOf('<button', lockedIndex), lockedIndex)).toContain('disabled=""');
      expect(disabledHtml).toContain('Cần vượt qua level trước hoặc hoàn thành các bài học trước để làm bài kiểm tra cấp độ.');
    });

    it('displays "Chưa mở bài tập" badge for Locked step A1-03', () => {
      queryClient.setQueryData(['grammar', 'roadmap'], mockRoadmap);

      const html = renderWithProviders(<GrammarRoadmapPage />);

      expect(html).toContain('A1-03');
      expect(html).toContain('Chưa mở bài tập');
    });
  });

  describe('GrammarStepPage UI Attributes & Semantic Tabs', () => {
    const mockStepDetail: GrammarStepDetailDto = {
      code: 'A1-02',
      cefrLevel: 'A1',
      order: 2,
      titleVi: 'Thì hiện tại đơn',
      titleEn: 'Present Simple',
      summaryVi: 'Học thì hiện tại đơn với động từ thường',
      isLevelReview: false,
      theory: {
        objectives: ['Master Present Simple'],
        sections: [
          {
            heading: 'Cấu trúc',
            explanationVi: 'Giải thích cấu trúc',
          },
        ],
        summary: ['Tóm tắt'],
      },
      status: 'Unlocked',
      bestScore100: 80,
      coreExerciseCount: 5,
      communityExerciseCount: 12,
      myExerciseCount: 3,
      prevCode: 'A1-01',
      nextCode: 'A1-03',
    };

    it('renders step details with semantic tablist and tabs (role="tablist", role="tab", aria-selected)', () => {
      queryClient.setQueryData(['grammar', 'step', 'A1-02'], mockStepDetail);

      const html = renderWithProviders(
        <Routes>
          <Route path="/student/grammar/:code" element={<GrammarStepPage />} />
        </Routes>,
        '/student/grammar/A1-02?tab=exercises'
      );

      expect(html).toContain('A1-02');
      expect(html).toContain('Thì hiện tại đơn');

      // Semantic tabs
      expect(html).toContain('role="tablist"');
      expect(html).toContain('role="tab"');
      expect(html).toContain('aria-selected="true"');
      expect(html).toContain('role="tabpanel"');

      // Tab contents
      expect(html).toContain('Lý thuyết');
      expect(html).toContain('Bài tập cốt lõi (5)');
      expect(html).toContain('Luyện thêm (15)');
      expect(html).toContain('5 bài');
      expect(html).toContain('≥ 70%');
      expect(html).toContain('Bắt đầu làm bài');
    });

    it('always renders StepCore button and displays secondary level review test button when isLevelReview is true', () => {
      const reviewStep: GrammarStepDetailDto = {
        ...mockStepDetail,
        code: 'A1-20',
        isLevelReview: true,
      };
      queryClient.setQueryData(['grammar', 'step', 'A1-20'], reviewStep);
      queryClient.setQueryData(['grammar', 'roadmap'], {
        levels: [
          {
            level: 'A1',
            isUnlocked: true,
            isPassed: false,
            canTakeLevelTest: true,
            steps: [],
          },
        ],
      });

      const html = renderWithProviders(
        <Routes>
          <Route path="/student/grammar/:code" element={<GrammarStepPage />} />
        </Routes>,
        '/student/grammar/A1-20?tab=exercises'
      );

      // Both StepCore and LevelTest buttons must be rendered
      expect(html).toContain('Bắt đầu làm bài');
      expect(html).toContain('Kiểm tra level A1');
    });
  });
});

import { computeStepPathPoints } from '../components/GrammarStepPath';

describe('computeStepPathPoints', () => {
  it('winds left and right and puts each card on the side away from the curve', () => {
    const points = computeStepPathPoints(9);
    const xs = points.map((p) => Math.round(p.x));
    expect(xs[0]).toBe(50);
    expect(Math.max(...xs)).toBeGreaterThan(70);
    expect(Math.min(...xs)).toBeLessThan(30);
    points.forEach((p) => {
      if (p.x > 50) expect(p.side).toBe('left');
      if (p.x < 50) expect(p.side).toBe('right');
    });
    expect(points[1].y - points[0].y).toBeGreaterThan(0);
  });
});
