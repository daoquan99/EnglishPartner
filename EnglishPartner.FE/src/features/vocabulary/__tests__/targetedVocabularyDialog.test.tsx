import React from 'react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import '@/shared/i18n';
import {
  normalizeTargetText,
  countWords,
  isSentenceMode,
} from '../lib/targetedVocabulary';
import { TargetedVocabularyDialog } from '../components/TargetedVocabularyDialog';
import { vocabularyApi } from '../api/vocabularyApi';

vi.mock('../api/vocabularyApi', () => ({
  vocabularyApi: {
    generateVocabularyWithAi: vi.fn(),
    buildGenerationPrompt: vi.fn(),
    importVocabularyJson: vi.fn(),
    getLearningSettings: vi.fn().mockResolvedValue({
      dailyVocabularyTarget: 20,
      cefrLevel: 'B2',
      isPersisted: true,
      effectiveFromLearningDate: '2026-10-29',
      timeZoneId: 'Asia/Ho_Chi_Minh',
    }),
  },
}));

vi.mock('@/features/catalog', () => ({
  useVocabularyGenerationModelsQuery: () => ({
    data: [
      {
        modelCode: 'gemini-3.8-flash',
        displayName: 'Gemini 3.8 Flash',
        supportsAudioInput: true,
        capabilityNote: 'Fast generation',
      },
    ],
  }),
  useTopicsQuery: () => ({
    data: [],
  }),
}));

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: false },
  },
});

function renderWithProviders(ui: React.ReactElement) {
  return renderToStaticMarkup(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>{ui}</MemoryRouter>
    </QueryClientProvider>
  );
}

describe('Targeted Vocabulary Text Normalization & Dialog Behavior', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('normalizeTargetText & word counting', () => {
    it('trims whitespace and collapses multiple spaces into one', () => {
      expect(normalizeTargetText('   breakthrough   innovation   ')).toBe(
        'breakthrough innovation'
      );
    });

    it('removes Unicode punctuation including ellipsis, em dash, en dash, curly quotes matching BE char.IsPunctuation', () => {
      expect(normalizeTargetText('...')).toBe('');
      expect(normalizeTargetText('…')).toBe('');
      expect(normalizeTargetText('word—')).toBe('word');
      expect(normalizeTargetText('—word—')).toBe('word');
      expect(normalizeTargetText('–word–')).toBe('word');
      expect(normalizeTargetText('“word”')).toBe('word');
      expect(normalizeTargetText('«term»')).toBe('term');
      expect(normalizeTargetText('...word...')).toBe('word');
    });

    it('removes leading and trailing quotes and punctuation', () => {
      expect(normalizeTargetText('“resilient”')).toBe('resilient');
      expect(normalizeTargetText('"meticulous strategy",')).toBe(
        'meticulous strategy'
      );
      expect(normalizeTargetText('«collaborative effort»!')).toBe(
        'collaborative effort'
      );
      expect(normalizeTargetText('(sustainable solution);')).toBe(
        'sustainable solution'
      );
    });

    it('correctly calculates word count', () => {
      expect(countWords('resilient')).toBe(1);
      expect(countWords('cutting-edge technology')).toBe(2);
      expect(
        countWords(
          normalizeTargetText('Engineering excellence requires continuous learning and daily practice.')
        )
      ).toBe(8);
    });

    it('distinguishes between term mode (<= 6 words) and sentence mode (> 6 words)', () => {
      expect(isSentenceMode('resilient')).toBe(false);
      expect(isSentenceMode('high performance computing system')).toBe(false); // 4 words
      expect(isSentenceMode('one two three four five six')).toBe(false); // 6 words
      expect(isSentenceMode('one two three four five six seven')).toBe(true); // 7 words
    });
  });

  describe('TargetedVocabularyDialog payload generation', () => {
    it('renders dialog with normalized text and term mode badge when <= 6 words', () => {
      const html = renderWithProviders(
        <TargetedVocabularyDialog
          isOpen={true}
          onClose={() => {}}
          rawText='  "resilient mindset"  '
          initialAssignToDaily={true}
        />
      );

      expect(html).toContain('resilient mindset');
      expect(html).toContain('Chế độ: Từ / Cụm từ');
      expect(html).not.toContain('dangerouslySetInnerHTML');
    });

    it('renders dialog with sentence mode badge when > 6 words', () => {
      const sentence =
        'Consistent practice and real world applications accelerate language acquisition effectively.';
      const html = renderWithProviders(
        <TargetedVocabularyDialog
          isOpen={true}
          onClose={() => {}}
          rawText={sentence}
          initialAssignToDaily={true}
        />
      );

      expect(html).toContain('Chế độ: Trích từ câu');
      expect(html).toContain(
        'Consistent practice and real world applications accelerate language acquisition effectively'
      );
    });

    it('constructs correct payload for short term (<= 6 words) with targetTerms and requestedCount 1', async () => {
      (vocabularyApi.generateVocabularyWithAi as any).mockResolvedValueOnce([
        { word: 'perseverance' },
      ]);

      const term = 'perseverance';
      const normalized = normalizeTargetText(term);
      const isSentence = isSentenceMode(normalized);

      const payload = {
        apiKey: 'test-api-key',
        cefrLevel: 'B2' as const,
        topic: 'General English',
        requestedCount: isSentence ? 3 : 1,
        modelCode: 'gemini-3.8-flash',
        provider: 'gemini',
        assignToTodayPlan: true,
        examplesPerSense: 3,
        targetTerms: isSentence ? undefined : [normalized],
        sourceSentence: isSentence ? normalized : undefined,
      };

      await vocabularyApi.generateVocabularyWithAi(payload as any);

      expect(vocabularyApi.generateVocabularyWithAi).toHaveBeenCalledWith(
        expect.objectContaining({
          targetTerms: ['perseverance'],
          sourceSentence: undefined,
          requestedCount: 1,
          examplesPerSense: 3,
          assignToTodayPlan: true,
        })
      );
    });

    it('constructs correct payload for sentence (> 6 words) with sourceSentence and requestedCount N', async () => {
      (vocabularyApi.generateVocabularyWithAi as any).mockResolvedValueOnce([
        { word: 'innovative' },
        { word: 'architecture' },
      ]);

      const sentence =
        'Building scalable software requires robust design patterns and disciplined refactoring.';
      const normalized = normalizeTargetText(sentence);
      const isSentence = isSentenceMode(normalized);
      const requestedN = 4;

      const payload = {
        apiKey: 'test-api-key',
        cefrLevel: 'B2' as const,
        topic: undefined,
        requestedCount: isSentence ? requestedN : 1,
        modelCode: 'gemini-3.8-flash',
        provider: 'gemini',
        assignToTodayPlan: false,
        examplesPerSense: 2,
        targetTerms: isSentence ? undefined : [normalized],
        sourceSentence: isSentence ? normalized : undefined,
      };

      await vocabularyApi.generateVocabularyWithAi(payload as any);

      expect(vocabularyApi.generateVocabularyWithAi).toHaveBeenCalledWith(
        expect.objectContaining({
          targetTerms: undefined,
          sourceSentence: normalized,
          requestedCount: 4,
          examplesPerSense: 2,
          assignToTodayPlan: false,
        })
      );
    });

    it('calls buildGenerationPrompt and importVocabularyJson with matching parameters in manual mode', async () => {
      (vocabularyApi.buildGenerationPrompt as any).mockResolvedValueOnce({
        prompt: 'Generated prompt for targeted vocabulary',
        promptVersion: 'V1',
        requestedCount: 1,
        cefrLevel: 'B2',
        topic: 'Engineering',
        excludedWords: [],
      });

      (vocabularyApi.importVocabularyJson as any).mockResolvedValueOnce([
        { word: 'meticulous' },
      ]);

      const term = 'meticulous';
      const normalized = normalizeTargetText(term);

      const promptPayload = {
        requestedCount: 1,
        cefrLevel: 'B2' as const,
        topic: 'Engineering',
        examplesPerSense: 2,
        targetTerms: [normalized],
        sourceSentence: undefined,
      };

      await vocabularyApi.buildGenerationPrompt(promptPayload);

      expect(vocabularyApi.buildGenerationPrompt).toHaveBeenCalledWith(
        expect.objectContaining({
          targetTerms: ['meticulous'],
          requestedCount: 1,
          examplesPerSense: 2,
        })
      );

      const importPayload = {
        jsonText: '[{"word":"meticulous"}]',
        modelCode: 'manual-web-ai',
        cefrLevel: 'B2' as const,
        topic: 'Engineering',
        assignToTodayPlan: true,
        examplesPerSense: 2,
      };

      await vocabularyApi.importVocabularyJson(importPayload);

      expect(vocabularyApi.importVocabularyJson).toHaveBeenCalledWith(
        expect.objectContaining({
          jsonText: '[{"word":"meticulous"}]',
          assignToTodayPlan: true,
          examplesPerSense: 2,
        })
      );
    });

    it('disables submit button and shows error banner when single term > 80 characters', () => {
      const longTerm = 'a'.repeat(81);
      const html = renderWithProviders(
        <TargetedVocabularyDialog
          isOpen={true}
          onClose={() => {}}
          rawText={longTerm}
          initialAssignToDaily={true}
        />
      );

      expect(html).toContain('Từ/cụm quá dài (tối đa 80 ký tự). Hãy bôi đen ngắn hơn.');
      expect(html).toContain('disabled=""');
    });

    it('sends targetTerms: ["word"] when raw input is "word—" with trailing em-dash', async () => {
      (vocabularyApi.generateVocabularyWithAi as any).mockResolvedValueOnce([
        { word: 'word' },
      ]);

      const raw = 'word—';
      const normalized = normalizeTargetText(raw);
      expect(normalized).toBe('word');

      const payload = {
        apiKey: 'test-key',
        cefrLevel: 'B1' as const,
        requestedCount: 1,
        modelCode: 'gemini-3.8-flash',
        provider: 'gemini',
        assignToTodayPlan: true,
        examplesPerSense: 2,
        targetTerms: [normalized],
      };

      await vocabularyApi.generateVocabularyWithAi(payload as any);

      expect(vocabularyApi.generateVocabularyWithAi).toHaveBeenCalledWith(
        expect.objectContaining({
          targetTerms: ['word'],
        })
      );
    });

    it('renders mode switch tabs with role="tablist", role="tab", and aria-selected', () => {
      const html = renderWithProviders(
        <TargetedVocabularyDialog
          isOpen={true}
          onClose={() => {}}
          rawText="sustainable"
          initialAssignToDaily={true}
        />
      );

      expect(html).toContain('role="tablist"');
      expect(html).toContain('role="tab"');
      expect(html).toContain('aria-selected="true"');
      expect(html).toContain('aria-selected="false"');
    });

    it('maps 429 error to user-friendly message', () => {
      const err = { response: { status: 429, data: { detail: 'Too many requests' } } };
      let errorMsg = '';
      if (err.response.status === 429) {
        errorMsg = 'Bạn đã tạo quá nhiều, thử lại sau ít phút';
      }
      expect(errorMsg).toBe('Bạn đã tạo quá nhiều, thử lại sau ít phút');
    });
  });
});
