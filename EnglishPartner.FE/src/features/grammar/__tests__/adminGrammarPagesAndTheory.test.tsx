import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import '@/shared/i18n';
import {
  createEmptyTheory,
  addObjectiveToTheory,
  removeObjectiveFromTheory,
  reorderObjectivesInTheory,
  addSectionToTheory,
  removeSectionFromTheory,
  reorderSectionsInTheory,
  updateSectionInTheory,
  addMistakeToTheory,
  removeMistakeFromTheory,
  addTipToTheory,
  removeTipFromTheory,
  convertTheoryBetweenModes,
  validateTheoryJson,
  getGrammarErrorMessage,
  validateGenerateItems,
  groupIssuesByField,
  getModerationActionButtons,
  getInvalidationKeysFor,
  validateRejectReason,
  hasAnyTheoryContent,
} from '../utils/grammarHelpers';
import { AdminGrammarModerationPage } from '../pages/admin/AdminGrammarModerationPage';
import { RejectReasonDialog } from '../components/admin/RejectReasonDialog';
import { AppRoutes } from '@/app/router';
import { adminGrammarApi } from '../api/adminGrammarApi';
import { useAuthStore } from '@/features/auth/store/authStore';
import type { UserProfileDto, GrammarTheoryDto } from '@/shared/api/openapi.types';

const mockStepData = {
  id: 'step-1',
  code: 'A1-01',
  cefrLevel: 'A1',
  order: 1,
  titleVi: 'Thì hiện tại đơn',
  titleEn: 'Present Simple',
  summaryVi: 'Tóm tắt bài học',
  isPublished: true,
  theoryEditedByAdmin: false,
  theoryVersion: 1,
  theory: { sections: [{ heading: 'Khẳng định', explanationVi: 'Chủ ngữ + V' }] },
  officialExercises: [],
};

const mockQueueItem = {
  id: 'ex-mod-1',
  stepId: 'step-1',
  stepCode: 'A1-01',
  cefrLevel: 'A1',
  authorName: 'Learner One',
  type: 'MultipleChoice',
  difficulty: 1,
  title: 'Câu hỏi trắc nghiệm',
  instructions: 'Chọn đáp án đúng',
  maxPoints: 10,
  gradingMode: 'Deterministic',
  visibility: 'Public',
  content: { question: 'I ___ a student.' },
  grading: { correctOptionIds: ['1'] },
  moderation: 'Pending',
  createdAtUtc: '2026-10-29T10:00:00Z',
};

const mockQueueData = {
  items: [mockQueueItem],
  totalCount: 1,
  page: 1,
  pageSize: 10,
  totalPages: 1,
};

vi.mock('../api/adminGrammarApi', () => ({
  adminGrammarApi: {
    listSteps: vi.fn(),
    getStep: vi.fn(),
    listModerationQueue: vi.fn(),
    approveExercise: vi.fn(),
    rejectExercise: vi.fn(),
    promoteExercise: vi.fn(),
    draftTheoryWithAi: vi.fn(),
    generateOfficialExercises: vi.fn(),
    importOfficialExercises: vi.fn(),
    buildOfficialPrompt: vi.fn(),
  },
}));

vi.mock('@/features/catalog', () => ({
  useVocabularyGenerationModelsQuery: () => ({ data: [] }),
  useConversationModesQuery: () => ({ data: [] }),
}));

vi.mock('@/features/tutor', () => ({
  useTutorSessionStore: () => ({
    apiKey: 'mock-key',
    aiProvider: 'gemini',
    agyModelCode: 'gemini-3.8-flash-high',
    isAdminSidebarCollapsed: false,
    toggleAdminSidebar: vi.fn(),
    requestFocusLeftMenuApiKey: vi.fn(),
  }),
}));

const mockAdminUser: UserProfileDto = {
  id: 'admin-1',
  username: 'admin',
  email: 'admin@lingomint.local',
  fullName: 'Admin User',
  preferredCefrLevel: 'C1',
  preferredVoiceName: 'Aoede',
  timeZoneId: 'Asia/Ho_Chi_Minh',
  isActive: true,
  roles: ['ADMIN'],
  permissions: [
    'users.read',
    'users.manage',
    'roles.manage',
    'grammar.manage',
    'grammar.moderate',
  ],
};

const mockModeratorOnlyUser: UserProfileDto = {
  ...mockAdminUser,
  id: 'mod-1',
  roles: ['TEACHER'],
  permissions: ['grammar.moderate'],
};

describe('Admin Grammar Theory Editor Pure Helpers', () => {
  it('creates empty valid structured theory', () => {
    const theory = createEmptyTheory();
    expect(theory.objectives).toEqual([]);
    expect(theory.sections).toEqual([]);
    expect(theory.commonMistakes).toEqual([]);
    expect(theory.tips).toEqual([]);
    expect(theory.summary).toEqual([]);
    expect(hasAnyTheoryContent(theory)).toBe(false);
  });

  it('detects existing theory content accurately', () => {
    expect(hasAnyTheoryContent(createEmptyTheory())).toBe(false);
    expect(hasAnyTheoryContent({ ...createEmptyTheory(), objectives: ['Learn A1'] })).toBe(true);
    expect(
      hasAnyTheoryContent({
        ...createEmptyTheory(),
        sections: [{ heading: 'H1', explanationVi: 'E1', formulas: [], examples: [], notes: [] }],
      })
    ).toBe(true);
    expect(hasAnyTheoryContent({ ...createEmptyTheory(), tips: ['Tip 1'] })).toBe(true);
    expect(
      hasAnyTheoryContent({
        ...createEmptyTheory(),
        commonMistakes: [{ wrong: 'w', right: 'r', explanationVi: 'e' }],
      })
    ).toBe(true);
    expect(hasAnyTheoryContent({ ...createEmptyTheory(), summary: ['Summary item'] })).toBe(true);
  });

  it('adds, removes, and reorders objectives properly', () => {
    let theory = createEmptyTheory();
    theory = addObjectiveToTheory(theory, 'Obj A');
    theory = addObjectiveToTheory(theory, 'Obj B');
    theory = addObjectiveToTheory(theory, 'Obj C');
    expect(theory.objectives).toEqual(['Obj A', 'Obj B', 'Obj C']);

    theory = reorderObjectivesInTheory(theory, 2, 0);
    expect(theory.objectives).toEqual(['Obj C', 'Obj A', 'Obj B']);

    theory = removeObjectiveFromTheory(theory, 1);
    expect(theory.objectives).toEqual(['Obj C', 'Obj B']);
  });

  it('adds, updates, removes, and reorders sections properly', () => {
    let theory = createEmptyTheory();
    theory = addSectionToTheory(theory, { heading: 'Section 1', explanationVi: 'Expl 1' });
    theory = addSectionToTheory(theory, { heading: 'Section 2', explanationVi: 'Expl 2' });
    expect(theory.sections).toHaveLength(2);
    expect(theory.sections[0].heading).toBe('Section 1');

    theory = updateSectionInTheory(theory, 0, { heading: 'Section 1 Updated' });
    expect(theory.sections[0].heading).toBe('Section 1 Updated');

    theory = reorderSectionsInTheory(theory, 1, 0);
    expect(theory.sections[0].heading).toBe('Section 2');
    expect(theory.sections[1].heading).toBe('Section 1 Updated');

    theory = removeSectionFromTheory(theory, 0);
    expect(theory.sections).toHaveLength(1);
    expect(theory.sections[0].heading).toBe('Section 1 Updated');
  });

  it('adds and removes common mistakes and tips properly', () => {
    let theory = createEmptyTheory();
    theory = addMistakeToTheory(theory, { wrong: 'He go', right: 'He goes', explanationVi: 'Ngôi thứ 3' });
    theory = addTipToTheory(theory, 'Nhớ thêm -s hoặc -es');

    expect(theory.commonMistakes).toHaveLength(1);
    expect(theory.commonMistakes?.[0].wrong).toBe('He go');
    expect(theory.tips).toEqual(['Nhớ thêm -s hoặc -es']);

    theory = removeMistakeFromTheory(theory, 0);
    theory = removeTipFromTheory(theory, 0);
    expect(theory.commonMistakes).toHaveLength(0);
    expect(theory.tips).toHaveLength(0);
  });

  it('converts between structured and raw JSON modes without data loss', () => {
    const originalTheory: GrammarTheoryDto = {
      objectives: ['Master Present Simple'],
      sections: [
        {
          heading: 'Khẳng định',
          explanationVi: 'Chủ ngữ số ít đi với động từ thêm s/es',
          formulas: [{ label: 'Khẳng định', pattern: 'S + V(s/es) + O' }],
          examples: [{ en: 'He plays tennis.', vi: 'Anh ấy chơi quần vợt.', highlight: 'plays' }],
          notes: ['Lưu ý trường hợp bất quy tắc'],
        },
      ],
      commonMistakes: [{ wrong: 'She play', right: 'She plays', explanationVi: 'Thiếu s' }],
      tips: ['Học thuộc bảng quy tắc'],
      summary: ['Tóm tắt bài học'],
    };

    const toRaw = convertTheoryBetweenModes(originalTheory, 'raw', '');
    expect(toRaw.raw).toContain('Master Present Simple');
    expect(toRaw.raw).toContain('S + V(s/es) + O');

    const toStructured = convertTheoryBetweenModes(createEmptyTheory(), 'structured', toRaw.raw);
    expect(toStructured.errorKey).toBeUndefined();
    expect(toStructured.theory).toEqual(originalTheory);

    const invalidConversion = convertTheoryBetweenModes(
      originalTheory,
      'structured',
      '{ invalid_json: '
    );
    expect(invalidConversion.errorKey).toBe('admin.grammar.theoryErrors.invalidJson');
    expect(invalidConversion.theory).toEqual(originalTheory);
  });

  it('validateTheoryJson checks element types strictly to prevent preview crash', () => {
    // Non-string in objectives
    const invalidObj = validateTheoryJson(JSON.stringify({ objectives: [123] }));
    expect(invalidObj.ok).toBe(false);
    if (!invalidObj.ok) {
      expect(invalidObj.errorKey).toBe('admin.grammar.theoryErrors.invalidObjectives');
    }

    // Non-object in sections
    const invalidSec = validateTheoryJson(JSON.stringify({ sections: ['not an object'] }));
    expect(invalidSec.ok).toBe(false);
    if (!invalidSec.ok) {
      expect(invalidSec.errorKey).toBe('admin.grammar.theoryErrors.invalidSections');
    }

    // Valid JSON
    const valid = validateTheoryJson(JSON.stringify({ objectives: ['Valid objective'], sections: [{ heading: 'H', explanationVi: 'E' }] }));
    expect(valid.ok).toBe(true);
    if (valid.ok) {
      expect(valid.value.objectives).toEqual(['Valid objective']);
    }
  });

  it('groups BE validation errors by field prefix with groupIssuesByField', () => {
    const errors = {
      'content.statements[0]': ['Statement is required'],
      'grading.correctOptionIds': ['Missing correct option ID'],
      'maxPoints': ['Max points must be at least 1'],
      '$.sections[0].heading': ['Heading is missing'],
      'unexpectedNested.customField': ['Custom error message'],
    };

    const { byField, otherIssues } = groupIssuesByField(errors, [
      'content',
      'grading',
      'maxPoints',
      'sections',
    ]);

    expect(byField.content).toEqual(['Statement is required']);
    expect(byField.grading).toEqual(['Missing correct option ID']);
    expect(byField.maxPoints).toEqual(['Max points must be at least 1']);
    expect(byField.sections).toEqual(['Heading is missing']);
    expect(otherIssues).toHaveLength(1);
    expect(otherIssues[0].path).toBe('unexpectedNested.customField');
    expect(otherIssues[0].messages).toEqual(['Custom error message']);
  });

  it('determines moderation action buttons according to status and permissions', () => {
    // 1. Pending + Moderate permission -> Approve & Reject available, Promote not allowed
    const pendingModerate = getModerationActionButtons('Pending', {
      canModerate: true,
      canManage: false,
    });
    expect(pendingModerate.canApprove).toBe(true);
    expect(pendingModerate.canReject).toBe(true);
    expect(pendingModerate.canPromote).toBe(false);

    // 2. Approved + Manage permission -> Promote available, Approve/Reject not allowed
    const approvedManage = getModerationActionButtons('Approved', {
      canModerate: false,
      canManage: true,
    });
    expect(approvedManage.canApprove).toBe(false);
    expect(approvedManage.canReject).toBe(false);
    expect(approvedManage.canPromote).toBe(true);

    // 3. Pending + Manage ONLY (no moderate) -> CANNOT promote pending item!
    const pendingManageOnly = getModerationActionButtons('Pending', {
      canModerate: false,
      canManage: true,
    });
    expect(pendingManageOnly.canApprove).toBe(false);
    expect(pendingManageOnly.canReject).toBe(false);
    expect(pendingManageOnly.canPromote).toBe(false);

    // 4. Rejected item -> Read-only
    const rejectedItem = getModerationActionButtons('Rejected', {
      canModerate: true,
      canManage: true,
    });
    expect(rejectedItem.canApprove).toBe(false);
    expect(rejectedItem.canReject).toBe(false);
    expect(rejectedItem.canPromote).toBe(false);
    expect(rejectedItem.isReadOnly).toBe(true);
  });

  it('computes correct invalidation keys for admin grammar actions', () => {
    // Approve invalidates moderation queue + steps list
    expect(getInvalidationKeysFor('approve')).toEqual([
      ['admin', 'grammar', 'moderation'],
      ['admin', 'grammar', 'steps'],
    ]);

    // Reject invalidates moderation queue + steps list
    expect(getInvalidationKeysFor('reject')).toEqual([
      ['admin', 'grammar', 'moderation'],
      ['admin', 'grammar', 'steps'],
    ]);

    // Promote invalidates moderation queue + steps list + step detail
    expect(getInvalidationKeysFor('promote', 'A1-01')).toEqual([
      ['admin', 'grammar', 'moderation'],
      ['admin', 'grammar', 'steps'],
      ['admin', 'grammar', 'step', 'A1-01'],
    ]);

    // Update exercise invalidates step detail + steps list (for core counts)
    expect(getInvalidationKeysFor('updateExercise', 'B1-02')).toEqual([
      ['admin', 'grammar', 'step', 'B1-02'],
      ['admin', 'grammar', 'steps'],
    ]);
  });

  it('validates reject reason length strictly', () => {
    expect(validateRejectReason('').ok).toBe(false);
    expect(validateRejectReason('   \n  \t ').ok).toBe(false);
    expect(validateRejectReason('   \n  \t ').errorKey).toBe('admin.grammar.rejectReasonRequired');

    const str500 = 'a'.repeat(500);
    expect(validateRejectReason(str500).ok).toBe(true);

    const str501 = 'a'.repeat(501);
    expect(validateRejectReason(str501).ok).toBe(false);
    expect(validateRejectReason(str501).errorKey).toBe('admin.grammar.rejectReasonMaxLength');
  });

  it('maps 403 and 404 by status while preserving learner G4 error codes', () => {
    const mockT = ((k: string) => k) as any;

    // 403 status maps to forbidden
    expect(getGrammarErrorMessage({ response: { status: 403 } }, mockT)).toBe('common.forbidden');

    // 404 status in admin mode maps to stepNotFound
    expect(getGrammarErrorMessage({ response: { status: 404 } }, mockT, true)).toBe('admin.grammar.stepNotFoundTitle');

    // 404 status in learner mode maps to general error or learner fallback
    const learner404 = getGrammarErrorMessage({ response: { status: 404 } }, mockT, false);
    expect(learner404).toBe('grammar.errors.generic');

    // G4 Learner error detail codes preserved exactly
    expect(
      getGrammarErrorMessage(
        { response: { status: 409, data: { detail: "An exercise with key 'mc-01' already exists in step 'A1-01'." } } },
        mockT
      )
    ).toBe('admin.grammar.errors.duplicateKey');

    expect(
      getGrammarErrorMessage(
        { response: { status: 409, data: { detail: 'identical content hash already exists' } } },
        mockT
      )
    ).toBe('admin.grammar.errors.duplicateContentHash');
  });

  it('validateGenerateItems supports admin maxCount = 20 vs learner maxCount = 10', () => {
    const items = [
      { type: 'MultipleChoice', count: 10, difficulty: 1 },
      { type: 'FillInTheBlanks', count: 5, difficulty: 2 },
    ];
    // For learner: 15 > 10 => invalid
    const learnerVal = validateGenerateItems(items, 10);
    expect(learnerVal.valid).toBe(false);
    expect(learnerVal.errorKey).toBe('grammar.max10Exercises');

    // For admin: 15 <= 20 => valid
    const adminVal = validateGenerateItems(items, 20);
    expect(adminVal.valid).toBe(true);
    expect(adminVal.totalCount).toBe(15);
  });

});

describe('AdminGrammarModerationPage Permission Filtering', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    (useAuthStore as any).getInitialState = () => useAuthStore.getState();
    (useAuthStore as any).getServerSnapshot = () => useAuthStore.getState();
    queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    (adminGrammarApi.listModerationQueue as any).mockResolvedValue(mockQueueData);
    queryClient.setQueryData(
      ['admin', 'grammar', 'moderation', { status: 'Pending', page: 1, pageSize: 10, paged: true }],
      mockQueueData
    );
  });

  it('renders Approve and Reject buttons when user has grammar.moderate permission', () => {
    queryClient.setQueryData(['auth', 'me'], mockModeratorOnlyUser);
    useAuthStore.setState({
      isAuthenticated: true,
      isInitialized: true,
      currentUser: mockModeratorOnlyUser,
      accessToken: 'token',
    });

    const html = renderToStaticMarkup(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <AdminGrammarModerationPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(html).toContain('Phê duyệt');
    expect(html).toContain('Từ chối');
    expect(html).not.toContain('Nâng thành Official');
  });

  it('renders both Approve/Reject when user has Admin permissions on Pending queue', () => {
    queryClient.setQueryData(['auth', 'me'], mockAdminUser);
    useAuthStore.setState({
      isAuthenticated: true,
      isInitialized: true,
      currentUser: mockAdminUser,
      accessToken: 'token',
    });

    const html = renderToStaticMarkup(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <AdminGrammarModerationPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    // In Pending queue: Approve & Reject are shown
    expect(html).toContain('Phê duyệt');
    expect(html).toContain('Từ chối');
    // Cannot promote Pending items
    expect(html).not.toContain('Nâng thành Official');
  });
});

describe('Real AppRoutes resolution for Admin Grammar routes', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    (useAuthStore as any).getInitialState = () => useAuthStore.getState();
    (useAuthStore as any).getServerSnapshot = () => useAuthStore.getState();
    queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    queryClient.setQueryData(['auth', 'me'], mockAdminUser);
    useAuthStore.setState({
      isAuthenticated: true,
      isInitialized: true,
      currentUser: mockAdminUser,
      accessToken: 'token',
    });

    queryClient.setQueryData(
      ['admin', 'grammar', 'steps', { level: 'A1', page: 1, pageSize: 20, paged: true }],
      { items: [mockStepData], totalCount: 1, page: 1, pageSize: 20, totalPages: 1 }
    );
    queryClient.setQueryData(['admin', 'grammar', 'step', 'A1-01'], mockStepData);
    queryClient.setQueryData(
      ['admin', 'grammar', 'moderation', { status: 'Pending', page: 1, pageSize: 10, paged: true }],
      mockQueueData
    );
  });

  const renderRoutes = async (initialPath: string) => {
    await import('@/features/grammar');
    try {
      renderToStaticMarkup(
        <QueryClientProvider client={queryClient}>
          <MemoryRouter initialEntries={[initialPath]}>
            <AppRoutes />
          </MemoryRouter>
        </QueryClientProvider>
      );
    } catch {
      // ignore suspense
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

  it('/admin/grammar resolves and renders AdminGrammarStepsPage without redirect', async () => {
    const html = await renderRoutes('/admin/grammar');

    expect(html).toContain('Quản lý Lộ trình Ngữ pháp');
    expect(html).toContain('Nhập nội dung chuẩn');
  });

  it('/admin/grammar/moderation resolves and renders AdminGrammarModerationPage', async () => {
    const html = await renderRoutes('/admin/grammar/moderation');

    expect(html).toContain('Kiểm duyệt Bài tập Ngữ pháp');
  });

  it('/admin/grammar/:code resolves and renders AdminGrammarStepEditorPage with wrapper', async () => {
    const html = await renderRoutes('/admin/grammar/A1-01');

    expect(html).toContain('A1-01');
    expect(html).toContain('Thì hiện tại đơn');
  });
});
