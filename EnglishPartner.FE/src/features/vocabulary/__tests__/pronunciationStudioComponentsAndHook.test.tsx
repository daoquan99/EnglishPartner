import React from 'react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import '@/shared/i18n';
import { VocabularyFlashcard } from '../components/VocabularyFlashcard';
import { PronunciationStudio } from '../components/PronunciationStudio';
import { DailyVocabularyToolbar } from '../components/DailyVocabularyToolbar';
import { DailyVocabularyAlerts } from '../components/DailyVocabularyAlerts';
import { DailyVocabularyEmptyState } from '../components/DailyVocabularyEmptyState';
import { usePronunciationStudio } from '../hooks/usePronunciationStudio';
import type { VocabularyFlashcardDto } from '../types/vocabulary.types';

const mockCard: VocabularyFlashcardDto = {
  vocabularyId: '11111111-1111-1111-1111-111111111111',
  senseId: '22222222-2222-2222-2222-222222222222',
  dailyItemId: '33333333-3333-3333-3333-333333333333',
  word: 'resilient',
  partOfSpeech: 'adjective',
  cefrLevel: 'B2',
  topicCategory: 'Workplace & Character',
  definitionEn: 'Able to recover quickly from difficult conditions.',
  meaningVi: 'Kiên cường, có khả năng phục hồi nhanh.',
  descriptionEn: 'Often used to describe people who bounce back after hardships.',
  isCompletedToday: false,
  itemType: 'New',
  pronunciations: [
    { id: 'p-1', accent: 'US', ipa: '/rɪˈzɪl.jənt/', audioUrl: null },
    { id: 'p-2', accent: 'UK', ipa: '/rɪˈzɪl.i.ənt/', audioUrl: null },
  ],
  collocations: [
    { id: 'c-1', phrase: 'highly resilient', meaningVi: 'vô cùng kiên cường' },
  ],
  synonyms: ['tenacious', 'tough'],
  antonyms: ['fragile', 'vulnerable'],
  wordFamilies: [],
  grammarPatterns: [],
  learningStatus: 'New',
  repetitionCount: 0,
  intervalDays: 1,
  easeFactor: 2.5,
  examples: [
    {
      id: 'ex-1',
      sentenceEn: 'Local businesses proved remarkably resilient during the crisis.',
      translationVi: 'Các doanh nghiệp địa phương đã chứng tỏ sự kiên cường đáng kể trong khủng hoảng.',
      cefrLevel: 'B2',
    },
    {
      id: 'ex-2',
      sentenceEn: 'She is resilient and determined to succeed.',
      translationVi: 'Cô ấy kiên cường và quyết tâm thành công.',
      cefrLevel: 'B2',
    },
  ],
};

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: false },
    mutations: { retry: false },
  },
});

function renderWithProviders(ui: React.ReactElement) {
  return renderToStaticMarkup(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        {ui}
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('Daily Vocabulary Components & Pronunciation Studio Hook', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('VocabularyFlashcard Component', () => {
    it('renders front side with word, definition, CEFR badge, and pronunciation buttons', () => {
      const html = renderWithProviders(
        <VocabularyFlashcard
          currentCard={mockCard}
          currentIndex={0}
          totalCards={5}
          isFlipped={false}
          onToggleFlip={vi.fn()}
          onFlipToBack={vi.fn()}
          onPrevCard={vi.fn()}
          onNextCard={vi.fn()}
          onSynthesizeTeacherSpeech={vi.fn()}
          isSynthesizing={false}
          ttsVoiceName="Aoede"
          ttsModelCode="gemini-3.8-live"
          ttsAccent="US"
          onReview={vi.fn()}
        />
      );

      expect(html).toContain('resilient');
      expect(html).toContain('Able to recover quickly from difficult conditions.');
      expect(html).toContain('B2');
      expect(html).toContain('/rɪˈzɪl.jənt/');
      expect(html).toContain('/rɪˈzɪl.i.ənt/');
      expect(html).toContain('Aoede');
    });

    it('renders back side with Vietnamese meaning, collocations, synonyms and antonyms', () => {
      const html = renderWithProviders(
        <VocabularyFlashcard
          currentCard={mockCard}
          currentIndex={0}
          totalCards={5}
          isFlipped={true}
          onToggleFlip={vi.fn()}
          onFlipToBack={vi.fn()}
          onPrevCard={vi.fn()}
          onNextCard={vi.fn()}
          onSynthesizeTeacherSpeech={vi.fn()}
          isSynthesizing={false}
          ttsVoiceName="Aoede"
          ttsModelCode="gemini-3.8-live"
          ttsAccent="US"
          onReview={vi.fn()}
        />
      );

      expect(html).toContain('Kiên cường, có khả năng phục hồi nhanh.');
      expect(html).toContain('highly resilient');
      expect(html).toContain('tenacious');
      expect(html).toContain('fragile');
    });
  });

  describe('PronunciationStudio Component', () => {
    it('renders single word mode with current card word', () => {
      const html = renderWithProviders(
        <PronunciationStudio
          currentCard={mockCard}
          targetType="word"
          onSelectTargetType={vi.fn()}
          examples={mockCard.examples}
          selectedExample={mockCard.examples[0]}
          selectedExampleIndex={0}
          hasExamples={true}
          onSelectExampleIndex={vi.fn()}
          onOpenHistory={vi.fn()}
          onOpenTtsSettings={vi.fn()}
          aiProvider="gemini"
          onSetAiProvider={vi.fn()}
          effectiveAssessmentModel="gemini-3.8-flash"
          onSynthesizeTeacherSpeech={vi.fn()}
          isSynthesizing={false}
          isRecording={false}
          onStartRecording={vi.fn()}
          onStopRecording={vi.fn()}
          isAssessing={false}
          assessmentResult={null}
          assessmentError={null}
          onClearAssessmentError={vi.fn()}
        />
      );

      expect(html).toContain('resilient');
      expect(html).toContain('gemini-3.8-flash');
      expect(html).toContain('Google Gemini API');
    });

    it('renders example sentence mode with sentence pagination when targetType is example', () => {
      const html = renderWithProviders(
        <PronunciationStudio
          currentCard={mockCard}
          targetType="example"
          onSelectTargetType={vi.fn()}
          examples={mockCard.examples}
          selectedExample={mockCard.examples[0]}
          selectedExampleIndex={0}
          hasExamples={true}
          onSelectExampleIndex={vi.fn()}
          onOpenHistory={vi.fn()}
          onOpenTtsSettings={vi.fn()}
          aiProvider="gemini"
          onSetAiProvider={vi.fn()}
          effectiveAssessmentModel="gemini-3.8-flash"
          onSynthesizeTeacherSpeech={vi.fn()}
          isSynthesizing={false}
          isRecording={false}
          onStartRecording={vi.fn()}
          onStopRecording={vi.fn()}
          isAssessing={false}
          assessmentResult={null}
          assessmentError={null}
          onClearAssessmentError={vi.fn()}
        />
      );

      expect(html).toContain(mockCard.examples[0].sentenceEn);
      expect(html).toContain(mockCard.examples[0].translationVi);
    });
  });

  describe('DailyVocabularyToolbar & Alerts', () => {
    it('renders progress, badges and actions in toolbar', () => {
      const html = renderWithProviders(
        <DailyVocabularyToolbar
          cards={[mockCard]}
          currentIndex={0}
          ttsVoiceName="Aoede"
          ttsAccent="US"
          ttsSpeed={1}
          onOpenLearningSettings={vi.fn()}
          onOpenTtsSettings={vi.fn()}
          onOpenAiGenerator={vi.fn()}
        />
      );

      expect(html).toContain('Aoede (US · 1.00x)');
    });

    it('renders review error and api key warning in alerts banner', () => {
      const html = renderWithProviders(
        <DailyVocabularyAlerts
          reviewError="Lỗi mạng khi lưu kết quả ôn tập"
          onClearReviewError={vi.fn()}
          apiKeyWarning="Chưa có API key"
          onClearApiKeyWarning={vi.fn()}
        />
      );

      expect(html).toContain('Lỗi mạng khi lưu kết quả ôn tập');
      expect(html).toContain('Chưa có API key');
    });
  });

  describe('DailyVocabularyEmptyState Component', () => {
    it('renders 404 uninitialized plan empty state with start action', () => {
      const html = renderWithProviders(
        <DailyVocabularyEmptyState
          is404Uninitialized={true}
          planError={{ response: { status: 404 } }}
          isInitializingPlan={false}
          onInitializePlan={vi.fn()}
          onOpenLearningSettings={vi.fn()}
          onRefetchPlan={vi.fn()}
          onOpenAiGenerator={vi.fn()}
        />
      );

      expect(html).toContain('Bắt đầu học hôm nay');
    });

    it('renders network error state with retry button', () => {
      const html = renderWithProviders(
        <DailyVocabularyEmptyState
          is404Uninitialized={false}
          planError={new Error('Connection timed out')}
          isInitializingPlan={false}
          onInitializePlan={vi.fn()}
          onOpenLearningSettings={vi.fn()}
          onRefetchPlan={vi.fn()}
          onOpenAiGenerator={vi.fn()}
        />
      );

      expect(html).toContain('Connection timed out');
      expect(html).toContain('Thử lại');
    });
  });

  describe('usePronunciationStudio Hook Behavior', () => {
    it('initializes with targetType word and exposes cancel/teardown functions', () => {
      let capturedStudio: ReturnType<typeof usePronunciationStudio> | null = null;

      function HookHarness() {
        capturedStudio = usePronunciationStudio({
          currentCard: mockCard,
          onSetApiKeyWarning: vi.fn(),
        });
        return <div>Harness</div>;
      }

      renderWithProviders(<HookHarness />);

      expect(capturedStudio).not.toBeNull();
      expect(capturedStudio!.targetType).toBe('word');
      expect(capturedStudio!.selectedExampleIndex).toBe(0);
      expect(capturedStudio!.isRecording).toBe(false);
      expect(typeof capturedStudio!.resetPronunciationAndStopRecordingSafely).toBe('function');
      expect(typeof capturedStudio!.stopAllAudioPlayback).toBe('function');
      expect(typeof capturedStudio!.handleSynthesizeTeacherSpeech).toBe('function');

      // Calling teardown executes cleanly without throwing
      expect(() => {
        capturedStudio!.resetPronunciationAndStopRecordingSafely();
        capturedStudio!.stopAllAudioPlayback();
      }).not.toThrow();
    });
  });
});
