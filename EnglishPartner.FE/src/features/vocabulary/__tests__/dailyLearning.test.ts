import { describe, it, expect, vi, beforeEach } from 'vitest';
import { vocabularyApi, mapPlanResponse } from '../api/vocabularyApi';
import { httpClient } from '@/shared/api/httpClient';

vi.mock('@/shared/api/httpClient', () => ({
  httpClient: {
    get: vi.fn(),
    put: vi.fn(),
    post: vi.fn(),
  },
}));

describe('Daily Learning API & Adapters', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('getLearningSettings', () => {
    it('calls GET /api/learning/settings and returns settings DTO', async () => {
      const mockSettings = {
        dailyVocabularyTarget: 25,
        cefrLevel: 'B2',
        isPersisted: true,
        effectiveFromLearningDate: '2026-10-18',
        timeZoneId: 'Asia/Ho_Chi_Minh',
      };

      (httpClient.get as any).mockResolvedValueOnce({ data: mockSettings });

      const result = await vocabularyApi.getLearningSettings();

      expect(httpClient.get).toHaveBeenCalledWith('/api/learning/settings');
      expect(result).toEqual(mockSettings);
      expect(result.dailyVocabularyTarget).toBe(25);
      expect(result.cefrLevel).toBe('B2');
      expect(result.isPersisted).toBe(true);
    });
  });

  describe('updateLearningSettings', () => {
    it('calls PUT /api/learning/settings with target and level', async () => {
      const payload = {
        dailyVocabularyTarget: 15,
        cefrLevel: 'C1' as const,
      };

      const mockResponse = {
        dailyVocabularyTarget: 15,
        cefrLevel: 'C1',
        isPersisted: true,
        effectiveFromLearningDate: '2026-10-19',
        timeZoneId: 'Asia/Ho_Chi_Minh',
      };

      (httpClient.put as any).mockResolvedValueOnce({ data: mockResponse });

      const result = await vocabularyApi.updateLearningSettings(payload);

      expect(httpClient.put).toHaveBeenCalledWith('/api/learning/settings', payload);
      expect(result.dailyVocabularyTarget).toBe(15);
      expect(result.cefrLevel).toBe('C1');
      expect(result.effectiveFromLearningDate).toBe('2026-10-19');
    });
  });

  describe('initializeTodayPlan', () => {
    it('calls POST /api/learning/flashcards/today/initialize with empty body', async () => {
      const rawApiPlan = {
        planId: 'plan-123',
        userId: 'user-abc',
        learningDate: '2026-10-18',
        timeZoneId: 'Asia/Ho_Chi_Minh',
        newVocabularyTarget: 20,
        targetCefrLevel: 'B1',
        newCount: 12,
        carryOverCount: 8,
        firstLearningAssignedCount: 20,
        firstLearningCompletedCount: 5,
        firstLearningRemainingCount: 15,
        pendingReviewCount: 3,
        remainingReviewCount: 2,
        shortfallCount: 0,
        overflowCount: 0,
        items: [
          {
            dailyItemId: 'item-1',
            vocabularyId: 'voc-1',
            senseId: 'sen-1',
            word: 'resilient',
            partOfSpeech: 'adjective',
            cefrLevel: 'B1',
            definitionEn: 'Able to recover quickly',
            meaningVi: 'Kiên cường',
            itemType: 'CarryOver',
            itemStatus: 'Pending',
            pronunciations: [],
            synonyms: [],
            antonyms: [],
            wordFamilies: [],
            collocations: [],
            examples: [],
            grammarPatterns: [],
          },
        ],
      };

      (httpClient.post as any).mockResolvedValueOnce({ data: rawApiPlan });

      const result = await vocabularyApi.initializeTodayPlan();

      expect(httpClient.post).toHaveBeenCalledWith('/api/learning/flashcards/today/initialize', {});
      expect(result.planId).toBe('plan-123');
      expect(result.targetWordCount).toBe(20);
      expect(result.targetCefrLevel).toBe('B1');
      expect(result.firstLearningAssignedCount).toBe(20);
      expect(result.carryOverCount).toBe(8);
      expect(result.newWordCount).toBe(12);
      expect(result.items).toHaveLength(1);
      expect(result.items[0].itemType).toBe('CarryOver');
    });
  });

  describe('getTodayPlan and mapPlanResponse', () => {
    it('calls GET /api/learning/flashcards/today without query target', async () => {
      const rawApiPlan = {
        planId: 'plan-456',
        userId: 'user-xyz',
        learningDate: '2026-10-18',
        newVocabularyTarget: 20,
        targetCefrLevel: 'B2',
        newCount: 15,
        carryOverCount: 0,
        pendingReviewCount: 4,
        completedCount: 2,
        items: [],
      };

      (httpClient.get as any).mockResolvedValueOnce({ data: rawApiPlan });

      const result = await vocabularyApi.getTodayPlan();

      expect(httpClient.get).toHaveBeenCalledWith('/api/learning/flashcards/today', { params: {} });
      expect(result.targetWordCount).toBe(20);
      expect(result.targetCefrLevel).toBe('B2');
    });

    it('passes timeZoneId when provided', async () => {
      (httpClient.get as any).mockResolvedValueOnce({ data: { planId: 'p1', items: [] } });

      await vocabularyApi.getTodayPlan('America/New_York');

      expect(httpClient.get).toHaveBeenCalledWith('/api/learning/flashcards/today', {
        params: { timeZoneId: 'America/New_York' },
      });
    });

    it('propagates 404 error without mock fallback', async () => {
      const notFoundError = {
        response: {
          status: 404,
          data: { code: 'daily_plan_not_initialized', error: 'Daily plan not initialized yet' },
        },
      };

      (httpClient.get as any).mockRejectedValueOnce(notFoundError);

      await expect(vocabularyApi.getTodayPlan()).rejects.toEqual(notFoundError);
    });

    it('mapPlanResponse correctly computes shortfallCount and overflowCount', () => {
      // Scenario 1: Shortfall (target 20, only 15 available in catalog, 0 carry-over)
      const shortfallRaw = {
        planId: 'plan-shortfall',
        newVocabularyTarget: 20,
        targetCefrLevel: 'C1',
        newCount: 15,
        carryOverCount: 0,
        firstLearningAssignedCount: 15,
        shortfallCount: 5,
        overflowCount: 0,
        items: [],
      };

      const mappedShortfall = mapPlanResponse(shortfallRaw);
      expect(mappedShortfall.shortfallCount).toBe(5);
      expect(mappedShortfall.overflowCount).toBe(0);
      expect(mappedShortfall.firstLearningAssignedCount).toBe(15);
      expect(mappedShortfall.targetWordCount).toBe(20);

      // Scenario 2: Overflow (15 carry-over, target changed to 10 -> assigned 15, overflow 5)
      const overflowRaw = {
        planId: 'plan-overflow',
        newVocabularyTarget: 10,
        targetCefrLevel: 'B1',
        newCount: 0,
        carryOverCount: 15,
        firstLearningAssignedCount: 15,
        shortfallCount: 0,
        overflowCount: 5,
        items: [],
      };

      const mappedOverflow = mapPlanResponse(overflowRaw);
      expect(mappedOverflow.overflowCount).toBe(5);
      expect(mappedOverflow.shortfallCount).toBe(0);
      expect(mappedOverflow.firstLearningAssignedCount).toBe(15);
      expect(mappedOverflow.targetWordCount).toBe(10);
    });

    it('mapPlanResponse normalizes itemType casing and itemStatus completion', () => {
      const raw = {
        planId: 'plan-items',
        items: [
          {
            dailyItemId: 'item-new',
            vocabularyId: 'v1',
            senseId: 's1',
            word: 'innovate',
            itemType: 'new',
            itemStatus: 'Pending',
          },
          {
            dailyItemId: 'item-carry',
            vocabularyId: 'v2',
            senseId: 's2',
            word: 'persist',
            itemType: 'carry_over',
            itemStatus: 'Completed',
          },
          {
            dailyItemId: 'item-review',
            vocabularyId: 'v3',
            senseId: 's3',
            word: 'optimize',
            itemType: 'review',
            itemStatus: 'Completed',
          },
        ],
      };

      const mapped = mapPlanResponse(raw);
      expect(mapped.items[0].itemType).toBe('New');
      expect(mapped.items[0].isCompletedToday).toBe(false);

      expect(mapped.items[1].itemType).toBe('CarryOver');
      expect(mapped.items[1].isCompletedToday).toBe(true);

      expect(mapped.items[2].itemType).toBe('Review');
      expect(mapped.items[2].isCompletedToday).toBe(true);
    });
  });

  describe('submitReview', () => {
    it('calls POST /api/learning/flashcards/{itemId}/review when dailyItemId provided with clientRequestId', async () => {
      const mockUpdated = {
        vocabularyId: 'voc-1',
        senseId: 'sen-1',
        word: 'resilient',
        learningStatus: 'Review',
        repetitionCount: 1,
        intervalDays: 1,
        easeFactor: 2.5,
        isCompletedToday: true,
      };

      (httpClient.post as any).mockResolvedValueOnce({ data: mockUpdated });

      const payload = {
        vocabularyId: 'voc-1',
        senseId: 'sen-1',
        rating: 'Good' as const,
        responseTimeMs: 2500,
        clientRequestId: 'client-req-uuid-123',
      };

      const result = await vocabularyApi.submitReview(payload, 'item-uuid-456');

      expect(httpClient.post).toHaveBeenCalledWith(
        '/api/learning/flashcards/item-uuid-456/review',
        payload
      );
      expect(result.learningStatus).toBe('Review');
      expect(result.repetitionCount).toBe(1);
    });

    it('calls legacy POST /api/learning/flashcards/review when dailyItemId is not provided', async () => {
      (httpClient.post as any).mockResolvedValueOnce({ data: { vocabularyId: 'voc-2' } });

      const payload = {
        vocabularyId: 'voc-2',
        rating: 'Easy' as const,
        clientRequestId: 'client-req-legacy',
      };

      await vocabularyApi.submitReview(payload);

      expect(httpClient.post).toHaveBeenCalledWith('/api/learning/flashcards/review', payload);
    });
  });
});
