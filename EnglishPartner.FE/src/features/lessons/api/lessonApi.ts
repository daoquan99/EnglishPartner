import { httpClient } from '@/shared/api/httpClient';
import type {
  LessonSummaryDto,
  LessonDetailAuthorDto,
  LessonDetailLearnerDto,
  CreateLessonDraftRequest,
  UpdateLessonDraftRequest,
  AddExerciseRequest,
  UpdateExerciseRequest,
  PublishedLessonResultDto,
  DraftRevisionResultDto,
  LessonAttemptResumeDto,
  ExerciseAnswerResultDto,
  LessonAttemptSummaryDto,
  LessonRubricSummaryDto,
  LessonRubricDetailDto,
  CreateLessonRubricRequest,
  LessonCandidateValidationResultDto,
  CommitLessonImportRequest,
  ExerciseTypeMetadata,
} from '../types/lessons.types';

export interface ListLessonsParams {
  search?: string;
  cefrLevel?: string;
  topic?: string;
  status?: string;
  isLearner?: boolean;
  page?: number;
  pageNumber?: number;
  pageSize?: number;
}

export interface ListLessonRubricsParams {
  exerciseType?: string;
  cefrLevel?: string;
  isActive?: boolean;
}

export const lessonApi = {
  // Lesson Authoring APIs
  listLessons: async (params?: ListLessonsParams): Promise<LessonSummaryDto[]> => {
    const { data } = await httpClient.get<LessonSummaryDto[]>('/api/lessons', { params });
    return data;
  },

  getLessonAuthorDetail: async (id: string): Promise<LessonDetailAuthorDto> => {
    const { data } = await httpClient.get<LessonDetailAuthorDto>(`/api/lessons/${id}`, {
      params: { isLearner: false },
    });
    return data;
  },

  getLessonLearnerDetail: async (id: string): Promise<LessonDetailLearnerDto> => {
    const { data } = await httpClient.get<LessonDetailLearnerDto>(`/api/lessons/${id}`, {
      params: { isLearner: true },
    });
    return data;
  },

  createLessonDraft: async (payload: CreateLessonDraftRequest): Promise<{ id: string; lessonId: string }> => {
    const { data } = await httpClient.post<any>('/api/lessons', payload);
    const lessonId = data?.lessonId || data?.id || '';
    return {
      ...data,
      id: lessonId,
      lessonId,
    };
  },

  updateLessonDraft: async (
    id: string,
    payload: UpdateLessonDraftRequest
  ): Promise<{ id: string; lessonId: string; rowVersion: string }> => {
    const { data } = await httpClient.put<any>(`/api/lessons/${id}`, payload);
    const lessonId = data?.lessonId || data?.id || id;
    const rowVersion = data?.rowVersion || '';
    return {
      ...data,
      id: lessonId,
      lessonId,
      rowVersion,
    };
  },

  deleteLessonDraft: async (id: string): Promise<void> => {
    await httpClient.delete<void>(`/api/lessons/${id}`);
  },

  buildLessonPrompt: async (payload: {
    topic: string;
    cefrLevel: string;
    lessonTitle?: string;
    preferredExerciseTypes?: string[];
    targetExerciseCount?: number;
    customInstructions?: string;
  }): Promise<{ promptText?: string; systemPrompt: string; userPrompt: string; schemaVersion: string }> => {
    const { data } = await httpClient.post<{
      promptText?: string;
      systemPrompt: string;
      userPrompt: string;
      schemaVersion: string;
    }>('/api/lessons/generation/prompt', payload);
    return data;
  },

  generateLessonExercises: async (payload: {
    apiKey?: string;
    modelCode?: string;
    targetLessonId?: string;
    topic: string;
    cefrLevel: string;
    lessonTitle?: string;
    preferredExerciseTypes?: string[];
    targetExerciseCount?: number;
    customInstructions?: string;
    commitMode?: 'PreviewOnly' | 'Append' | 'Replace';
  }): Promise<LessonCandidateValidationResultDto> => {
    const { data } = await httpClient.post<LessonCandidateValidationResultDto>(
      '/api/lessons/generation/generate',
      payload
    );
    return data;
  },

  publishLesson: async (
    id: string,
    rowVersion?: string | null
  ): Promise<PublishedLessonResultDto> => {
    const { data } = await httpClient.post<PublishedLessonResultDto>(`/api/lessons/${id}/publish`, {
      rowVersion,
    });
    return data;
  },

  revertLessonToDraft: async (id: string): Promise<LessonDetailAuthorDto> => {
    const { data } = await httpClient.post<LessonDetailAuthorDto>(`/api/lessons/${id}/revert-to-draft`, {});
    return data;
  },

  createLessonRevision: async (id: string): Promise<DraftRevisionResultDto> => {
    const { data } = await httpClient.post<DraftRevisionResultDto>(`/api/lessons/${id}/revision`, {});
    return data;
  },

  addExercise: async (
    lessonId: string,
    payload: AddExerciseRequest
  ): Promise<{ exerciseId: string }> => {
    const { data } = await httpClient.post<{ exerciseId: string }>(`/api/lessons/${lessonId}/exercises`, payload);
    return data;
  },

  updateExercise: async (
    lessonId: string,
    exerciseId: string,
    payload: UpdateExerciseRequest
  ): Promise<{ exerciseId: string }> => {
    const { data } = await httpClient.put<{ exerciseId: string }>(
      `/api/lessons/${lessonId}/exercises/${exerciseId}`,
      payload
    );
    return data;
  },

  deleteExercise: async (lessonId: string, exerciseId: string): Promise<void> => {
    await httpClient.delete<void>(`/api/lessons/${lessonId}/exercises/${exerciseId}`);
  },

  reorderExercises: async (
    lessonId: string,
    orderedExerciseIds: string[]
  ): Promise<void> => {
    await httpClient.put<void>(`/api/lessons/${lessonId}/exercises/reorder`, {
      orderedExerciseIds,
    });
  },

  validateCandidate: async (
    candidate: Record<string, unknown>
  ): Promise<LessonCandidateValidationResultDto> => {
    const { data } = await httpClient.post<LessonCandidateValidationResultDto>(
      '/api/lessons/validate-candidate',
      candidate
    );
    return data;
  },

  commitImport: async (
    payload: CommitLessonImportRequest,
    operationKey?: string
  ): Promise<{ lessonId: string; exerciseCount: number; version: number }> => {
    const headers = operationKey ? { 'X-Operation-Key': operationKey } : undefined;
    const { data } = await httpClient.post<any>(
      '/api/lessons/commit-import',
      payload,
      { headers }
    );
    return {
      lessonId: data.id || data.lessonId,
      exerciseCount: Array.isArray(data.exercises) ? data.exercises.length : (data.exerciseCount ?? 0),
      version: data.version ?? 1,
    };
  },

  getExerciseTypeCatalog: async (): Promise<ExerciseTypeMetadata[]> => {
    const { data } = await httpClient.get<ExerciseTypeMetadata[]>('/api/lessons/exercise-types');
    return data;
  },

  // Learner Attempt & Player APIs
  synthesizeExerciseAudio: async (
    attemptId: string,
    exerciseId: string,
    payload: { apiKey?: string; voiceName?: string; speechSpeed?: number; cardId?: string },
    signal?: AbortSignal
  ): Promise<{ audioBase64: string; mimeType: string; sampleRate: number; durationMs: number }> => {
    const { data } = await httpClient.post(
      `/api/lesson-attempts/${attemptId}/exercises/${exerciseId}/synthesize`, payload, { signal, timeout: 180_000 }
    );
    return data;
  },

  startAttempt: async (lessonId: string): Promise<{ attemptId: string }> => {
    const { data } = await httpClient.post<{ attemptId: string }>(`/api/lessons/${lessonId}/attempts`, {});
    return data;
  },

  resumeAttempt: async (attemptId: string): Promise<LessonAttemptResumeDto> => {
    const { data } = await httpClient.get<LessonAttemptResumeDto>(`/api/lesson-attempts/${attemptId}/resume`);
    return data;
  },

  submitExerciseAnswer: async (
    attemptId: string,
    payload: {
      exerciseId: string;
      subQuestionId?: string | null;
      attemptRevision: number;
      answerPayload: Record<string, unknown>;
    }
  ): Promise<ExerciseAnswerResultDto> => {
    const { data } = await httpClient.post<ExerciseAnswerResultDto>(
      `/api/lesson-attempts/${attemptId}/answers`,
      payload
    );
    return data;
  },

  retryGrading: async (
    attemptId: string,
    answerId: string
  ): Promise<ExerciseAnswerResultDto> => {
    const { data } = await httpClient.post<ExerciseAnswerResultDto>(
      `/api/lesson-attempts/${attemptId}/answers/${answerId}/retry`,
      {}
    );
    return data;
  },

  listAttempts: async (params?: { userId?: string }): Promise<LessonAttemptSummaryDto[]> => {
    const { data } = await httpClient.get<LessonAttemptSummaryDto[]>('/api/lesson-attempts', { params });
    return data;
  },

  // Rubrics Management APIs
  listRubrics: async (params?: ListLessonRubricsParams): Promise<LessonRubricSummaryDto[]> => {
    const { data } = await httpClient.get<LessonRubricSummaryDto[]>('/api/lesson-rubrics', { params });
    return data;
  },

  getRubricDetail: async (id: string): Promise<LessonRubricDetailDto> => {
    const { data } = await httpClient.get<LessonRubricDetailDto>(`/api/lesson-rubrics/${id}`);
    return data;
  },

  createRubric: async (payload: CreateLessonRubricRequest): Promise<{ rubricId: string }> => {
    const { data } = await httpClient.post<{ rubricId: string }>('/api/lesson-rubrics', payload);
    return data;
  },

  deactivateRubric: async (id: string): Promise<void> => {
    await httpClient.post<void>(`/api/lesson-rubrics/${id}/deactivate`, {});
  },
};
