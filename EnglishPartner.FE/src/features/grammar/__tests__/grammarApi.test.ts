import { describe, it, expect, vi, beforeEach } from 'vitest';
import { grammarApi, GRAMMAR_AI_TIMEOUT_MS } from '../api/grammarApi';
import { httpClient } from '@/shared/api/httpClient';
import { buildGrammarSubmitPayload } from '../utils/grammarHelpers';

vi.mock('@/shared/api/httpClient', () => ({
  httpClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe('grammarApi Client Contracts', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('getRoadmap calls GET /api/grammar/roadmap', async () => {
    const mockRoadmap = { levels: [] };
    (httpClient.get as any).mockResolvedValueOnce({ data: mockRoadmap });

    const result = await grammarApi.getRoadmap();

    expect(httpClient.get).toHaveBeenCalledWith('/api/grammar/roadmap');
    expect(result).toEqual(mockRoadmap);
  });

  it('getStep calls GET /api/grammar/steps/{code}', async () => {
    const mockStep = { code: 'A1-01', titleVi: 'Thì hiện tại đơn' };
    (httpClient.get as any).mockResolvedValueOnce({ data: mockStep });

    const result = await grammarApi.getStep('A1-01');

    expect(httpClient.get).toHaveBeenCalledWith('/api/grammar/steps/A1-01');
    expect(result).toEqual(mockStep);
  });

  it('listStepExercises parses pagination headers when array returned', async () => {
    const mockExercises = [
      { id: 'ex-1', type: 'MultipleChoice', title: 'Q1' },
      { id: 'ex-2', type: 'FillInTheBlanks', title: 'Q2' },
    ];
    (httpClient.get as any).mockResolvedValueOnce({
      data: mockExercises,
      headers: {
        'x-total-count': '25',
        'x-page': '2',
        'x-page-size': '10',
        'x-total-pages': '3',
      },
    });

    const result = await grammarApi.listStepExercises('A1-01', {
      source: 'community',
      page: 2,
      pageSize: 10,
    });

    expect(httpClient.get).toHaveBeenCalledWith('/api/grammar/steps/A1-01/exercises', {
      params: { source: 'community', page: 2, pageSize: 10 },
    });
    expect(result).toEqual({
      items: mockExercises,
      totalCount: 25,
      page: 2,
      pageSize: 10,
      totalPages: 3,
    });
  });

  it('startAttempt sends StartGrammarAttemptRequest payload to POST /api/grammar/attempts', async () => {
    const payload = { kind: 'StepCore' as const, stepCode: 'A1-02' };
    const mockAttempt = { id: 'att-1', kind: 'StepCore', stepCode: 'A1-02' };
    (httpClient.post as any).mockResolvedValueOnce({ data: mockAttempt });

    const result = await grammarApi.startAttempt(payload);

    expect(httpClient.post).toHaveBeenCalledWith('/api/grammar/attempts', payload);
    expect(result).toEqual(mockAttempt);
  });

  it('startAttempt forwards forceNew so restarting creates a fresh attempt', async () => {
    const payload = { kind: 'StepCore' as const, stepCode: 'A1-02', forceNew: true };
    (httpClient.post as any).mockResolvedValueOnce({ data: { id: 'att-2' } });

    await grammarApi.startAttempt(payload);

    expect(httpClient.post).toHaveBeenCalledWith('/api/grammar/attempts', expect.objectContaining({ forceNew: true }));
  });

  it('submitAnswer sends SubmitGrammarAnswerRequest formatted by buildGrammarSubmitPayload with aiConfig', async () => {
    const payload = buildGrammarSubmitPayload(
      'ex-trans-1',
      { translatedText: 'She walks to school' },
      'gemini',
      'key-123',
      'gemini-2.5-flash'
    );
    const mockResult = { attemptId: 'att-1', exerciseId: 'ex-trans-1', status: 'Completed', score100: 95 };
    (httpClient.post as any).mockResolvedValueOnce({ data: mockResult });

    const result = await grammarApi.submitAnswer('att-1', payload);

    expect(httpClient.post).toHaveBeenCalledWith('/api/grammar/attempts/att-1/answers', {
      exerciseId: 'ex-trans-1',
      answerPayload: { translatedText: 'She walks to school' },
      aiConfig: {
        provider: 'gemini',
        assessmentModelCode: 'gemini-2.5-flash',
        apiKey: 'key-123',
      },
    }, { timeout: GRAMMAR_AI_TIMEOUT_MS });
    expect(result).toEqual(mockResult);
  });

  it('submitAnswer sends SubmitGrammarAnswerRequest without aiConfig when key is empty', async () => {
    const payload = buildGrammarSubmitPayload(
      'ex-mc-1',
      { selectedOptionIds: ['opt-a'] },
      'gemini',
      '   '
    );
    const mockResult = { attemptId: 'att-1', exerciseId: 'ex-mc-1', status: 'Completed', score100: 100 };
    (httpClient.post as any).mockResolvedValueOnce({ data: mockResult });

    const result = await grammarApi.submitAnswer('att-1', payload);

    expect(httpClient.post).toHaveBeenCalledWith('/api/grammar/attempts/att-1/answers', {
      exerciseId: 'ex-mc-1',
      answerPayload: { selectedOptionIds: ['opt-a'] },
      aiConfig: undefined,
    }, { timeout: GRAMMAR_AI_TIMEOUT_MS });
    expect(result).toEqual(mockResult);
  });

  it('submitAnswer sends SubmitGrammarAnswerRequest without aiConfig when null', async () => {
    const payload = {
      exerciseId: 'ex-mc-1',
      answerPayload: { selectedOptionIds: ['opt-a'] },
      aiConfig: null,
    };
    const mockResult = { attemptId: 'att-1', exerciseId: 'ex-mc-1', status: 'Completed', score100: 100 };
    (httpClient.post as any).mockResolvedValueOnce({ data: mockResult });

    const result = await grammarApi.submitAnswer('att-1', payload);

    expect(httpClient.post).toHaveBeenCalledWith('/api/grammar/attempts/att-1/answers', payload, { timeout: GRAMMAR_AI_TIMEOUT_MS });
    expect(result).toEqual(mockResult);
  });

  it('completeAttempt calls POST /api/grammar/attempts/{id}/complete', async () => {
    const mockResult = { score100: 85, passed: true, unlockedStepCodes: ['A1-03'] };
    (httpClient.post as any).mockResolvedValueOnce({ data: mockResult });

    const result = await grammarApi.completeAttempt('att-1');

    expect(httpClient.post).toHaveBeenCalledWith('/api/grammar/attempts/att-1/complete');
    expect(result).toEqual(mockResult);
  });

  it('generateExercises calls POST /api/grammar/steps/{code}/exercises/generate', async () => {
    const payload = {
      items: [{ type: 'MultipleChoice', count: 2, difficulty: 2 }],
      learnerNote: 'Travel topic',
      visibility: 'Private' as const,
      aiConfig: { provider: 'gemini', modelCode: 'gemini-2.5-flash', apiKey: 'test' },
    };
    const mockResponse = { created: [], rejected: [] };
    (httpClient.post as any).mockResolvedValueOnce({ data: mockResponse });

    const result = await grammarApi.generateExercises('A1-01', payload);

    expect(httpClient.post).toHaveBeenCalledWith(
      '/api/grammar/steps/A1-01/exercises/generate',
      payload,
      { timeout: GRAMMAR_AI_TIMEOUT_MS }
    );
    expect(result).toEqual(mockResponse);
  });

  it('buildPrompt calls POST /api/grammar/steps/{code}/exercises/prompt', async () => {
    const payload = {
      items: [{ type: 'FillInTheBlanks', count: 1, difficulty: 1 }],
      learnerNote: null,
    };
    const mockResponse = { prompt: 'System prompt content' };
    (httpClient.post as any).mockResolvedValueOnce({ data: mockResponse });

    const result = await grammarApi.buildPrompt('A1-01', payload);

    expect(httpClient.post).toHaveBeenCalledWith(
      '/api/grammar/steps/A1-01/exercises/prompt',
      payload
    );
    expect(result).toEqual(mockResponse);
  });

  it('importExercises calls POST /api/grammar/steps/{code}/exercises/import', async () => {
    const payload = {
      jsonText: '[{"type": "TrueFalse"}]',
      visibility: 'Public' as const,
    };
    const mockResponse = { created: [], rejected: [] };
    (httpClient.post as any).mockResolvedValueOnce({ data: mockResponse });

    const result = await grammarApi.importExercises('A1-01', payload);

    expect(httpClient.post).toHaveBeenCalledWith(
      '/api/grammar/steps/A1-01/exercises/import',
      payload
    );
    expect(result).toEqual(mockResponse);
  });

  it('changeMyExerciseVisibility calls POST /api/grammar/my-exercises/{id}/visibility', async () => {
    const payload = { visibility: 'Public' as const };
    const mockResponse = { id: 'ex-1', visibility: 'Public' };
    (httpClient.post as any).mockResolvedValueOnce({ data: mockResponse });

    const result = await grammarApi.changeMyExerciseVisibility('ex-1', payload);

    expect(httpClient.post).toHaveBeenCalledWith(
      '/api/grammar/my-exercises/ex-1/visibility',
      payload
    );
    expect(result).toEqual(mockResponse);
  });

  it('deleteMyExercise calls DELETE /api/grammar/my-exercises/{id}', async () => {
    (httpClient.delete as any).mockResolvedValueOnce({ data: undefined });

    await grammarApi.deleteMyExercise('ex-1');

    expect(httpClient.delete).toHaveBeenCalledWith('/api/grammar/my-exercises/ex-1');
  });

  it('safely encodes URL path parameters with encodeURIComponent', async () => {
    (httpClient.get as any).mockResolvedValue({ data: {} });
    (httpClient.delete as any).mockResolvedValue({ data: undefined });

    await grammarApi.getStep('A1/01?special=true');
    expect(httpClient.get).toHaveBeenCalledWith('/api/grammar/steps/A1%2F01%3Fspecial%3Dtrue');

    await grammarApi.getAttempt('att 123/sub');
    expect(httpClient.get).toHaveBeenCalledWith('/api/grammar/attempts/att%20123%2Fsub');

    await grammarApi.deleteMyExercise('ex#1');
    expect(httpClient.delete).toHaveBeenCalledWith('/api/grammar/my-exercises/ex%231');
  });
});
