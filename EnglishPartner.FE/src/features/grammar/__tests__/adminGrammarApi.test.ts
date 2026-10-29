import { describe, it, expect, vi, beforeEach } from 'vitest';
import { adminGrammarApi } from '../api/adminGrammarApi';
import { GRAMMAR_AI_TIMEOUT_MS } from '../api/grammarApi';
import { httpClient } from '@/shared/api/httpClient';

vi.mock('@/shared/api/httpClient', () => ({
  httpClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe('adminGrammarApi Client Contracts', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('listSteps calls GET /api/admin/grammar/steps with params and parses pagination headers', async () => {
    const mockSteps = [
      { id: 'step-1', code: 'A1-01', titleVi: 'Thì hiện tại đơn' },
      { id: 'step-2', code: 'A1-02', titleVi: 'Thì hiện tại tiếp diễn' },
    ];
    (httpClient.get as any).mockResolvedValueOnce({
      data: mockSteps,
      headers: {
        'x-total-count': '20',
        'x-page': '1',
        'x-page-size': '20',
        'x-total-pages': '1',
      },
    });

    const result = await adminGrammarApi.listSteps({ level: 'A1', page: 1, pageSize: 20 });

    expect(httpClient.get).toHaveBeenCalledWith('/api/admin/grammar/steps', {
      params: { level: 'A1', page: 1, pageSize: 20 },
    });
    expect(result).toEqual({
      items: mockSteps,
      totalCount: 20,
      page: 1,
      pageSize: 20,
      totalPages: 1,
    });
  });

  it('getStep calls GET /api/admin/grammar/steps/{code}', async () => {
    const mockDetail = { id: 'step-1', code: 'A1-01', titleVi: 'Hiện tại đơn' };
    (httpClient.get as any).mockResolvedValueOnce({ data: mockDetail });

    const result = await adminGrammarApi.getStep('A1-01');

    expect(httpClient.get).toHaveBeenCalledWith('/api/admin/grammar/steps/A1-01');
    expect(result).toEqual(mockDetail);
  });

  it('updateStep calls PUT /api/admin/grammar/steps/{code} with metadata payload', async () => {
    const payload = {
      titleVi: 'Hiện tại đơn',
      titleEn: 'Present Simple',
      summaryVi: 'Cách dùng hiện tại đơn',
      isPublished: true,
    };
    const mockResult = { id: 'step-1', code: 'A1-01', ...payload };
    (httpClient.put as any).mockResolvedValueOnce({ data: mockResult });

    const result = await adminGrammarApi.updateStep('A1-01', payload);

    expect(httpClient.put).toHaveBeenCalledWith('/api/admin/grammar/steps/A1-01', payload);
    expect(result).toEqual(mockResult);
  });

  it('updateTheory calls PUT /api/admin/grammar/steps/{code}/theory with theory payload', async () => {
    const payload = {
      theory: {
        objectives: ['Obj 1'],
        sections: [{ heading: 'Sec 1', explanationVi: 'Expl 1' }],
      },
    };
    (httpClient.put as any).mockResolvedValueOnce({ data: undefined });

    await adminGrammarApi.updateTheory('A1-01', payload);

    expect(httpClient.put).toHaveBeenCalledWith('/api/admin/grammar/steps/A1-01/theory', payload);
  });

  it('draftTheoryWithAi calls POST /api/admin/grammar/steps/{code}/theory/ai-draft', async () => {
    const payload = {
      aiConfig: { provider: 'gemini', modelCode: 'gemini-2.5-flash', apiKey: 'key-test' },
    };
    const mockResponse = {
      theory: { sections: [{ heading: 'AI Section', explanationVi: 'AI Expl' }] },
      issues: [],
    };
    (httpClient.post as any).mockResolvedValueOnce({ data: mockResponse });

    const result = await adminGrammarApi.draftTheoryWithAi('A1-01', payload);

    expect(httpClient.post).toHaveBeenCalledWith(
      '/api/admin/grammar/steps/A1-01/theory/ai-draft',
      payload,
      { timeout: GRAMMAR_AI_TIMEOUT_MS }
    );
    expect(result).toEqual(mockResponse);
  });

  it('createOfficialExercise calls POST /api/admin/grammar/steps/{code}/exercises', async () => {
    const payload = {
      key: 'mc-01',
      type: 'MultipleChoice',
      difficulty: 1,
      title: 'Q1',
      instructions: 'Choose correct answer',
      maxPoints: 10,
      gradingMode: 'Deterministic',
      content: { question: 'What is your name?' },
      grading: { correctOptionIds: ['opt-1'] },
      isCore: true,
    };
    const mockCreated = { id: 'ex-1', ...payload };
    (httpClient.post as any).mockResolvedValueOnce({ data: mockCreated });

    const result = await adminGrammarApi.createOfficialExercise('A1-01', payload);

    expect(httpClient.post).toHaveBeenCalledWith(
      '/api/admin/grammar/steps/A1-01/exercises',
      payload
    );
    expect(result).toEqual(mockCreated);
  });

  it('updateOfficialExercise calls PUT /api/admin/grammar/exercises/{id}', async () => {
    const payload = {
      type: 'MultipleChoice',
      difficulty: 2,
      title: 'Q1 Updated',
      instructions: 'Updated instruction',
      maxPoints: 15,
      gradingMode: 'Deterministic',
      content: { question: 'Where do you live?' },
      grading: { correctOptionIds: ['opt-2'] },
      isCore: false,
    };
    const mockUpdated = { id: 'ex-123', ...payload };
    (httpClient.put as any).mockResolvedValueOnce({ data: mockUpdated });

    const result = await adminGrammarApi.updateOfficialExercise('ex-123', payload);

    expect(httpClient.put).toHaveBeenCalledWith('/api/admin/grammar/exercises/ex-123', payload);
    expect(result).toEqual(mockUpdated);
  });

  it('deleteOfficialExercise calls DELETE /api/admin/grammar/exercises/{id}', async () => {
    (httpClient.delete as any).mockResolvedValueOnce({ data: undefined });

    await adminGrammarApi.deleteOfficialExercise('ex-123');

    expect(httpClient.delete).toHaveBeenCalledWith('/api/admin/grammar/exercises/ex-123');
  });

  it('generateOfficialExercises calls POST /api/admin/grammar/steps/{code}/exercises/generate', async () => {
    const payload = {
      items: [{ type: 'MultipleChoice', count: 2, difficulty: 1 }],
      isCore: true,
      aiConfig: { provider: 'agy', modelCode: 'gemini-3.8-flash-high' },
    };
    const mockResponse = { created: [{ id: 'ex-1' }], rejected: [] };
    (httpClient.post as any).mockResolvedValueOnce({ data: mockResponse });

    const result = await adminGrammarApi.generateOfficialExercises('A1-01', payload);

    expect(httpClient.post).toHaveBeenCalledWith(
      '/api/admin/grammar/steps/A1-01/exercises/generate',
      payload,
      { timeout: GRAMMAR_AI_TIMEOUT_MS }
    );
    expect(result).toEqual(mockResponse);
  });

  it('buildOfficialPrompt calls POST /api/admin/grammar/steps/{code}/exercises/prompt', async () => {
    const payload = {
      items: [{ type: 'FillInTheBlanks', count: 1, difficulty: 2 }],
      learnerNote: 'Travel topic',
    };
    const mockPrompt = { prompt: 'System prompt content...' };
    (httpClient.post as any).mockResolvedValueOnce({ data: mockPrompt });

    const result = await adminGrammarApi.buildOfficialPrompt('A1-01', payload);

    expect(httpClient.post).toHaveBeenCalledWith(
      '/api/admin/grammar/steps/A1-01/exercises/prompt',
      payload
    );
    expect(result).toEqual(mockPrompt);
  });

  it('importOfficialExercises calls POST /api/admin/grammar/steps/{code}/exercises/import', async () => {
    const payload = {
      jsonText: '[{"type": "MultipleChoice"}]',
      isCore: true,
      expectedItems: [{ type: 'MultipleChoice', count: 1, difficulty: 1 }],
    };
    const mockImportResponse = { created: [{ id: 'ex-imp-1' }], rejected: [] };
    (httpClient.post as any).mockResolvedValueOnce({ data: mockImportResponse });

    const result = await adminGrammarApi.importOfficialExercises('A1-01', payload);

    expect(httpClient.post).toHaveBeenCalledWith(
      '/api/admin/grammar/steps/A1-01/exercises/import',
      payload
    );
    expect(result).toEqual(mockImportResponse);
  });

  it('importContentPack calls POST /api/admin/grammar/content-pack/import', async () => {
    const mockPackResult = {
      stepsCreated: 20,
      stepsUpdated: 0,
      stepsSkipped: 0,
      exercisesCreated: 60,
      exercisesSkipped: 0,
      errors: [],
    };
    (httpClient.post as any).mockResolvedValueOnce({ data: mockPackResult });

    const result = await adminGrammarApi.importContentPack({ overwriteExistingTheory: false });

    expect(httpClient.post).toHaveBeenCalledWith('/api/admin/grammar/content-pack/import', {
      overwriteExistingTheory: false,
    });
    expect(result).toEqual(mockPackResult);
  });

  it('listModerationQueue calls GET /api/admin/grammar/moderation with pagination headers', async () => {
    const mockItems = [
      { id: 'mod-1', stepCode: 'A1-01', title: 'Community Exercise 1' },
    ];
    (httpClient.get as any).mockResolvedValueOnce({
      data: mockItems,
      headers: {
        'x-total-count': '1',
        'x-page': '1',
        'x-page-size': '10',
        'x-total-pages': '1',
      },
    });

    const result = await adminGrammarApi.listModerationQueue({ status: 'Pending', page: 1, pageSize: 10 });

    expect(httpClient.get).toHaveBeenCalledWith('/api/admin/grammar/moderation', {
      params: { status: 'Pending', page: 1, pageSize: 10 },
    });
    expect(result).toEqual({
      items: mockItems,
      totalCount: 1,
      page: 1,
      pageSize: 10,
      totalPages: 1,
    });
  });

  it('approveExercise calls POST /api/admin/grammar/exercises/{id}/approve', async () => {
    (httpClient.post as any).mockResolvedValueOnce({ data: undefined });

    await adminGrammarApi.approveExercise('mod-123');

    expect(httpClient.post).toHaveBeenCalledWith('/api/admin/grammar/exercises/mod-123/approve');
  });

  it('rejectExercise calls POST /api/admin/grammar/exercises/{id}/reject with reason', async () => {
    const payload = { reason: 'Incorrect grammar explanation' };
    (httpClient.post as any).mockResolvedValueOnce({ data: undefined });

    await adminGrammarApi.rejectExercise('mod-123', payload);

    expect(httpClient.post).toHaveBeenCalledWith(
      '/api/admin/grammar/exercises/mod-123/reject',
      payload
    );
  });

  it('promoteExercise calls POST /api/admin/grammar/exercises/{id}/promote with isCore', async () => {
    const payload = { isCore: true };
    const mockPromoted = { id: 'mod-123', isCore: true, source: 'Official' };
    (httpClient.post as any).mockResolvedValueOnce({ data: mockPromoted });

    const result = await adminGrammarApi.promoteExercise('mod-123', payload);

    expect(httpClient.post).toHaveBeenCalledWith(
      '/api/admin/grammar/exercises/mod-123/promote',
      payload
    );
    expect(result).toEqual(mockPromoted);
  });
});
