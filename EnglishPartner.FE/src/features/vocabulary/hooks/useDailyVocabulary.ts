import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { vocabularyApi } from '../api/vocabularyApi';
import type {
  BuildVocabularyGenerationPromptRequestDto,
  GenerateVocabularyRequestDto,
  ImportVocabularyJsonRequestDto,
  SubmitVocabularyReviewRequestDto,
} from '../types/vocabulary.types';

export const VOCAB_QUERY_KEYS = {
  todayPlan: (target = 10) => ['vocabulary', 'today-plan', target] as const,
  pronunciationHistory: (params: {
    userId?: string;
    vocabularyId?: string;
    senseId?: string;
    targetType?: string;
    targetId?: string;
  }) => ['vocabulary', 'pronunciation-history', params] as const,
  learningHistory: (params: {
    userId?: string;
    vocabularyId?: string;
    senseId?: string;
  }) => ['vocabulary', 'learning-history', params] as const,
};

export function useDailyVocabularyPlanQuery(target = 10) {
  return useQuery({
    queryKey: VOCAB_QUERY_KEYS.todayPlan(target),
    queryFn: () => vocabularyApi.getTodayPlan(target),
    staleTime: 60_000,
  });
}

export function useSubmitVocabularyReviewMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: SubmitVocabularyReviewRequestDto) =>
      vocabularyApi.submitReview(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['vocabulary', 'today-plan'] });
      void queryClient.invalidateQueries({ queryKey: ['vocabulary', 'learning-history'] });
    },
  });
}

export function useGenerateVocabularyMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: GenerateVocabularyRequestDto) =>
      vocabularyApi.generateVocabularyWithAi(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['vocabulary', 'today-plan'] });
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

export function useBuildVocabularyGenerationPromptMutation() {
  return useMutation({
    mutationFn: (payload: BuildVocabularyGenerationPromptRequestDto) =>
      vocabularyApi.buildGenerationPrompt(payload),
  });
}

export function useImportVocabularyJsonMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: ImportVocabularyJsonRequestDto) =>
      vocabularyApi.importVocabularyJson(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['vocabulary', 'today-plan'] });
    },
  });
}
