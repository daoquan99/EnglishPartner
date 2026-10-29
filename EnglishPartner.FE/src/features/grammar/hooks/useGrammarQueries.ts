import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { grammarApi } from '../api/grammarApi';
import type {
  StartGrammarAttemptRequestDto,
  SubmitGrammarAnswerRequestDto,
  GenerateGrammarExercisesRequestDto,
  BuildGrammarExercisePromptRequestDto,
  ImportGrammarExercisesJsonRequestDto,
  UpdateMyGrammarExerciseRequestDto,
  ChangeMyGrammarExerciseVisibilityRequestDto,
  ListGrammarExercisesParams,
  ListMyGrammarExercisesParams,
} from '../types/grammar.types';

export const grammarKeys = {
  all: ['grammar'] as const,
  roadmap: () => ['grammar', 'roadmap'] as const,
  step: (code: string) => ['grammar', 'step', code] as const,
  exercises: (code: string, source?: string, page?: number) =>
    ['grammar', 'exercises', code, source ?? 'all', page ?? 1] as const,
  attempt: (id: string) => ['grammar', 'attempt', id] as const,
  myExercises: (params?: ListMyGrammarExercisesParams) =>
    ['grammar', 'my-exercises', params ?? {}] as const,
};

export function useGrammarRoadmapQuery() {
  return useQuery({
    queryKey: grammarKeys.roadmap(),
    queryFn: () => grammarApi.getRoadmap(),
    staleTime: 60 * 1000,
  });
}

export function useGrammarStepQuery(code: string, enabled = true) {
  return useQuery({
    queryKey: grammarKeys.step(code),
    queryFn: () => grammarApi.getStep(code),
    enabled: Boolean(code) && enabled,
  });
}

export function useGrammarStepExercisesQuery(
  code: string,
  params?: ListGrammarExercisesParams,
  enabled = true
) {
  return useQuery({
    queryKey: grammarKeys.exercises(code, params?.source, params?.page),
    queryFn: () => grammarApi.listStepExercises(code, params),
    enabled: Boolean(code) && enabled,
  });
}

export function useGrammarAttemptQuery(id: string, enabled = true) {
  return useQuery({
    queryKey: grammarKeys.attempt(id),
    queryFn: () => grammarApi.getAttempt(id),
    enabled: Boolean(id) && enabled,
    refetchOnWindowFocus: false,
  });
}

export function useMyGrammarExercisesQuery(params?: ListMyGrammarExercisesParams) {
  return useQuery({
    queryKey: grammarKeys.myExercises(params),
    queryFn: () => grammarApi.listMyExercises(params),
  });
}

export function useStartGrammarAttemptMutation() {
  return useMutation({
    mutationFn: (payload: StartGrammarAttemptRequestDto) => grammarApi.startAttempt(payload),
  });
}

export function useSubmitGrammarAnswerMutation(attemptId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: SubmitGrammarAnswerRequestDto) =>
      grammarApi.submitAnswer(attemptId, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: grammarKeys.attempt(attemptId) });
    },
  });
}

export function useCompleteGrammarAttemptMutation(attemptId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => grammarApi.completeAttempt(attemptId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: grammarKeys.roadmap() });
      queryClient.invalidateQueries({ queryKey: ['grammar', 'step'] });
      queryClient.invalidateQueries({ queryKey: grammarKeys.attempt(attemptId) });
    },
  });
}

export function useGenerateGrammarExercisesMutation(stepCode: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: GenerateGrammarExercisesRequestDto) =>
      grammarApi.generateExercises(stepCode, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['grammar', 'exercises'] });
      queryClient.invalidateQueries({ queryKey: ['grammar', 'my-exercises'] });
      queryClient.invalidateQueries({ queryKey: grammarKeys.step(stepCode) });
    },
  });
}

export function useBuildGrammarExercisePromptMutation(stepCode: string) {
  return useMutation({
    mutationFn: (payload: BuildGrammarExercisePromptRequestDto) =>
      grammarApi.buildPrompt(stepCode, payload),
  });
}

export function useImportGrammarExercisesMutation(stepCode: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: ImportGrammarExercisesJsonRequestDto) =>
      grammarApi.importExercises(stepCode, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['grammar', 'exercises'] });
      queryClient.invalidateQueries({ queryKey: ['grammar', 'my-exercises'] });
      queryClient.invalidateQueries({ queryKey: grammarKeys.step(stepCode) });
    },
  });
}

export function useUpdateMyGrammarExerciseMutation(stepCode?: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateMyGrammarExerciseRequestDto }) =>
      grammarApi.updateMyExercise(id, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['grammar', 'exercises'] });
      queryClient.invalidateQueries({ queryKey: ['grammar', 'my-exercises'] });
      if (stepCode) {
        queryClient.invalidateQueries({ queryKey: grammarKeys.step(stepCode) });
      }
    },
  });
}

export function useChangeMyGrammarExerciseVisibilityMutation(stepCode?: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      id,
      payload,
    }: {
      id: string;
      payload: ChangeMyGrammarExerciseVisibilityRequestDto;
    }) => grammarApi.changeMyExerciseVisibility(id, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['grammar', 'exercises'] });
      queryClient.invalidateQueries({ queryKey: ['grammar', 'my-exercises'] });
      if (stepCode) {
        queryClient.invalidateQueries({ queryKey: grammarKeys.step(stepCode) });
      }
    },
  });
}

export function useDeleteMyGrammarExerciseMutation(stepCode?: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => grammarApi.deleteMyExercise(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['grammar', 'exercises'] });
      queryClient.invalidateQueries({ queryKey: ['grammar', 'my-exercises'] });
      if (stepCode) {
        queryClient.invalidateQueries({ queryKey: grammarKeys.step(stepCode) });
      }
    },
  });
}
