import { httpClient } from '@/shared/api/httpClient';
import type {
  AiModelOptionDto,
  CefrLevelPromptDto,
  ConversationModePromptDto,
  ScoringRubricDto,
  SpeechSpeedOptionDto,
  TeacherVoiceDto,
  TopicDto,
} from '@/shared/api/openapi.types';
import { isAgyLocalEnabled } from '@/shared/config/features';
import {
  AGY_LOCAL_MODELS,
  DEFAULT_AI_MODELS,
  DEFAULT_CEFR_PROMPTS,
  DEFAULT_CONVERSATION_MODES,
  DEFAULT_SCORING_RUBRIC,
  DEFAULT_SPEECH_SPEEDS,
  DEFAULT_TEACHER_VOICES,
  DEFAULT_TOPICS,
} from './catalogDefaults';

const AGY_LOCAL_MODEL_CODES = new Set<string>(AGY_LOCAL_MODELS.map((m) => m.code));

/** AGY Local models are hidden (also from the offline fallback list) unless VITE_ENABLE_AGY_LOCAL=true. */
export function withoutDisabledAgyModels<T extends { modelCode: string }>(models: T[]): T[] {
  return isAgyLocalEnabled ? models : models.filter((m) => !AGY_LOCAL_MODEL_CODES.has(m.modelCode));
}

export function filterVocabularyGenerationModels(models: AiModelOptionDto[]): AiModelOptionDto[] {
  return models
    .filter((m) => m.isEnabled && m.isAvailable && m.supportsTextGeneration && m.supportsStructuredOutput)
    .sort((a, b) => a.displayOrder - b.displayOrder);
}

export function filterAudioPronunciationModels(models: AiModelOptionDto[]): AiModelOptionDto[] {
  return models
    .filter((m) => m.isEnabled && m.isAvailable && m.supportsAudioInput && m.supportsPronunciationAssessment)
    .sort((a, b) => a.displayOrder - b.displayOrder);
}

export const catalogApi = {
  async getConversationModes(category?: string): Promise<ConversationModePromptDto[]> {
    try {
      const { data } = await httpClient.get<ConversationModePromptDto[]>('/api/prompts/modes', {
        params: category ? { category } : undefined,
      });
      return Array.isArray(data) && data.length > 0 ? data : DEFAULT_CONVERSATION_MODES;
    } catch {
      return DEFAULT_CONVERSATION_MODES;
    }
  },

  async getCefrLevelPrompts(): Promise<CefrLevelPromptDto[]> {
    try {
      const { data } = await httpClient.get<CefrLevelPromptDto[]>('/api/prompts/cefr-levels');
      return Array.isArray(data) && data.length > 0 ? data : DEFAULT_CEFR_PROMPTS;
    } catch {
      return DEFAULT_CEFR_PROMPTS;
    }
  },

  async getTeacherVoices(onlyEnabled = true, onlyFeatured = false): Promise<TeacherVoiceDto[]> {
    try {
      const { data } = await httpClient.get<TeacherVoiceDto[]>('/api/voices', {
        params: { onlyEnabled, onlyFeatured },
      });
      return Array.isArray(data) && data.length > 0 ? data : DEFAULT_TEACHER_VOICES;
    } catch {
      return onlyFeatured
        ? DEFAULT_TEACHER_VOICES.filter((v) => v.isFeatured)
        : DEFAULT_TEACHER_VOICES;
    }
  },

  async getSpeechSpeeds(): Promise<SpeechSpeedOptionDto[]> {
    try {
      const { data } = await httpClient.get<SpeechSpeedOptionDto[]>('/api/prompts/speech-speeds');
      return Array.isArray(data) && data.length > 0 ? data : DEFAULT_SPEECH_SPEEDS;
    } catch {
      return DEFAULT_SPEECH_SPEEDS;
    }
  },

  async getTopics(params?: {
    onlyActive?: boolean;
    supportsConversation?: boolean;
    supportsVocabulary?: boolean;
    modeCode?: string;
    category?: string;
  }): Promise<TopicDto[]> {
    try {
      const { data } = await httpClient.get<TopicDto[]>('/api/topics', { params });
      if (Array.isArray(data) && data.length > 0) {
        return data;
      }
    } catch {
      // Fallback to seeded topics when offline
    }
    return DEFAULT_TOPICS.filter((t) => {
      if (params?.onlyActive !== false && !t.isActive) return false;
      if (params?.supportsConversation !== undefined && t.supportsConversation !== params.supportsConversation)
        return false;
      if (params?.supportsVocabulary !== undefined && t.supportsVocabulary !== params.supportsVocabulary)
        return false;
      return true;
    });
  },

  async getVocabularyGenerationModels(): Promise<AiModelOptionDto[]> {
    try {
      const { data } = await httpClient.get<AiModelOptionDto[]>('/api/vocabularies/models/generation');
      if (Array.isArray(data) && data.length > 0) {
        return filterVocabularyGenerationModels(withoutDisabledAgyModels(data));
      }
    } catch {
      // fallback
    }
    return filterVocabularyGenerationModels(withoutDisabledAgyModels(DEFAULT_AI_MODELS));
  },

  async getPronunciationModels(): Promise<AiModelOptionDto[]> {
    try {
      const { data } = await httpClient.get<AiModelOptionDto[]>('/api/pronunciation/models');
      if (Array.isArray(data) && data.length > 0) {
        return filterAudioPronunciationModels(withoutDisabledAgyModels(data));
      }
    } catch {
      // fallback
    }
    return filterAudioPronunciationModels(withoutDisabledAgyModels(DEFAULT_AI_MODELS));
  },

  async getActiveScoringRubric(): Promise<ScoringRubricDto> {
    try {
      const { data } = await httpClient.get<ScoringRubricDto>('/api/pronunciation/rubrics/active');
      return data ?? DEFAULT_SCORING_RUBRIC;
    } catch {
      return DEFAULT_SCORING_RUBRIC;
    }
  },
};
