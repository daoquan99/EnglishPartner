import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { vocabularyApi } from '../api/vocabularyApi';
import type {
  GenerateVocabularyRequestDto,
  SubmitVocabularyReviewRequestDto,
} from '../types/vocabulary.types';

export const VOCAB_QUERY_KEYS = {
  todayPlan: (target = 10) => ['vocabulary', 'today-plan', target] as const,
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
    },
  });
}

export function useSynthesizePronunciationMutation() {
  return useMutation({
    mutationFn: vocabularyApi.synthesizePronunciationAudio,
  });
}
