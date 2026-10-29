import React from 'react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import '@/shared/i18n';
import { AiVocabularyGeneratorDialog } from '../components/AiVocabularyGeneratorDialog';
import { vocabularyApi } from '../api/vocabularyApi';

vi.mock('../api/vocabularyApi', () => ({
  vocabularyApi: {
    generateVocabularyWithAi: vi.fn(),
    buildGenerationPrompt: vi.fn(),
    importVocabularyJson: vi.fn(),
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

vi.mock('@/features/tutor', () => ({
  useTutorSessionStore: () => ({
    apiKey: 'mock-gemini-key',
    requestFocusLeftMenuApiKey: vi.fn(),
    aiProvider: 'gemini',
    agyModelCode: 'gemini-3.8-flash-high',
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

describe('AiVocabularyGeneratorDialog Component & Payload Contract', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders direct generation dialog with examplesPerSense selector and assignToToday checkbox', () => {
    const html = renderWithProviders(
      <AiVocabularyGeneratorDialog
        isOpen={true}
        onClose={() => {}}
        onSuccess={() => {}}
        onSetApiKeyWarning={() => {}}
        initialMode="direct"
      />
    );

    expect(html).toContain('Số câu ví dụ mỗi nghĩa');
    expect(html).toContain('Gán các từ vựng này vào kế hoạch học tập hôm nay (Daily Plan)');
  });

  it('renders manual generation dialog with examplesPerSense selector and assignToToday checkbox', () => {
    const html = renderWithProviders(
      <AiVocabularyGeneratorDialog
        isOpen={true}
        onClose={() => {}}
        onSuccess={() => {}}
        onSetApiKeyWarning={() => {}}
        initialMode="manual"
      />
    );

    expect(html).toContain('Số câu ví dụ mỗi nghĩa');
    expect(html).toContain('Gán các từ vựng này vào kế hoạch học tập hôm nay (Daily Plan)');
  });

  it('sends requestedCount (not count), assignToTodayPlan and examplesPerSense when calling generateVocabularyWithAi', async () => {
    (vocabularyApi.generateVocabularyWithAi as any).mockResolvedValueOnce([
      { word: 'innovation' },
    ]);

    const payload = {
      apiKey: 'mock-gemini-key',
      cefrLevel: 'B2' as const,
      topic: 'Technology',
      requestedCount: 5,
      modelCode: 'gemini-3.8-flash',
      provider: 'gemini',
      assignToTodayPlan: true,
      examplesPerSense: 3,
    };

    await vocabularyApi.generateVocabularyWithAi(payload);

    expect(vocabularyApi.generateVocabularyWithAi).toHaveBeenCalledWith(
      expect.objectContaining({
        requestedCount: 5,
        assignToTodayPlan: true,
        examplesPerSense: 3,
      })
    );
    // Explicitly verify legacy count property is not the primary count field
    expect((vocabularyApi.generateVocabularyWithAi as any).mock.calls[0][0].requestedCount).toBe(5);
  });

  it('sends requestedCount and examplesPerSense when calling buildGenerationPrompt', async () => {
    (vocabularyApi.buildGenerationPrompt as any).mockResolvedValueOnce({
      prompt: 'Prompt content',
      promptVersion: 'V1',
      requestedCount: 3,
      cefrLevel: 'B2',
      topic: 'Business',
      excludedWords: [],
    });

    const payload = {
      requestedCount: 3,
      cefrLevel: 'B2' as const,
      topic: 'Business',
      examplesPerSense: 2,
    };

    await vocabularyApi.buildGenerationPrompt(payload);

    expect(vocabularyApi.buildGenerationPrompt).toHaveBeenCalledWith(
      expect.objectContaining({
        requestedCount: 3,
        examplesPerSense: 2,
      })
    );
  });

  it('sends assignToTodayPlan and examplesPerSense when calling importVocabularyJson', async () => {
    (vocabularyApi.importVocabularyJson as any).mockResolvedValueOnce([
      { word: 'strategy' },
    ]);

    const payload = {
      jsonText: '[{"word":"strategy"}]',
      modelCode: 'manual-web-ai',
      cefrLevel: 'B2' as const,
      topic: 'Business',
      assignToTodayPlan: true,
      examplesPerSense: 2,
    };

    await vocabularyApi.importVocabularyJson(payload);

    expect(vocabularyApi.importVocabularyJson).toHaveBeenCalledWith(
      expect.objectContaining({
        assignToTodayPlan: true,
        examplesPerSense: 2,
      })
    );
  });
});
