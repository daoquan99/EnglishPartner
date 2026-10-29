import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { vocabularyApi } from '../api/vocabularyApi';
import type {
  BuildVocabularyGenerationPromptRequestDto,
  GenerateVocabularyRequestDto,
  ImportVocabularyJsonRequestDto,
  SubmitVocabularyReviewRequestDto,
  UpdateLearningSettingsRequestDto,
} from '../types/vocabulary.types';

export const VOCAB_QUERY_KEYS = {
  settings: () => ['vocabulary', 'settings'] as const,
  todayPlan: () => ['vocabulary', 'today-plan'] as const,
  pronunciationHistory: (params: {
    vocabularyId?: string;
    senseId?: string;
    targetType?: string;
    targetId?: string;
  }) => ['vocabulary', 'pronunciation-history', params] as const,
  learningHistory: (params: {
    vocabularyId?: string;
    senseId?: string;
  }) => ['vocabulary', 'learning-history', params] as const,
};

export function useLearningSettingsQuery() {
  return useQuery({
    queryKey: VOCAB_QUERY_KEYS.settings(),
    queryFn: () => vocabularyApi.getLearningSettings(),
    staleTime: 60_000,
  });
}

export function useUpdateLearningSettingsMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpdateLearningSettingsRequestDto) =>
      vocabularyApi.updateLearningSettings(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: VOCAB_QUERY_KEYS.settings() });
      void queryClient.invalidateQueries({ queryKey: VOCAB_QUERY_KEYS.todayPlan() });
    },
  });
}

export function useDailyVocabularyPlanQuery(timeZoneId?: string) {
  return useQuery({
    queryKey: VOCAB_QUERY_KEYS.todayPlan(),
    queryFn: () => vocabularyApi.getTodayPlan(timeZoneId),
    staleTime: 60_000,
    retry: (failureCount, error: any) => {
      if (error?.response?.status === 404 || error?.status === 404) {
        return false;
      }
      return failureCount < 2;
    },
  });
}

export function useInitializeDailyPlanMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => vocabularyApi.initializeTodayPlan(),
    onSuccess: (data) => {
      queryClient.setQueryData(VOCAB_QUERY_KEYS.todayPlan(), data);
      void queryClient.invalidateQueries({ queryKey: VOCAB_QUERY_KEYS.settings() });
    },
  });
}

export function useSubmitVocabularyReviewMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ payload, dailyItemId }: { payload: SubmitVocabularyReviewRequestDto; dailyItemId?: string }) =>
      vocabularyApi.submitReview(payload, dailyItemId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: VOCAB_QUERY_KEYS.todayPlan() });
      void queryClient.invalidateQueries({ queryKey: ['vocabulary', 'learning-history'] });
    },
  });
}

export function useGenerateVocabularyMutation(options?: { skipErrorToast?: boolean }) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: GenerateVocabularyRequestDto) =>
      vocabularyApi.generateVocabularyWithAi(payload, options),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: VOCAB_QUERY_KEYS.todayPlan() });
      void queryClient.invalidateQueries({ queryKey: ['vocabulary'] });
      void queryClient.invalidateQueries({ queryKey: ['admin', 'vocabularies'] });
    },
  });
}

export function useAssessPronunciationMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: vocabularyApi.assessPronunciation,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['vocabulary', 'today-plan'] });
      void queryClient.invalidateQueries({ queryKey: ['vocabulary', 'pronunciation-history'] });
    },
  });
}

export function useSynthesizePronunciationMutation() {
  return useMutation({
    mutationFn: vocabularyApi.synthesizePronunciationAudio,
  });
}

export function useBuildVocabularyGenerationPromptMutation(options?: { skipErrorToast?: boolean }) {
  return useMutation({
    mutationFn: (payload: BuildVocabularyGenerationPromptRequestDto) =>
      vocabularyApi.buildGenerationPrompt(payload, options),
  });
}

export function useImportVocabularyJsonMutation(options?: { skipErrorToast?: boolean }) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: ImportVocabularyJsonRequestDto) =>
      vocabularyApi.importVocabularyJson(payload, options),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: VOCAB_QUERY_KEYS.todayPlan() });
      void queryClient.invalidateQueries({ queryKey: ['vocabulary'] });
      void queryClient.invalidateQueries({ queryKey: ['admin', 'vocabularies'] });
    },
  });
}
