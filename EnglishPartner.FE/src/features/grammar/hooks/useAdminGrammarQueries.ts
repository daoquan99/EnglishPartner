import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { adminGrammarApi } from '../api/adminGrammarApi';
import { grammarKeys } from './useGrammarQueries';
import type {
  UpdateGrammarStepRequestDto,
  UpdateGrammarStepTheoryRequestDto,
  CreateOfficialGrammarExerciseRequestDto,
  UpdateOfficialGrammarExerciseRequestDto,
  ImportGrammarContentPackRequestDto,
  RejectGrammarExerciseRequestDto,
  PromoteGrammarExerciseRequestDto,
  GenerateOfficialGrammarExercisesRequestDto,
  ImportOfficialGrammarExercisesJsonRequestDto,
  BuildGrammarExercisePromptRequestDto,
  DraftGrammarTheoryWithAiRequestDto,
  ListAdminGrammarStepsParams,
  ListAdminGrammarModerationParams,
} from '../types/grammar.types';

export const grammarAdminKeys = {
  all: ['admin', 'grammar'] as const,
  steps: (params?: ListAdminGrammarStepsParams) =>
    ['admin', 'grammar', 'steps', params ?? {}] as const,
  step: (code: string) => ['admin', 'grammar', 'step', code] as const,
  moderation: (params?: ListAdminGrammarModerationParams) =>
    ['admin', 'grammar', 'moderation', params ?? {}] as const,
};

export function useAdminGrammarStepsQuery(params?: ListAdminGrammarStepsParams, enabled = true) {
  return useQuery({
    queryKey: grammarAdminKeys.steps(params),
    queryFn: () => adminGrammarApi.listSteps(params),
    enabled,
  });
}

export function useAdminGrammarStepQuery(code: string, enabled = true) {
  return useQuery({
    queryKey: grammarAdminKeys.step(code),
    queryFn: () => adminGrammarApi.getStep(code),
    enabled: Boolean(code) && enabled,
  });
}

export function useAdminGrammarModerationQuery(params?: ListAdminGrammarModerationParams, enabled = true) {
  return useQuery({
    queryKey: grammarAdminKeys.moderation(params),
    queryFn: () => adminGrammarApi.listModerationQueue(params),
    enabled,
  });
}

export function useUpdateAdminGrammarStepMutation(code: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: UpdateGrammarStepRequestDto) =>
      adminGrammarApi.updateStep(code, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: grammarAdminKeys.step(code) });
      queryClient.invalidateQueries({ queryKey: ['admin', 'grammar', 'steps'] });
      queryClient.invalidateQueries({ queryKey: grammarKeys.roadmap() });
      queryClient.invalidateQueries({ queryKey: grammarKeys.step(code) });
    },
  });
}

export function useUpdateAdminGrammarTheoryMutation(code: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: UpdateGrammarStepTheoryRequestDto) =>
      adminGrammarApi.updateTheory(code, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: grammarAdminKeys.step(code) });
      queryClient.invalidateQueries({ queryKey: ['admin', 'grammar', 'steps'] });
      queryClient.invalidateQueries({ queryKey: grammarKeys.step(code) });
    },
  });
}

export function useDraftAdminGrammarTheoryMutation(code: string) {
  return useMutation({
    mutationFn: (payload: DraftGrammarTheoryWithAiRequestDto) =>
      adminGrammarApi.draftTheoryWithAi(code, payload),
  });
}

export function useCreateOfficialGrammarExerciseMutation(code: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: CreateOfficialGrammarExerciseRequestDto) =>
      adminGrammarApi.createOfficialExercise(code, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: grammarAdminKeys.step(code) });
      queryClient.invalidateQueries({ queryKey: ['admin', 'grammar', 'steps'] });
      queryClient.invalidateQueries({ queryKey: ['grammar', 'exercises', code] });
      queryClient.invalidateQueries({ queryKey: grammarKeys.step(code) });
    },
  });
}

export function useUpdateOfficialGrammarExerciseMutation(code: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateOfficialGrammarExerciseRequestDto }) =>
      adminGrammarApi.updateOfficialExercise(id, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: grammarAdminKeys.step(code) });
      queryClient.invalidateQueries({ queryKey: ['admin', 'grammar', 'steps'] });
      queryClient.invalidateQueries({ queryKey: ['grammar', 'exercises', code] });
      queryClient.invalidateQueries({ queryKey: grammarKeys.step(code) });
    },
  });
}

export function useGenerateOfficialGrammarExercisesMutation(code: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: GenerateOfficialGrammarExercisesRequestDto) =>
      adminGrammarApi.generateOfficialExercises(code, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: grammarAdminKeys.step(code) });
      queryClient.invalidateQueries({ queryKey: ['admin', 'grammar', 'steps'] });
      queryClient.invalidateQueries({ queryKey: ['grammar', 'exercises', code] });
      queryClient.invalidateQueries({ queryKey: grammarKeys.step(code) });
    },
  });
}

export function useImportOfficialGrammarExercisesMutation(code: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: ImportOfficialGrammarExercisesJsonRequestDto) =>
      adminGrammarApi.importOfficialExercises(code, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: grammarAdminKeys.step(code) });
      queryClient.invalidateQueries({ queryKey: ['admin', 'grammar', 'steps'] });
      queryClient.invalidateQueries({ queryKey: ['grammar', 'exercises', code] });
      queryClient.invalidateQueries({ queryKey: grammarKeys.step(code) });
    },
  });
}

export function useBuildOfficialGrammarPromptMutation(code: string) {
  return useMutation({
    mutationFn: (payload: BuildGrammarExercisePromptRequestDto) =>
      adminGrammarApi.buildOfficialPrompt(code, payload),
  });
}

export function useDeleteOfficialGrammarExerciseMutation(code: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => adminGrammarApi.deleteOfficialExercise(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: grammarAdminKeys.step(code) });
      queryClient.invalidateQueries({ queryKey: ['admin', 'grammar', 'steps'] });
      queryClient.invalidateQueries({ queryKey: ['grammar', 'exercises', code] });
      queryClient.invalidateQueries({ queryKey: grammarKeys.step(code) });
    },
  });
}

export function useImportAdminContentPackMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload?: ImportGrammarContentPackRequestDto) =>
      adminGrammarApi.importContentPack(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: grammarAdminKeys.all });
      queryClient.invalidateQueries({ queryKey: grammarKeys.all });
    },
  });
}

export function useApproveGrammarExerciseMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => adminGrammarApi.approveExercise(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'grammar', 'moderation'] });
      queryClient.invalidateQueries({ queryKey: ['admin', 'grammar', 'steps'] });
      queryClient.invalidateQueries({ queryKey: grammarKeys.all });
    },
  });
}

export function useRejectGrammarExerciseMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: RejectGrammarExerciseRequestDto }) =>
      adminGrammarApi.rejectExercise(id, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'grammar', 'moderation'] });
      queryClient.invalidateQueries({ queryKey: ['admin', 'grammar', 'steps'] });
      queryClient.invalidateQueries({ queryKey: grammarKeys.all });
    },
  });
}

export function usePromoteGrammarExerciseMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      id,
      payload,
      stepCode: _stepCode,
    }: {
      id: string;
      payload: PromoteGrammarExerciseRequestDto;
      stepCode?: string;
    }) => adminGrammarApi.promoteExercise(id, payload),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'grammar', 'moderation'] });
      queryClient.invalidateQueries({ queryKey: ['admin', 'grammar', 'steps'] });
      if (variables.stepCode) {
        queryClient.invalidateQueries({ queryKey: grammarAdminKeys.step(variables.stepCode) });
      }
      queryClient.invalidateQueries({ queryKey: grammarKeys.all });
    },
  });
}
