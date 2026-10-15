import { useQuery } from '@tanstack/react-query';
import { catalogApi } from '../api/catalogApi';

export const CATALOG_QUERY_KEYS = {
  modes: (category?: string) => ['catalog', 'modes', category ?? 'ALL'] as const,
  cefrLevels: ['catalog', 'cefr-levels'] as const,
  voices: (onlyEnabled = true, onlyFeatured = false) =>
    ['catalog', 'voices', { onlyEnabled, onlyFeatured }] as const,
  speechSpeeds: ['catalog', 'speech-speeds'] as const,
  topics: (filters?: {
    onlyActive?: boolean;
    supportsConversation?: boolean;
    supportsVocabulary?: boolean;
    modeCode?: string;
    category?: string;
  }) => ['catalog', 'topics', filters ?? {}] as const,
  vocabModels: ['catalog', 'models', 'vocabulary-generation'] as const,
  pronunciationModels: ['catalog', 'models', 'pronunciation-audio'] as const,
  activeRubric: ['catalog', 'rubrics', 'active'] as const,
};

export function useConversationModesQuery(category?: string) {
  return useQuery({
    queryKey: CATALOG_QUERY_KEYS.modes(category),
    queryFn: () => catalogApi.getConversationModes(category),
    staleTime: 5 * 60_000,
  });
}

export function useCefrLevelPromptsQuery() {
  return useQuery({
    queryKey: CATALOG_QUERY_KEYS.cefrLevels,
    queryFn: () => catalogApi.getCefrLevelPrompts(),
    staleTime: 5 * 60_000,
  });
}

export function useTeacherVoicesQuery(onlyEnabled = true, onlyFeatured = false) {
  return useQuery({
    queryKey: CATALOG_QUERY_KEYS.voices(onlyEnabled, onlyFeatured),
    queryFn: () => catalogApi.getTeacherVoices(onlyEnabled, onlyFeatured),
    staleTime: 5 * 60_000,
  });
}

export function useSpeechSpeedsQuery() {
  return useQuery({
    queryKey: CATALOG_QUERY_KEYS.speechSpeeds,
    queryFn: () => catalogApi.getSpeechSpeeds(),
    staleTime: 10 * 60_000,
  });
}

export function useTopicsQuery(filters?: {
  onlyActive?: boolean;
  supportsConversation?: boolean;
  supportsVocabulary?: boolean;
  modeCode?: string;
  category?: string;
}) {
  return useQuery({
    queryKey: CATALOG_QUERY_KEYS.topics(filters),
    queryFn: () => catalogApi.getTopics(filters),
    staleTime: 2 * 60_000,
  });
}

export function useVocabularyGenerationModelsQuery() {
  return useQuery({
    queryKey: CATALOG_QUERY_KEYS.vocabModels,
    queryFn: () => catalogApi.getVocabularyGenerationModels(),
    staleTime: 5 * 60_000,
  });
}

export function usePronunciationModelsQuery() {
  return useQuery({
    queryKey: CATALOG_QUERY_KEYS.pronunciationModels,
    queryFn: () => catalogApi.getPronunciationModels(),
    staleTime: 5 * 60_000,
  });
}

export function useActiveScoringRubricQuery() {
  return useQuery({
    queryKey: CATALOG_QUERY_KEYS.activeRubric,
    queryFn: () => catalogApi.getActiveScoringRubric(),
    staleTime: 5 * 60_000,
  });
}
