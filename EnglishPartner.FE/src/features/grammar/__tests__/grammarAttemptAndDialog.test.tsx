import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import '@/shared/i18n';
import { GrammarAttemptPage } from '../pages/GrammarAttemptPage';
import { GenerateGrammarExercisesDialog } from '../components/GenerateGrammarExercisesDialog';
import { MyGrammarExercisesPage } from '../pages/MyGrammarExercisesPage';
import { grammarApi } from '../api/grammarApi';
import { GrammarAttemptRoute } from '@/app/router';
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog';
import {
  buildGrammarSubmitPayload,
  parseJsonObjectField,
  validateGenerateItems,
  getGrammarErrorMessage,
  shouldConfirmGrammarSubmit,
  canRedoGrammarExercise,
  isGrammarAnswerLocked,
  extractGrammarRejectionsFromError,
} from '../utils/grammarHelpers';
import type { GrammarAttemptDto, GrammarMyExerciseDto } from '../types/grammar.types';

vi.mock('../api/grammarApi', () => ({
  grammarApi: {
    getAttempt: vi.fn(),
    submitAnswer: vi.fn(),
    completeAttempt: vi.fn(),
    generateExercises: vi.fn(),
    buildPrompt: vi.fn(),
    importExercises: vi.fn(),
    listMyExercises: vi.fn(),
    updateMyExercise: vi.fn(),
    changeMyExerciseVisibility: vi.fn(),
    deleteMyExercise: vi.fn(),
  },
}));

vi.mock('@/features/catalog', () => ({
  useVocabularyGenerationModelsQuery: () => ({
    data: [{ modelCode: 'gemini-2.5-flash', modelName: 'Gemini 2.5 Flash' }],
  }),
}));

const mockTutorSession = {
  apiKey: 'test-api-key',
  aiProvider: 'gemini',
  assessmentModelCode: 'gemini-2.5-flash',
  agyModelCode: 'gemini-3.8-flash-high',
  requestFocusLeftMenuApiKey: vi.fn(),
};

vi.mock('@/features/tutor', () => ({
  useTutorSessionStore: () => mockTutorSession,
}));

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: false },
  },
});

function renderWithProviders(ui: React.ReactElement, initialEntry = '/') {
  return renderToStaticMarkup(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialEntry]}>
        {ui}
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('GrammarAttemptPage, GenerateDialog & MyExercisesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Pure Helpers: buildGrammarSubmitPayload, parseJsonObjectField, validateGenerateItems', () => {
    it('buildGrammarSubmitPayload does NOT include aiConfig when apiKey is empty or whitespace or AGY without key', () => {
      const payloadNoKey = buildGrammarSubmitPayload('ex-1', { text: 'ans' }, 'gemini', '', 'gemini-2.5-flash');
      expect(payloadNoKey.aiConfig).toBeUndefined();

      const payloadWhitespaceKey = buildGrammarSubmitPayload('ex-1', { text: 'ans' }, 'gemini', '   ', 'gemini-2.5-flash');
      expect(payloadWhitespaceKey.aiConfig).toBeUndefined();

      const payloadNullKey = buildGrammarSubmitPayload('ex-1', { text: 'ans' }, 'gemini', null, 'gemini-2.5-flash');
      expect(payloadNullKey.aiConfig).toBeUndefined();

      // AGY without key also yields undefined aiConfig per BE requirement
      const agyNoKey = buildGrammarSubmitPayload('ex-1', { text: 'ans' }, 'agy', '', null, 'gemini-3.8-flash-high');
      expect(agyNoKey.aiConfig).toBeUndefined();

      const agyWhitespaceKey = buildGrammarSubmitPayload('ex-1', { text: 'ans' }, 'agy', '   ', null, 'gemini-3.8-flash-high');
      expect(agyWhitespaceKey.aiConfig).toBeUndefined();
    });

    it('buildGrammarSubmitPayload includes LessonAiExecutionConfigDto when apiKey is valid', () => {
      const geminiPayload = buildGrammarSubmitPayload(
        'ex-1',
        { text: 'ans' },
        'gemini',
        'valid-gemini-key',
        'gemini-2.5-flash'
      );
      expect(geminiPayload.aiConfig).toEqual({
        provider: 'gemini',
        assessmentModelCode: 'gemini-2.5-flash',
        apiKey: 'valid-gemini-key',
      });

      const agyPayload = buildGrammarSubmitPayload(
        'ex-2',
        { text: 'ans' },
        'agy',
        'valid-agy-key',
        'gemini-2.5-flash',
        'gemini-3.8-flash-high'
      );
      expect(agyPayload.aiConfig).toEqual({
        provider: 'agy',
        assessmentModelCode: 'gemini-3.8-flash-high',
        apiKey: 'valid-agy-key',
      });
    });

    it('getGrammarErrorMessage extracts error detail from real axios response error objects', () => {
      const t = (k: string) => k;
      const err409 = { response: { status: 409, data: { detail: 'grammar_pending_limit' } } };
      expect(getGrammarErrorMessage(err409, t)).toBe('grammar.errors.pendingLimit');

      const err429 = { response: { status: 429, data: { detail: 'grammar_daily_quota_exceeded' } } };
      expect(getGrammarErrorMessage(err429, t)).toBe('grammar.errors.dailyQuotaExceeded');

      const err503 = { response: { status: 503, data: { detail: 'grammar_ai_provider_error_503' } } };
      expect(getGrammarErrorMessage(err503, t)).toBe('grammar.errors.aiProviderError');

      const errStepNoEx = { response: { status: 400, data: { detail: 'grammar_step_has_no_exercises' } } };
      expect(getGrammarErrorMessage(errStepNoEx, t)).toBe('grammar.errors.stepHasNoExercises');

      const errDup = { response: { status: 400, data: { detail: 'grammar_duplicate_content' } } };
      expect(getGrammarErrorMessage(errDup, t)).toBe('grammar.errors.duplicateContent');

      const errUnknown = { response: { status: 500, data: { detail: 'some_unknown_error_code' } } };
      expect(getGrammarErrorMessage(errUnknown, t)).toBe('grammar.errors.generic');
    });

    it('pure submission logic: shouldConfirmGrammarSubmit and canRedoGrammarExercise', () => {
      // shouldConfirmGrammarSubmit:
      // StepCore / LevelTest unsubmitted -> true
      expect(shouldConfirmGrammarSubmit('StepCore', null)).toBe(true);
      expect(shouldConfirmGrammarSubmit('LevelTest', undefined)).toBe(true);
      // FailedGrading -> true (allows resubmitting with confirm)
      expect(shouldConfirmGrammarSubmit('StepCore', 'FailedGrading')).toBe(true);
      expect(shouldConfirmGrammarSubmit('LevelTest', 'FailedGrading')).toBe(true);
      // Completed / Already submitted -> false
      expect(shouldConfirmGrammarSubmit('StepCore', 'Completed')).toBe(false);
      expect(shouldConfirmGrammarSubmit('LevelTest', 'SelfAssessed')).toBe(false);
      // Practice -> false (no confirmation needed)
      expect(shouldConfirmGrammarSubmit('Practice', null)).toBe(false);
      expect(shouldConfirmGrammarSubmit('Practice', 'Completed')).toBe(false);

      // canRedoGrammarExercise:
      // Practice mode allows redo anytime
      expect(canRedoGrammarExercise('Practice', 'Completed')).toBe(true);
      expect(canRedoGrammarExercise('Practice', null)).toBe(true);
      // StepCore and LevelTest only allow redo on FailedGrading
      expect(canRedoGrammarExercise('StepCore', 'FailedGrading')).toBe(true);
      expect(canRedoGrammarExercise('LevelTest', 'FailedGrading')).toBe(true);
      expect(canRedoGrammarExercise('StepCore', 'Completed')).toBe(false);
      expect(canRedoGrammarExercise('LevelTest', 'Completed')).toBe(false);

      // isGrammarAnswerLocked:
      expect(isGrammarAnswerLocked('Practice', 'Completed')).toBe(false);
      expect(isGrammarAnswerLocked('StepCore', 'Completed')).toBe(true);
      expect(isGrammarAnswerLocked('StepCore', 'FailedGrading')).toBe(false);
      expect(isGrammarAnswerLocked('StepCore', null)).toBe(false);
    });

    it('extractGrammarRejectionsFromError parses rejected array from 422 ProblemDetails', () => {
      const errWithRejected = {
        response: {
          status: 422,
          data: {
            rejected: [{ index: 0, issues: ['Missing question field'] }],
          },
        },
      };
      expect(extractGrammarRejectionsFromError(errWithRejected)).toEqual([
        { index: 0, issues: ['Missing question field'] },
      ]);

      const errWithExtensions = {
        response: {
          status: 422,
          data: {
            extensions: {
              rejected: [{ index: 1, issues: ['Invalid difficulty'] }],
            },
          },
        },
      };
      expect(extractGrammarRejectionsFromError(errWithExtensions)).toEqual([
        { index: 1, issues: ['Invalid difficulty'] },
      ]);

      expect(extractGrammarRejectionsFromError(null)).toEqual([]);
      expect(extractGrammarRejectionsFromError({})).toEqual([]);
    });

    it('parseJsonObjectField validates strictly for objects and rejects null, [], "x", 5, and syntax errors', () => {
      expect(parseJsonObjectField('{"valid": true}')).toEqual({
        ok: true,
        value: { valid: true },
      });

      expect(parseJsonObjectField('null')).toEqual({
        ok: false,
        errorKey: 'grammar.editErrors.mustBeObject',
      });

      expect(parseJsonObjectField('[1, 2, 3]')).toEqual({
        ok: false,
        errorKey: 'grammar.editErrors.mustBeObject',
      });

      expect(parseJsonObjectField('"just a string"')).toEqual({
        ok: false,
        errorKey: 'grammar.editErrors.mustBeObject',
      });

      expect(parseJsonObjectField('5')).toEqual({
        ok: false,
        errorKey: 'grammar.editErrors.mustBeObject',
      });

      expect(parseJsonObjectField('{broken')).toEqual({
        ok: false,
        errorKey: 'grammar.editErrors.invalidJson',
      });

      expect(parseJsonObjectField('', 'optionalField', true)).toEqual({
        ok: true,
        value: null,
      });

      expect(parseJsonObjectField('', 'requiredField', false)).toEqual({
        ok: false,
        errorKey: 'grammar.editErrors.required',
      });
    });

    it('validateGenerateItems validates bounds (total > 0 and <= 10)', () => {
      expect(validateGenerateItems([])).toEqual({
        valid: false,
        totalCount: 0,
        errorKey: 'grammar.selectAtLeastOneExercise',
      });

      expect(
        validateGenerateItems([{ type: 'MultipleChoice', count: 12, difficulty: 2 }])
      ).toEqual({
        valid: false,
        totalCount: 12,
        errorKey: 'grammar.max10Exercises',
      });

      expect(
        validateGenerateItems([
          { type: 'MultipleChoice', count: 4, difficulty: 1 },
          { type: 'FillInTheBlanks', count: 3, difficulty: 2 },
        ])
      ).toEqual({
        valid: true,
        totalCount: 7,
      });
    });
  });

  describe('GrammarAttemptPage UI & States', () => {
    const mockAttempt: GrammarAttemptDto = {
      id: 'att-123',
      kind: 'StepCore',
      stepCode: 'A1-01',
      cefrLevel: 'A1',
      startedAtUtc: '2026-10-29T10:00:00Z',
      exercises: [
        {
          id: 'ex-1',
          key: 'mc-1',
          type: 'MultipleChoice',
          title: 'Câu hỏi 1',
          instructions: 'Chọn đáp án đúng',
          order: 1,
          maxPoints: 10,
          gradingMode: 'Deterministic',
          content: {
            question: 'I ___ a student.',
            options: [
              { id: 'opt-a', text: 'am' },
              { id: 'opt-b', text: 'is' },
            ],
            selectionMode: 'Single',
          },
          source: 'Official',
          visibility: 'Public',
          moderation: 'Approved',
        },
        {
          id: 'ex-2',
          key: 'trans-1',
          type: 'Translation',
          title: 'Câu hỏi 2: Dịch câu',
          instructions: 'Dịch sang tiếng Anh',
          order: 2,
          maxPoints: 10,
          gradingMode: 'AiRubric',
          content: {
            sourceText: 'Tôi là giáo viên.',
          },
          source: 'Official',
          visibility: 'Public',
          moderation: 'Approved',
        },
      ],
      answers: [],
    };

    it('renders attempt player with question navigation and one-time submit notice for StepCore', () => {
      queryClient.setQueryData(['grammar', 'attempt', 'att-123'], mockAttempt);

      const html = renderWithProviders(
        <Routes>
          <Route path="/student/grammar/attempts/:id" element={<GrammarAttemptPage />} />
        </Routes>,
        '/student/grammar/attempts/att-123'
      );

      expect(html).toContain('Bài tập cốt lõi: A1-01');
      expect(html).toContain('Bài thi này chỉ cho phép nộp mỗi câu 1 lần.');
      expect(html).toContain('Câu 1 / 2');
      expect(html).toContain('I ___ a student.');
      expect(html).toContain('Hoàn thành bài thi');
    });

    it('renders "Bài tập không còn khả dụng" when exercise has unavailable: true', () => {
      const unavailableAttempt: GrammarAttemptDto = {
        ...mockAttempt,
        exercises: [
          {
            ...mockAttempt.exercises[0],
            unavailable: true,
          },
        ],
      };
      queryClient.setQueryData(['grammar', 'attempt', 'att-unavail'], unavailableAttempt);

      const html = renderWithProviders(
        <Routes>
          <Route path="/student/grammar/attempts/:id" element={<GrammarAttemptPage />} />
        </Routes>,
        '/student/grammar/attempts/att-unavail'
      );

      expect(html).toContain('Bài tập không còn khả dụng');
    });

    it('renders SelfAssessed banner with suggestedAnswer when status is SelfAssessed', () => {
      const selfAssessedAttempt: GrammarAttemptDto = {
        ...mockAttempt,
        answers: [
          {
            exerciseId: 'ex-1',
            status: 'SelfAssessed',
            score100: 100,
            maxPoints: 10,
            suggestedAnswer: 'I am a student.',
            feedback: { explanation: 'Đáp án chuẩn' },
          },
        ],
      };
      queryClient.setQueryData(['grammar', 'attempt', 'att-self'], selfAssessedAttempt);

      const html = renderWithProviders(
        <Routes>
          <Route path="/student/grammar/attempts/:id" element={<GrammarAttemptPage />} />
        </Routes>,
        '/student/grammar/attempts/att-self'
      );

      expect(html).toContain('Tự đánh giá');
      expect(html).toContain('I am a student.');
    });

    it('renders completion view for reloaded completed attempt WITHOUT pass/fail badge (only shows score and restart)', () => {
      const completedAttempt: GrammarAttemptDto = {
        ...mockAttempt,
        completedAtUtc: '2026-10-29T10:15:00Z',
        score100: 85,
      };
      queryClient.setQueryData(['grammar', 'attempt', 'att-done-core'], completedAttempt);

      const html = renderWithProviders(
        <Routes>
          <Route path="/student/grammar/attempts/:id" element={<GrammarAttemptPage />} />
        </Routes>,
        '/student/grammar/attempts/att-done-core'
      );

      // In reloaded completed attempts, completionResult is null -> FE does not show pass/fail badge or levelPassed banner
      expect(html).not.toContain('ĐẠT YÊU CẦU');
      expect(html).not.toContain('CHƯA ĐẠT');
      expect(html).toContain('Phiên làm bài đã hoàn thành');
      expect(html).toContain('85%');
      expect(html).toContain('Làm lại');
    });

    it('renders completion view for Practice WITHOUT pass/fail badge, only showing score and restart button', () => {
      const practiceAttempt: GrammarAttemptDto = {
        ...mockAttempt,
        kind: 'Practice',
        completedAtUtc: '2026-10-29T10:15:00Z',
        score100: 75,
      };
      queryClient.setQueryData(['grammar', 'attempt', 'att-done-practice'], practiceAttempt);

      const html = renderWithProviders(
        <Routes>
          <Route path="/student/grammar/attempts/:id" element={<GrammarAttemptPage />} />
        </Routes>,
        '/student/grammar/attempts/att-done-practice'
      );

      // Practice mode must not show pass/fail badge
      expect(html).not.toContain('ĐẠT YÊU CẦU');
      expect(html).not.toContain('CHƯA ĐẠT');
      expect(html).toContain('75%');
      expect(html).toContain('Làm lại');
    });

    it('renders completion view for reloaded LevelTest without completionResult (no levelPassed banner)', () => {
      const levelTestAttempt: GrammarAttemptDto = {
        ...mockAttempt,
        kind: 'LevelTest',
        cefrLevel: 'A1',
        completedAtUtc: '2026-10-29T10:15:00Z',
        score100: 90,
      };
      queryClient.setQueryData(['grammar', 'attempt', 'att-done-level'], levelTestAttempt);

      const html = renderWithProviders(
        <Routes>
          <Route path="/student/grammar/attempts/:id" element={<GrammarAttemptPage />} />
        </Routes>,
        '/student/grammar/attempts/att-done-level'
      );

      expect(html).toContain('Phiên làm bài đã hoàn thành');
      expect(html).toContain('90%');
      expect(html).not.toContain('Đã đạt toàn bộ level A1');
    });

    it('GrammarAttemptRoute renders GrammarAttemptPage with attempt id as key to reset state on redo', () => {
      let element: React.ReactElement | null = null;
      function RouteElementCapture() {
        element = GrammarAttemptRoute({}) as React.ReactElement;
        return null;
      }

      renderWithProviders(
        <Routes>
          <Route path="/student/grammar/attempts/:id" element={<RouteElementCapture />} />
        </Routes>,
        '/student/grammar/attempts/att-switch-999'
      );

      expect(element).not.toBeNull();
      expect((element as any)?.key).toBe('att-switch-999');
    });
  });

  describe('GenerateGrammarExercisesDialog UI & Rejections', () => {
    it('renders dialog with mode switch tabs (role="tablist", role="tab") and visibility selection', () => {
      const html = renderWithProviders(
        <GenerateGrammarExercisesDialog
          isOpen={true}
          onClose={() => {}}
          stepCode="A1-01"
          stepCefrLevel="A1"
        />
      );

      expect(html).toContain('Tạo bài tập ngữ pháp bằng AI');
      expect(html).toContain('A1-01');

      // Semantic tabs
      expect(html).toContain('role="tablist"');
      expect(html).toContain('role="tab"');
      expect(html).toContain('Sinh trực tiếp (AI API)');
      expect(html).toContain('Chế độ thủ công');

      // Exercise selection & counter
      expect(html).toContain('0/300');

      // Visibility options
      expect(html).toContain('Riêng tư (Chỉ mình tôi luyện tập)');
      expect(html).toContain('Công khai (Gửi admin duyệt để chia sẻ với cộng đồng)');
    });

    it('renders manual mode initially when user has no API key, displaying JSON textarea and get prompt button', () => {
      const html = renderWithProviders(
        <GenerateGrammarExercisesDialog
          isOpen={true}
          onClose={() => {}}
          stepCode="A1-01"
          stepCefrLevel="A1"
        />
      );

      expect(html).toContain('id="dialog-tabpanel-manual"');
      expect(html).toContain('id="grammar-json-input"');
      expect(html).toContain('Lấy prompt');
      expect(html).toContain('Chế độ thủ công');
    });

    it('renders moderation warning notice when visibility is Public', () => {
      const html = renderWithProviders(
        <GenerateGrammarExercisesDialog
          isOpen={true}
          onClose={() => {}}
          stepCode="A1-01"
          stepCefrLevel="A1"
          defaultVisibility="Public"
        />
      );

      expect(html).toContain('Bài tập công khai cần được quản trị viên duyệt trước khi hiển thị cho mọi người.');
    });
  });

  describe('MyGrammarExercisesPage UI & ConfirmDialog', () => {
    const mockMyExercises: GrammarMyExerciseDto[] = [
      {
        id: 'my-1',
        stepId: 'step-1',
        stepCode: 'A1-01',
        type: 'MultipleChoice',
        difficulty: 2,
        title: 'Custom Present Simple Q',
        instructions: 'Pick one',
        content: { question: 'He ___ football.' },
        grading: { correctOptionIds: ['opt-1'] },
        maxPoints: 10,
        gradingMode: 'Deterministic',
        source: 'UserAi',
        visibility: 'Public',
        moderation: 'Rejected',
        rejectReason: 'Nội dung trùng lặp với câu hỏi chính thức.',
        isCore: false,
        contentHash: 'hash123',
        createdAtUtc: '2026-10-29T08:00:00Z',
      },
    ];

    it('renders list of user exercises with reject reason callout and exercise card metadata', () => {
      queryClient.setQueryData(
        ['grammar', 'my-exercises', { stepCode: undefined, moderation: undefined, page: 1, pageSize: 10, paged: true }],
        {
          items: mockMyExercises,
          totalCount: 1,
          page: 1,
          pageSize: 10,
          totalPages: 1,
        }
      );

      const html = renderWithProviders(<MyGrammarExercisesPage />);

      expect(html).toContain('Bài tập ngữ pháp của tôi');
      expect(html).toContain('A1-01');
      expect(html).toContain('Custom Present Simple Q');
      expect(html).toContain('Bị từ chối');
      expect(html).toContain('Lý do từ chối:');
      expect(html).toContain('Nội dung trùng lặp với câu hỏi chính thức.');
    });

    it('renders ConfirmDialog with real accessible attributes when isOpen is true', () => {
      const html = renderToStaticMarkup(
        <ConfirmDialog
          isOpen={true}
          onClose={() => {}}
          onConfirm={() => {}}
          variant="danger"
          title="Xác nhận xóa bài tập"
          targetName="Custom Present Simple Q"
          targetLabel="Bài tập"
          description="Bạn có chắc muốn xóa bài tập 'Custom Present Simple Q' không? Thao tác này không thể hoàn tác."
          confirmLabel="Xóa bài tập"
          cancelLabel="Hủy"
        />
      );

      expect(html).toContain('role="dialog"');
      expect(html).toContain('aria-modal="true"');
      expect(html).toContain('Xác nhận xóa bài tập');
      expect(html).toContain('Custom Present Simple Q');
      expect(html).toContain('Xóa bài tập');
      expect(html).toContain('Hủy');
    });
  });
});
