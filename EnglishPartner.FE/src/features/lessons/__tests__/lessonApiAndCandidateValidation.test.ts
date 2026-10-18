import { describe, it, expect, vi, beforeEach } from 'vitest';
import { lessonApi } from '../api/lessonApi';
import { httpClient } from '@/shared/api/httpClient';

vi.mock('@/shared/api/httpClient', () => ({
  httpClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe('Lesson API Client & Candidate Operations', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('listLessons calls GET /api/lessons with query filters', async () => {
    const mockLessons = [
      {
        id: 'l-1',
        title: 'Dining Out',
        cefrLevel: 'B1',
        topic: 'Restaurant',
        status: 'Published',
        version: 1,
        exerciseCount: 5,
        totalMaxPoints: 50,
      },
    ];

    (httpClient.get as any).mockResolvedValueOnce({ data: mockLessons });

    const params = { cefrLevel: 'B1', status: 'Published', pageNumber: 1, pageSize: 10 };
    const res = await lessonApi.listLessons(params);

    expect(httpClient.get).toHaveBeenCalledWith('/api/lessons', { params });
    expect(res).toEqual(mockLessons);
  });

  it('validateCandidate sends candidate payload to /api/lessons/validate-candidate', async () => {
    const candidate = {
      lesson: {
        title: 'Test Lesson',
        cefrLevel: 'B2',
        exercises: [],
      },
    };

    const mockValidationResult = {
      isValid: false,
      issues: [
        {
          path: '$.lesson.exercises',
          severity: 'Error',
          message: 'Lesson must contain at least one exercise before publishing.',
        },
      ],
      candidate,
    };

    (httpClient.post as any).mockResolvedValueOnce({ data: mockValidationResult });

    const res = await lessonApi.validateCandidate(candidate);

    expect(httpClient.post).toHaveBeenCalledWith('/api/lessons/validate-candidate', candidate);
    expect(res.isValid).toBe(false);
    expect(res.issues).toHaveLength(1);
    expect(res.issues[0].path).toBe('$.lesson.exercises');
  });

  it('commitImport sends X-Operation-Key header for idempotent mutation', async () => {
    const commitReq = {
      targetLessonId: 'lesson-123',
      targetRevision: 1,
      commitMode: 'Append' as const,
      candidate: {
        exercises: [{ key: 'ex1', type: 'Flashcards' }],
      },
    };

    const mockCommitResult = {
      lessonId: 'lesson-123',
      exerciseCount: 1,
      version: 2,
    };

    (httpClient.post as any).mockResolvedValueOnce({ data: mockCommitResult });

    const opKey = 'op-uuid-key-456';
    const res = await lessonApi.commitImport(commitReq, opKey);

    expect(httpClient.post).toHaveBeenCalledWith(
      '/api/lessons/commit-import',
      commitReq,
      { headers: { 'X-Operation-Key': opKey } }
    );
    expect(res).toEqual(mockCommitResult);
  });

  it('submitExerciseAnswer passes attemptRevision and returns updated progress', async () => {
    const submitReq = {
      exerciseId: 'ex-mc-1',
      subQuestionId: null,
      attemptRevision: 3,
      answerPayload: { selectedOptionIds: ['opt_2'] },
    };

    const mockResult = {
      answerId: 'ans-1',
      attemptId: 'att-1',
      attemptRevision: 4,
      exerciseId: 'ex-mc-1',
      status: 'Completed',
      gradingMode: 'Deterministic',
      score100: 100,
      pointsEarned: 10,
      maxPoints: 10,
      attemptProgress: {
        completedExercisesCount: 1,
        totalExercisesCount: 1,
        currentSubtotalPoints: 10,
        totalMaxPoints: 10,
        finalScore100: 100,
      },
    };

    (httpClient.post as any).mockResolvedValueOnce({ data: mockResult });

    const res = await lessonApi.submitExerciseAnswer('att-1', submitReq);

    expect(httpClient.post).toHaveBeenCalledWith(
      '/api/lesson-attempts/att-1/answers',
      submitReq
    );
    expect(res.attemptRevision).toBe(4);
    expect(res.score100).toBe(100);
    expect(res.status).toBe('Completed');
  });

  it('retryGrading calls retry endpoint on failed answer', async () => {
    const mockRetried = {
      answerId: 'ans-failed',
      status: 'Completed',
      score100: 85,
      pointsEarned: 8.5,
      maxPoints: 10,
    };

    (httpClient.post as any).mockResolvedValueOnce({ data: mockRetried });

    const res = await lessonApi.retryGrading('att-1', 'ans-failed');

    expect(httpClient.post).toHaveBeenCalledWith(
      '/api/lesson-attempts/att-1/answers/ans-failed/retry',
      {}
    );
    expect(res.status).toBe('Completed');
  });
});
