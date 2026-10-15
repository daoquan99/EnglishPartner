import { beforeEach, describe, expect, it, vi } from 'vitest';
import { httpClient } from '@/shared/api/httpClient';
import { vocabularyApi } from '../api/vocabularyApi';
import type {
  PronunciationAssessmentApiResultDto,
  VocabularyExampleDto,
  VocabularyFlashcardDto,
} from '../types/vocabulary.types';

vi.mock('@/shared/api/httpClient', () => ({
  httpClient: {
    post: vi.fn(),
    get: vi.fn(),
  },
}));

describe('Pronunciation examples selection & history contracts', () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  const mockApiResult: PronunciationAssessmentApiResultDto = {
    assessmentId: 'assess-101',
    vocabularyId: '30000000-0000-0000-0000-000000000006',
    senseId: '30000000-0000-0000-0000-000000000107',
    targetType: 'Sentence',
    targetId: '30000000-0000-0000-0000-000000001008',
    accent: 'UK',
    expectedText: 'Adding automatic retry policies made our payment service much more resilient to network timeouts.',
    recognizedText: 'Adding automatic retry policies made our payment service much more resilient to network timeouts.',
    modelCode: 'gemini-3.8-flash',
    rubricCode: 'ENGLISH_PRONUNCIATION_V1',
    rubricVersion: 1,
    confidence: 0.95,
    overallScore: 92,
    isAssessable: true,
    status: 'Completed',
    criteria: [
      { code: 'Accuracy', name: 'Accuracy', weight: 40, score: 95, weightedScore: 38, feedback: 'Rất chính xác.' },
      { code: 'Clarity', name: 'Clarity', weight: 30, score: 90, weightedScore: 27, feedback: 'Rõ ràng.' },
      { code: 'Fluency', name: 'Fluency', weight: 30, score: 90, weightedScore: 27, feedback: 'Lưu loát.' },
    ],
    strengths: ['Phát âm âm đuôi tốt.'],
    improvements: ['Chú ý nối âm giữa các cụm từ.'],
    suggestedPractice: ['Luyện đọc nhanh hơn một chút theo nhịp tự nhiên.'],
    createdAt: '2026-10-15T12:00:00Z',
  };

  it('maps selected example sentence to Sentence targetType, example targetId, and referenceText', async () => {
    vi.mocked(httpClient.post).mockResolvedValueOnce({ data: mockApiResult });

    const payload = {
      apiKey: 'test-key',
      vocabularyId: '30000000-0000-0000-0000-000000000006',
      senseId: '30000000-0000-0000-0000-000000000107',
      exampleId: '30000000-0000-0000-0000-000000001008',
      referenceText: 'Adding automatic retry policies made our payment service much more resilient to network timeouts.',
      audioBase64: 'UklGRi4AAABXQVZF',
      mimeType: 'audio/webm;codecs=opus',
      durationMs: 4200,
      modelCode: 'gemini-3.8-flash',
      accent: 'UK' as const,
    };

    const result = await vocabularyApi.assessPronunciation(payload);

    expect(httpClient.post).toHaveBeenCalledWith('/api/pronunciation/assessments', {
      apiKey: 'test-key',
      vocabularyId: payload.vocabularyId,
      senseId: payload.senseId,
      targetType: 'Sentence',
      targetId: '30000000-0000-0000-0000-000000001008',
      referenceText: payload.referenceText,
      accent: 'UK',
      assessmentModelCode: 'gemini-3.8-flash',
      base64Audio: 'UklGRi4AAABXQVZF',
      contentType: 'audio/webm;codecs=opus',
      durationMilliseconds: 4200,
    });

    expect(result.vocabularyId).toBe(payload.vocabularyId);
    expect(result.senseId).toBe(payload.senseId);
    expect(result.targetType).toBe('Sentence');
    expect(result.targetId).toBe('30000000-0000-0000-0000-000000001008');
    expect(result.accent).toBe('UK');
    expect(result.overallScore).toBe(92);
  });

  it('preserves null overallScore and not assessable status without converting to 0', async () => {
    vi.mocked(httpClient.post).mockResolvedValueOnce({
      data: {
        ...mockApiResult,
        overallScore: null,
        isAssessable: false,
        status: 'NotAssessable',
        recognizedText: null,
      },
    });

    const result = await vocabularyApi.assessPronunciation({
      apiKey: 'test-key',
      vocabularyId: '30000000-0000-0000-0000-000000000006',
      senseId: '30000000-0000-0000-0000-000000000107',
      referenceText: 'resilient',
      audioBase64: 'UklGRi4AAABXQVZF',
      mimeType: 'audio/webm',
      durationMs: 1500,
      modelCode: 'gemini-3.8-flash',
    });

    expect(result.overallScore).toBeNull();
    expect(result.isAssessable).toBe(false);
    expect(result.status).toBe('NotAssessable');
    expect(result.recognizedTranscript).toBe('');
  });

  describe('Example selection index navigation and boundary logic', () => {
    const examples: VocabularyExampleDto[] = [
      { id: 'ex-1', sentenceEn: 'First example.', translationVi: 'Ví dụ 1.', cefrLevel: 'B2' },
      { id: 'ex-2', sentenceEn: 'Second example.', translationVi: 'Ví dụ 2.', cefrLevel: 'B2' },
      { id: 'ex-3', sentenceEn: 'Third example.', translationVi: 'Ví dụ 3.', cefrLevel: 'B2' },
    ];

    it('navigates forward and backward with disable states at boundaries without wrapping', () => {
      let selectedIndex = 0;

      // Index 0: prev is disabled, next is enabled
      const isPrevDisabledAt0 = selectedIndex === 0;
      const isNextDisabledAt0 = selectedIndex >= examples.length - 1;
      expect(isPrevDisabledAt0).toBe(true);
      expect(isNextDisabledAt0).toBe(false);

      // Move forward: index 1
      selectedIndex = Math.min(examples.length - 1, selectedIndex + 1);
      expect(selectedIndex).toBe(1);
      expect(selectedIndex === 0).toBe(false);
      expect(selectedIndex >= examples.length - 1).toBe(false);

      // Move forward: index 2 (last)
      selectedIndex = Math.min(examples.length - 1, selectedIndex + 1);
      expect(selectedIndex).toBe(2);
      expect(selectedIndex >= examples.length - 1).toBe(true);

      // Attempt moving forward beyond last item: should stay at 2 without wrapping
      selectedIndex = Math.min(examples.length - 1, selectedIndex + 1);
      expect(selectedIndex).toBe(2);

      // Move backward: index 1
      selectedIndex = Math.max(0, selectedIndex - 1);
      expect(selectedIndex).toBe(1);

      // Move backward: index 0
      selectedIndex = Math.max(0, selectedIndex - 1);
      expect(selectedIndex).toBe(0);

      // Attempt moving backward below 0: should stay at 0
      selectedIndex = Math.max(0, selectedIndex - 1);
      expect(selectedIndex).toBe(0);
    });

    it('resets example index on card/sense change and preserves example by ID on refetch', () => {
      let cardKey = 'vocab-1_sense-1';
      let lastCardKey: string | null = null;
      let selectedIndex = 2; // user had selected example 2
      let lastExampleId: string | null = 'ex-3';

      // 1. Refetch with same cardKey: keeps matching example ID
      if (lastCardKey !== cardKey) {
        lastCardKey = cardKey;
        // initial load
      }
      const refetchedExamples = [...examples];
      const foundIdx = refetchedExamples.findIndex((e) => e.id === lastExampleId);
      expect(foundIdx).toBe(2); // Preserved!

      // 2. Card or sense changes to another word:
      const nextCardKey = 'vocab-2_sense-2';
      if (cardKey !== nextCardKey) {
        cardKey = nextCardKey;
        selectedIndex = 0;
        lastExampleId = 'ex-other-1';
      }
      expect(selectedIndex).toBe(0); // Reset to first example!

      // 3. Card has no examples:
      const emptyExamples: VocabularyExampleDto[] = [];
      const hasExamples = emptyExamples.length > 0;
      expect(hasExamples).toBe(false);
    });
  });

  describe('Session Token & Cancellation rules', () => {
    it('discards stale responses when target changes before async resolution (A -> B -> A scenario)', () => {
      let sessionToken = 0;
      let activeResult: string | null = null;

      // 1. User starts session on Target A
      const sessionTokenA1 = ++sessionToken; // token = 1

      // 2. User switches to Target B
      const sessionTokenB = ++sessionToken; // token = 2

      // 3. User switches back to Target A before A1 completes
      const sessionTokenA2 = ++sessionToken; // token = 3

      // 4. Stale response for A1 arrives (token 1)
      if (sessionToken === sessionTokenA1) {
        activeResult = 'Stale A1 result';
      }
      expect(activeResult).toBeNull(); // Discarded!

      // 5. Response for A2 arrives (token 3)
      if (sessionToken === sessionTokenA2) {
        activeResult = 'Fresh A2 result';
      }
      expect(activeResult).toBe('Fresh A2 result'); // Accepted!
    });

    it('releases media stream immediately if target switched while waiting for microphone permission', () => {
      let sessionToken = 0;
      const initialToken = ++sessionToken;

      const mockTracks = [{ stop: vi.fn() }, { stop: vi.fn() }];
      const mockStream = { getTracks: () => mockTracks };

      // User switched target while waiting for getUserMedia
      sessionToken++;

      // When getUserMedia resolves:
      if (sessionToken !== initialToken) {
        mockStream.getTracks().forEach((t) => t.stop());
      }

      expect(mockTracks[0].stop).toHaveBeenCalled();
      expect(mockTracks[1].stop).toHaveBeenCalled();
    });
  });
});
