import { toast } from '@/shared/lib/toast';
import { httpClient, extractApiErrorMessage } from '@/shared/api/httpClient';
import { AI_REQUEST_TIMEOUT_MS } from '@/shared/config/timeouts';
import type {
  SynthesizePronunciationAudioRequestDto,
  SynthesizePronunciationAudioResponseDto,
} from '@/shared/api/openapi.types';
import type {
  BuildVocabularyGenerationPromptRequestDto,
  DailyVocabularyPlanResponseDto,
  GenerateVocabularyRequestDto,
  ImportVocabularyJsonRequestDto,
  PronunciationAssessmentApiResultDto,
  PronunciationAssessmentResultDto,
  SubmitPronunciationAssessmentRequestDto,
  SubmitVocabularyReviewRequestDto,
  UpdateLearningSettingsRequestDto,
  UserLearningSettingsDto,
  VocabularyFlashcardDto,
  VocabularyGenerationPromptDto,
} from '../types/vocabulary.types';

export function mapPlanResponse(data: any): DailyVocabularyPlanResponseDto {
  const rawCards = data?.items || data?.flashcards || [];
  const items: VocabularyFlashcardDto[] = Array.isArray(rawCards)
    ? rawCards.map((c: any) => ({
        dailyItemId: c.dailyItemId,
        vocabularyId: c.vocabularyId,
        senseId: c.senseId,
        word: c.word,
        partOfSpeech: c.partOfSpeech,
        cefrLevel: c.cefrLevel,
        definitionEn: c.definitionEn,
        meaningVi: c.meaningVi,
        descriptionEn: c.descriptionEn ?? '',
        topicCategory: c.topicCategory,
        frequencyRank: c.frequencyRank,
        pronunciations: c.pronunciations || [],
        synonyms: c.synonyms || [],
        antonyms: c.antonyms || [],
        wordFamilies: (c.wordFamilies || []).map((wf: any) => ({
          id: wf.id,
          wordForm: wf.relatedWord || wf.wordForm,
          partOfSpeech: wf.partOfSpeech,
          note: wf.note ?? '',
        })),
        collocations: (c.collocations || []).map((col: any) => ({
          id: col.id,
          phrase: col.phraseEn || col.phrase,
          meaningVi: col.meaningVi,
          exampleSentence: col.exampleSentence,
        })),
        examples: (c.examples || []).map((ex: any) => ({
          id: ex.id,
          sentenceEn: ex.sentenceEn,
          translationVi: ex.sentenceVi || ex.translationVi || '',
          cefrLevel: ex.cefrLevel || c.cefrLevel,
        })),
        grammarPatterns: (c.grammarPatterns || []).map((gp: any) => ({
          id: gp.id,
          pattern: gp.pattern,
          explanationVi: gp.explanation || gp.explanationVi || '',
          sampleSentence: gp.example || gp.sampleSentence || '',
        })),
        itemType: ((): 'New' | 'Review' | 'CarryOver' => {
          const raw = String(c.itemType || '').toLowerCase();
          if (raw === 'carryover' || raw === 'carry_over') return 'CarryOver';
          if (raw === 'review') return 'Review';
          return 'New';
        })(),
        itemStatus: c.itemStatus,
        learningStatus: c.learningStatus || 'New',
        repetitionCount: c.repetitionCount || 0,
        intervalDays: c.intervalDays || 0,
        easeFactor: c.easeFactor || 2.5,
        nextReviewAtUtc: c.nextReviewAt,
        lastPronunciationScore: c.latestPronunciationScore,
        isCompletedToday: c.itemStatus === 'Completed',
      }))
    : [];

  const newWordCount = data.newCount ?? 0;
  const carryOverCount = data.carryOverCount ?? 0;
  const targetWordCount = data.newVocabularyTarget ?? 20;
  const firstLearningAssigned = data.firstLearningAssignedCount ?? (newWordCount + carryOverCount);

  return {
    planId: data.planId,
    userId: data.userId,
    planDate: data.learningDate || new Date().toISOString().slice(0, 10),
    timeZoneId: data.timeZoneId || 'Asia/Ho_Chi_Minh',
    targetWordCount,
    completedWordCount: data.completedCount ?? 0,
    dueReviewCount: data.pendingReviewCount ?? 0,
    newWordCount,
    targetCefrLevel: data.targetCefrLevel,
    firstLearningAssignedCount: firstLearningAssigned,
    firstLearningCompletedCount: data.firstLearningCompletedCount ?? 0,
    firstLearningRemainingCount: data.firstLearningRemainingCount ?? 0,
    remainingReviewCount: data.remainingReviewCount ?? (data.pendingReviewCount ?? 0),
    shortfallCount: data.shortfallCount ?? Math.max(0, targetWordCount - firstLearningAssigned),
    overflowCount: data.overflowCount ?? Math.max(0, firstLearningAssigned - targetWordCount),
    carryOverCount,
    isLegacyPlan: !!data.isLegacyPlan,
    items,
  };
}

export const vocabularyApi = {
  async synthesizePronunciationAudio(
    payload: SynthesizePronunciationAudioRequestDto
  ): Promise<SynthesizePronunciationAudioResponseDto> {
    const { data } = await httpClient.post<SynthesizePronunciationAudioResponseDto>(
      '/api/pronunciation/synthesize',
      payload,
      { timeout: AI_REQUEST_TIMEOUT_MS }
    );
    return data;
  },

  async getPronunciationAudio(params: {
    targetType: 'word' | 'sentence';
    targetId: string;
    voiceName?: string;
    speechSpeed?: number;
    accent?: 'US' | 'UK';
    modelCode?: string;
    vocabularyId?: string;
    senseId?: string;
  }): Promise<SynthesizePronunciationAudioResponseDto> {
    const { data } = await httpClient.get<SynthesizePronunciationAudioResponseDto>(
      '/api/pronunciation/audio',
      { params }
    );
    return data;
  },

  async getLearningSettings(): Promise<UserLearningSettingsDto> {
    const { data } = await httpClient.get<UserLearningSettingsDto>('/api/learning/settings');
    return data;
  },

  async updateLearningSettings(
    payload: UpdateLearningSettingsRequestDto
  ): Promise<UserLearningSettingsDto> {
    const { data } = await httpClient.put<UserLearningSettingsDto>(
      '/api/learning/settings',
      payload
    );
    return data;
  },

  async getTodayPlan(timeZoneId?: string): Promise<DailyVocabularyPlanResponseDto> {
    const params: Record<string, string> = {};
    if (timeZoneId) {
      params.timeZoneId = timeZoneId;
    }
    try {
      const { data } = await httpClient.get<any>(
        '/api/learning/flashcards/today',
        { params, skipErrorToast: true } as any
      );
      return mapPlanResponse(data);
    } catch (err: any) {
      const isDailyNotInitialized =
        err?.response?.status === 404 &&
        (err?.response?.data?.code === 'daily_plan_not_initialized' ||
          err?.response?.data?.error === 'daily_plan_not_initialized');

      if (!isDailyNotInitialized) {
        const errorMsg = extractApiErrorMessage(err);
        const status = err?.response?.status;
        const title = status ? `Lỗi API (${status})` : 'Lỗi kết nối';
        const toastKey = `api-err-${status ?? 'net'}-${errorMsg.slice(0, 50)}`;
        toast.error(errorMsg, {
          id: toastKey,
          title,
        });
      }

      throw err;
    }
  },

  async initializeTodayPlan(): Promise<DailyVocabularyPlanResponseDto> {
    const { data } = await httpClient.post<any>(
      '/api/learning/flashcards/today/initialize',
      {}
    );
    return mapPlanResponse(data);
  },

  async submitReview(
    payload: SubmitVocabularyReviewRequestDto,
    dailyItemId?: string
  ): Promise<VocabularyFlashcardDto> {
    const url = dailyItemId
      ? `/api/learning/flashcards/${dailyItemId}/review`
      : '/api/learning/flashcards/review';
    const { data } = await httpClient.post<VocabularyFlashcardDto>(url, payload);
    return data;
  },

  async generateVocabularyWithAi(
    payload: GenerateVocabularyRequestDto,
    options?: { skipErrorToast?: boolean }
  ): Promise<VocabularyFlashcardDto[]> {
    const { data } = await httpClient.post<VocabularyFlashcardDto[]>(
      '/api/vocabularies/generate',
      payload,
      { timeout: AI_REQUEST_TIMEOUT_MS, skipErrorToast: options?.skipErrorToast } as any
    );
    return data;
  },

  async buildGenerationPrompt(
    payload: BuildVocabularyGenerationPromptRequestDto,
    options?: { skipErrorToast?: boolean }
  ): Promise<VocabularyGenerationPromptDto> {
    const config = options?.skipErrorToast !== undefined ? { skipErrorToast: options.skipErrorToast } : undefined;
    const { data } = config
      ? await httpClient.post<VocabularyGenerationPromptDto>(
          '/api/vocabularies/generation/prompt',
          payload,
          config as any
        )
      : await httpClient.post<VocabularyGenerationPromptDto>(
          '/api/vocabularies/generation/prompt',
          payload
        );
    return data;
  },

  async importVocabularyJson(
    payload: ImportVocabularyJsonRequestDto,
    options?: { skipErrorToast?: boolean }
  ): Promise<VocabularyFlashcardDto[]> {
    const config = options?.skipErrorToast !== undefined ? { skipErrorToast: options.skipErrorToast } : undefined;
    const { data } = config
      ? await httpClient.post<VocabularyFlashcardDto[]>(
          '/api/vocabularies/generation/import',
          payload,
          config as any
        )
      : await httpClient.post<VocabularyFlashcardDto[]>(
          '/api/vocabularies/generation/import',
          payload
        );
    return data;
  },

  async assessPronunciation(payload: {
    apiKey: string;
    vocabularyId: string;
    senseId: string;
    exampleId?: string;
    referenceText: string;
    audioBase64: string;
    mimeType: string;
    durationMs: number;
    modelCode: string;
    accent?: 'US' | 'UK';
  }): Promise<PronunciationAssessmentResultDto> {
    const request: SubmitPronunciationAssessmentRequestDto = {
      apiKey: payload.apiKey,
      vocabularyId: payload.vocabularyId,
      senseId: payload.senseId,
      targetType: payload.exampleId ? 'Sentence' : 'Word',
      targetId: payload.exampleId,
      referenceText: payload.referenceText,
      accent: payload.accent ?? 'US',
      assessmentModelCode: payload.modelCode,
      base64Audio: payload.audioBase64,
      contentType: payload.mimeType,
      durationMilliseconds: payload.durationMs,
    };
    const { data } = await httpClient.post<PronunciationAssessmentApiResultDto>(
      '/api/pronunciation/assessments',
      request,
      { timeout: AI_REQUEST_TIMEOUT_MS }
    );
    return {
      assessmentId: data.assessmentId,
      vocabularyId: data.vocabularyId ?? payload.vocabularyId,
      senseId: data.senseId ?? payload.senseId,
      targetType: data.targetType ?? (payload.exampleId ? 'Sentence' : 'Word'),
      targetId: data.targetId ?? payload.exampleId,
      accent: data.accent ?? payload.accent ?? 'US',
      overallScore: data.overallScore,
      isAssessable: data.isAssessable,
      status: data.status,
      recognizedTranscript: data.recognizedText ?? '',
      referenceText: data.expectedText,
      modelCodeUsed: data.modelCode,
      rubricCode: data.rubricCode,
      phonemeAndStressFeedbackVi: [...data.strengths, ...data.improvements].join('\n'),
      coachingSuggestionVi: data.suggestedPractice.join('\n'),
      criterionScores: data.criteria.map((criterion) => ({
        criterionCode: criterion.code,
        criterionName: criterion.name,
        weight: criterion.weight,
        score: criterion.score,
        weightedScore: criterion.weightedScore,
        feedbackVi: criterion.feedback,
      })),
      assessedAtUtc: data.createdAt,
    };
  },
};
