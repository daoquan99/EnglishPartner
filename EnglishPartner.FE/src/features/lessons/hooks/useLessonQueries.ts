import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  lessonApi,
  type ListLessonsParams,
  type ListLessonRubricsParams,
} from '../api/lessonApi';
import type {
  CreateLessonDraftRequest,
  UpdateLessonDraftRequest,
  AddExerciseRequest,
  UpdateExerciseRequest,
  CommitLessonImportRequest,
  CreateLessonRubricRequest,
} from '../types/lessons.types';

export const LESSON_QUERY_KEYS = {
  all: ['lessons'] as const,
  catalog: () => [...LESSON_QUERY_KEYS.all, 'catalog'] as const,
  adminList: (params?: ListLessonsParams) => [...LESSON_QUERY_KEYS.all, 'admin-list', params] as const,
  adminDetail: (id: string) => [...LESSON_QUERY_KEYS.all, 'admin-detail', id] as const,
  learnerList: (params?: ListLessonsParams) => [...LESSON_QUERY_KEYS.all, 'learner-list', params] as const,
  learnerDetail: (id: string) => [...LESSON_QUERY_KEYS.all, 'learner-detail', id] as const,
  attempt: (attemptId: string) => [...LESSON_QUERY_KEYS.all, 'attempt', attemptId] as const,
  attemptsList: (userId?: string) => [...LESSON_QUERY_KEYS.all, 'attempts-list', userId] as const,
  rubrics: (params?: ListLessonRubricsParams) => [...LESSON_QUERY_KEYS.all, 'rubrics', params] as const,
  rubricDetail: (id: string) => [...LESSON_QUERY_KEYS.all, 'rubric-detail', id] as const,
};

export function useLessonCatalogQuery() {
  return useQuery({
    queryKey: LESSON_QUERY_KEYS.catalog(),
    queryFn: () => lessonApi.getExerciseTypeCatalog(),
    staleTime: 1000 * 60 * 30, // 30 minutes
  });
}

export function useAdminLessonsQuery(params?: ListLessonsParams) {
  return useQuery({
    queryKey: LESSON_QUERY_KEYS.adminList(params),
    queryFn: () => lessonApi.listLessons({ ...params, isLearner: false }),
  });
}

export function useAdminLessonDetailQuery(id?: string) {
  return useQuery({
    queryKey: LESSON_QUERY_KEYS.adminDetail(id ?? ''),
    queryFn: () => lessonApi.getLessonAuthorDetail(id!),
    enabled: Boolean(id),
  });
}

export function useLearnerLessonsQuery(params?: ListLessonsParams) {
  return useQuery({
    queryKey: LESSON_QUERY_KEYS.learnerList(params),
    queryFn: () => lessonApi.listLessons({ ...params, status: 'Published', isLearner: true }),
  });
}

export function useLearnerLessonDetailQuery(id?: string) {
  return useQuery({
    queryKey: LESSON_QUERY_KEYS.learnerDetail(id ?? ''),
    queryFn: () => lessonApi.getLessonLearnerDetail(id!),
    enabled: Boolean(id),
  });
}

export function useLessonAttemptQuery(attemptId?: string) {
  return useQuery({
    queryKey: LESSON_QUERY_KEYS.attempt(attemptId ?? ''),
    queryFn: () => lessonApi.resumeAttempt(attemptId!),
    enabled: Boolean(attemptId),
  });
}

export function useLearnerAttemptsQuery(userId?: string) {
  return useQuery({
    queryKey: LESSON_QUERY_KEYS.attemptsList(userId),
    queryFn: () => lessonApi.listAttempts({ userId }),
  });
}

export function useLessonRubricsQuery(params?: ListLessonRubricsParams) {
  return useQuery({
    queryKey: LESSON_QUERY_KEYS.rubrics(params),
    queryFn: () => lessonApi.listRubrics(params),
  });
}

export function useLessonRubricDetailQuery(id?: string) {
  return useQuery({
    queryKey: LESSON_QUERY_KEYS.rubricDetail(id ?? ''),
    queryFn: () => lessonApi.getRubricDetail(id!),
    enabled: Boolean(id),
  });
}

// ==========================================
// Mutations
// ==========================================

export function useCreateLessonDraftMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateLessonDraftRequest) => lessonApi.createLessonDraft(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: LESSON_QUERY_KEYS.all });
    },
  });
}

export function useUpdateLessonDraftMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateLessonDraftRequest }) =>
      lessonApi.updateLessonDraft(id, data),
    onSuccess: (_, vars) => {
      void queryClient.invalidateQueries({ queryKey: LESSON_QUERY_KEYS.adminDetail(vars.id) });
      void queryClient.invalidateQueries({ queryKey: LESSON_QUERY_KEYS.all });
    },
  });
}

export function usePublishLessonMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, rowVersion }: { id: string; rowVersion?: string | null }) =>
      lessonApi.publishLesson(id, rowVersion),
    onSuccess: (_, vars) => {
      void queryClient.invalidateQueries({ queryKey: LESSON_QUERY_KEYS.adminDetail(vars.id) });
      void queryClient.invalidateQueries({ queryKey: LESSON_QUERY_KEYS.all });
    },
  });
}

export function useRevertLessonToDraftMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => lessonApi.revertLessonToDraft(id),
    onSuccess: (_, id) => {
      void queryClient.invalidateQueries({ queryKey: LESSON_QUERY_KEYS.adminDetail(id) });
      void queryClient.invalidateQueries({ queryKey: LESSON_QUERY_KEYS.all });
    },
  });
}

export function useDeleteLessonDraftMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => lessonApi.deleteLessonDraft(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: LESSON_QUERY_KEYS.all });
    },
  });
}

export function useBuildLessonPromptMutation() {
  return useMutation({
    mutationFn: (payload: {
      topic: string;
      cefrLevel: string;
      lessonTitle?: string;
      preferredExerciseTypes?: string[];
      targetExerciseCount?: number;
      customInstructions?: string;
    }) => lessonApi.buildLessonPrompt(payload),
  });
}

export function useCreateLessonRevisionMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => lessonApi.createLessonRevision(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: LESSON_QUERY_KEYS.all });
    },
  });
}

export function useAddExerciseMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ lessonId, data }: { lessonId: string; data: AddExerciseRequest }) =>
      lessonApi.addExercise(lessonId, data),
    onSuccess: (_, vars) => {
      void queryClient.invalidateQueries({ queryKey: LESSON_QUERY_KEYS.adminDetail(vars.lessonId) });
    },
  });
}

export function useUpdateExerciseMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      lessonId,
      exerciseId,
      data,
    }: {
      lessonId: string;
      exerciseId: string;
      data: UpdateExerciseRequest;
    }) => lessonApi.updateExercise(lessonId, exerciseId, data),
    onSuccess: (_, vars) => {
      void queryClient.invalidateQueries({ queryKey: LESSON_QUERY_KEYS.adminDetail(vars.lessonId) });
    },
  });
}

export function useDeleteExerciseMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ lessonId, exerciseId }: { lessonId: string; exerciseId: string }) =>
      lessonApi.deleteExercise(lessonId, exerciseId),
    onSuccess: (_, vars) => {
      void queryClient.invalidateQueries({ queryKey: LESSON_QUERY_KEYS.adminDetail(vars.lessonId) });
    },
  });
}

export function useReorderExercisesMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      lessonId,
      orderedExerciseIds,
    }: {
      lessonId: string;
      orderedExerciseIds: string[];
    }) => lessonApi.reorderExercises(lessonId, orderedExerciseIds),
    onSuccess: (_, vars) => {
      void queryClient.invalidateQueries({ queryKey: LESSON_QUERY_KEYS.adminDetail(vars.lessonId) });
    },
  });
}

export function useValidateCandidateMutation() {
  return useMutation({
    mutationFn: (candidate: Record<string, unknown>) => lessonApi.validateCandidate(candidate),
  });
}

export function useCommitLessonImportMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      data,
      operationKey,
    }: {
      data: CommitLessonImportRequest;
      operationKey?: string;
    }) => lessonApi.commitImport(data, operationKey),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: LESSON_QUERY_KEYS.all });
    },
  });
}

export function useStartAttemptMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (lessonId: string) => lessonApi.startAttempt(lessonId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: LESSON_QUERY_KEYS.all });
    },
  });
}

export function useSubmitExerciseAnswerMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      attemptId,
      data,
    }: {
      attemptId: string;
      data: {
        exerciseId: string;
        subQuestionId?: string | null;
        attemptRevision: number;
        answerPayload: Record<string, unknown>;
        aiConfig?: import('../types/lessons.types').LessonAiExecutionConfigDto | null;
      };
    }) => lessonApi.submitExerciseAnswer(attemptId, data),
    onSuccess: (_, vars) => {
      void queryClient.invalidateQueries({ queryKey: LESSON_QUERY_KEYS.attempt(vars.attemptId) });
    },
  });
}

export function useRetryGradingMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      attemptId,
      answerId,
      payload,
    }: {
      attemptId: string;
      answerId: string;
      payload?: {
        attemptRevision: number;
        aiConfig?: import('../types/lessons.types').LessonAiExecutionConfigDto | null;
      };
    }) => lessonApi.retryGrading(attemptId, answerId, payload),
    onSuccess: (_, vars) => {
      void queryClient.invalidateQueries({ queryKey: LESSON_QUERY_KEYS.attempt(vars.attemptId) });
    },
  });
}

export function useRequestDictationAiReviewMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      attemptId,
      answerId,
      payload,
      operationKey,
    }: {
      attemptId: string;
      answerId: string;
      payload?: import('../types/lessons.types').RequestDictationAiReviewRequest;
      operationKey?: string;
    }) => lessonApi.requestDictationAiReview(attemptId, answerId, payload, operationKey),
    onSuccess: (_, vars) => {
      void queryClient.invalidateQueries({ queryKey: LESSON_QUERY_KEYS.attempt(vars.attemptId) });
    },
  });
}

export function useCreateLessonRubricMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateLessonRubricRequest) => lessonApi.createRubric(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: LESSON_QUERY_KEYS.rubrics() });
    },
  });
}

export function useDeactivateLessonRubricMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => lessonApi.deactivateRubric(id),
    onSuccess: (_, id) => {
      void queryClient.invalidateQueries({ queryKey: LESSON_QUERY_KEYS.rubricDetail(id) });
      void queryClient.invalidateQueries({ queryKey: LESSON_QUERY_KEYS.rubrics() });
    },
  });
}

export function useGenerateLessonExercisesMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: {
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
    }) => lessonApi.generateLessonExercises(payload),
    onSuccess: (_, variables) => {
      if (variables.targetLessonId && variables.commitMode && variables.commitMode !== 'PreviewOnly') {
        void queryClient.invalidateQueries({
          queryKey: LESSON_QUERY_KEYS.adminDetail(variables.targetLessonId),
        });
        void queryClient.invalidateQueries({
          queryKey: LESSON_QUERY_KEYS.all,
        });
      }
    },
  });
}

