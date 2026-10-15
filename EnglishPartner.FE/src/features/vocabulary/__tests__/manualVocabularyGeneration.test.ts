import { describe, it, expect, vi, beforeEach } from 'vitest';
import { vocabularyApi } from '../api/vocabularyApi';
import { httpClient } from '@/shared/api/httpClient';

vi.mock('@/shared/api/httpClient', () => ({
  httpClient: {
    post: vi.fn(),
  },
}));

describe('Manual Vocabulary Generation API', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('buildGenerationPrompt calls POST /api/vocabularies/generation/prompt with payload and returns prompt DTO', async () => {
    const mockResponse = {
      data: {
        prompt: 'You are an expert English lexicographer...',
        promptVersion: 'PROMPT_VOCAB_GEN_V1',
        requestedCount: 3,
        cefrLevel: 'B2',
        topic: 'Workplace communication',
        excludedWords: ['hello'],
      },
    };

    (httpClient.post as any).mockResolvedValueOnce(mockResponse);

    const payload = {
      requestedCount: 3,
      cefrLevel: 'B2' as const,
      topic: 'Workplace communication',
      excludedWords: ['hello'],
    };

    const result = await vocabularyApi.buildGenerationPrompt(payload);

    expect(httpClient.post).toHaveBeenCalledWith(
      '/api/vocabularies/generation/prompt',
      payload
    );
    expect(result).toEqual(mockResponse.data);
    expect(result.prompt).toContain('expert English lexicographer');
  });

  it('importVocabularyJson calls POST /api/vocabularies/generation/import with payload and returns flashcard array', async () => {
    const mockFlashcards = [
      {
        vocabularyId: 'voc-1',
        senseId: 'sen-1',
        word: 'resilient',
        partOfSpeech: 'adjective',
        cefrLevel: 'B2',
        definitionEn: 'Able to recover quickly',
        meaningVi: 'Kiên cường',
        descriptionEn: 'Important term',
        pronunciations: [],
        synonyms: ['strong'],
        antonyms: ['fragile'],
        wordFamilies: [],
        collocations: [],
        examples: [],
        grammarPatterns: [],
        learningStatus: 'New',
        repetitionCount: 0,
        intervalDays: 0,
        easeFactor: 2.5,
        isCompletedToday: false,
      },
    ];

    (httpClient.post as any).mockResolvedValueOnce({ data: mockFlashcards });

    const payload = {
      jsonText: '[{"word":"resilient"}]',
      modelCode: 'manual-web-ai',
      cefrLevel: 'B2' as const,
      topic: 'Workplace communication',
      assignToTodayPlan: true,
    };

    const result = await vocabularyApi.importVocabularyJson(payload);

    expect(httpClient.post).toHaveBeenCalledWith(
      '/api/vocabularies/generation/import',
      payload
    );
    expect(result).toEqual(mockFlashcards);
    expect(result[0].word).toBe('resilient');
  });
});
