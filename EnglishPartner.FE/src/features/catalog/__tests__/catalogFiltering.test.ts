import { describe, it, expect } from 'vitest';
import {
  filterAudioPronunciationModels,
  filterVocabularyGenerationModels,
} from '../api/catalogApi';
import { DEFAULT_AI_MODELS } from '../api/catalogDefaults';

describe('Database AI Model Capability Filtering', () => {
  it('includes both Gemini and Gemma 4 models for Vocabulary Text Generation', () => {
    const vocabModels = filterVocabularyGenerationModels(DEFAULT_AI_MODELS);
    expect(vocabModels.some((m) => m.modelCode === 'gemini-3.8-flash')).toBe(true);
    expect(vocabModels.some((m) => m.modelCode === 'gemma-4-26b-a4b-it')).toBe(true);
    expect(vocabModels.some((m) => m.modelCode === 'gemma-4-31b-it')).toBe(true);
  });

  it('strictly excludes Text-Only Gemma 4 models from Audio Pronunciation Assessment', () => {
    const audioModels = filterAudioPronunciationModels(DEFAULT_AI_MODELS);
    expect(audioModels.length).toBeGreaterThan(0);
    expect(audioModels.every((m) => m.supportsAudioInput && m.supportsPronunciationAssessment)).toBe(true);
    expect(audioModels.some((m) => m.modelCode.startsWith('gemma-4'))).toBe(false);
  });
});
