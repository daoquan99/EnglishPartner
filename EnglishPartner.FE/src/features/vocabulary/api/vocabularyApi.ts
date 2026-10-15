import { httpClient } from '@/shared/api/httpClient';
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
  VocabularyFlashcardDto,
  VocabularyGenerationPromptDto,
} from '../types/vocabulary.types';

const FALLBACK_FLASHCARDS: VocabularyFlashcardDto[] = [
  {
    vocabularyId: '30000000-0000-0000-0000-000000000004',
    senseId: '30000000-0000-0000-0000-000000000105',
    word: 'scalable',
    partOfSpeech: 'adjective',
    cefrLevel: 'B2',
    definitionEn: 'Able to handle a growing amount of work, traffic, or users smoothly without losing performance.',
    meaningVi: 'Có khả năng mở rộng quy mô, chịu tải lớn',
    descriptionEn: 'Essential technical term in software architecture and system design interviews.',
    topicCategory: '.NET 10, Clean Architecture, DDD & CQRS',
    frequencyRank: 3100,
    pronunciations: [
      { id: 'p1', accent: 'UK', ipa: '/ˈskeɪ.lə.bəl/', syllableCount: 3, stressPattern: 'SCA-la-ble (Ooo)' },
      { id: 'p2', accent: 'US', ipa: '/ˈskeɪ.lə.bəl/', syllableCount: 3, stressPattern: 'SCA-la-ble (Ooo)' },
    ],
    synonyms: ['extensible', 'elastic', 'high-capacity'],
    antonyms: ['rigid', 'unscalable'],
    wordFamilies: [
      { id: 'wf1', wordForm: 'scalability', partOfSpeech: 'noun', note: 'System scalability' },
      { id: 'wf2', wordForm: 'scale', partOfSpeech: 'verb', note: 'Scale horizontally' },
    ],
    collocations: [
      { id: 'c1', phrase: 'highly scalable architecture', meaningVi: 'kiến trúc có khả năng mở rộng cao' },
      { id: 'c2', phrase: 'scalable microservice', meaningVi: 'vi dịch vụ dễ mở rộng' },
    ],
    examples: [
      {
        id: '30000000-0000-0000-0000-000000001006',
        sentenceEn: 'We designed a stateless API so the backend remains horizontally scalable under peak load.',
        translationVi: 'Chúng tôi thiết kế API phi trạng thái để backend có thể mở rộng theo chiều ngang khi tải tăng cao.',
        cefrLevel: 'B2',
      },
    ],
    grammarPatterns: [
      {
        id: 'gp1',
        pattern: 'scalable + noun (architecture / system / solution)',
        explanationVi: 'Dùng trước danh từ chỉ hệ thống hoặc giải pháp',
        sampleSentence: 'A scalable cloud solution.',
      },
    ],
    learningStatus: 'Learning',
    repetitionCount: 1,
    intervalDays: 1,
    easeFactor: 2.5,
    isCompletedToday: false,
  },
  {
    vocabularyId: '30000000-0000-0000-0000-000000000005',
    senseId: '30000000-0000-0000-0000-000000000106',
    word: 'bottleneck',
    partOfSpeech: 'noun',
    cefrLevel: 'B2',
    definitionEn: 'A point of congestion in a system or workflow that slows down overall performance.',
    meaningVi: 'Điểm nghẽn cổ chai (gây chậm hệ thống hoặc quy trình)',
    descriptionEn: 'Used constantly in SQL query tuning, backend profiling, and Agile retrospectives.',
    topicCategory: 'SQL Server Indexing, Query Tuning & Deadlocks',
    frequencyRank: 3400,
    pronunciations: [
      { id: 'p3', accent: 'UK', ipa: '/ˈbɒt.əl.nek/', syllableCount: 3, stressPattern: 'BOT-tle-neck (Ooo)' },
      { id: 'p4', accent: 'US', ipa: '/ˈbɑː.t̬əl.nek/', syllableCount: 3, stressPattern: 'BOT-tle-neck (Ooo)' },
    ],
    synonyms: ['constraint', 'chokepoint', 'impediment'],
    antonyms: ['accelerator', 'catalyst'],
    wordFamilies: [],
    collocations: [
      { id: 'c3', phrase: 'identify a performance bottleneck', meaningVi: 'xác định điểm nghẽn hiệu năng' },
      { id: 'c4', phrase: 'eliminate the database bottleneck', meaningVi: 'loại bỏ điểm nghẽn cơ sở dữ liệu' },
    ],
    examples: [
      {
        id: '30000000-0000-0000-0000-000000001007',
        sentenceEn: 'Missing indexes on the Orders table became the main bottleneck during flash sales.',
        translationVi: 'Việc thiếu index trên bảng Orders đã trở thành điểm nghẽn chính trong đợt mở bán chớp nhoáng.',
        cefrLevel: 'B2',
      },
    ],
    grammarPatterns: [
      {
        id: 'gp2',
        pattern: 'bottleneck + in + noun / verb-ing',
        explanationVi: 'Điểm nghẽn trong khâu nào đó',
        sampleSentence: 'I/O wait was the biggest bottleneck in processing large files.',
      },
    ],
    learningStatus: 'Learning',
    repetitionCount: 0,
    intervalDays: 0,
    easeFactor: 2.5,
    isCompletedToday: false,
  },
  {
    vocabularyId: '30000000-0000-0000-0000-000000000006',
    senseId: '30000000-0000-0000-0000-000000000107',
    word: 'resilient',
    partOfSpeech: 'adjective',
    cefrLevel: 'B2',
    definitionEn: 'Able to recover quickly from difficult conditions, setbacks, or system failures.',
    meaningVi: 'Kiên cường, bền bỉ; có khả năng tự phục hồi lỗi',
    descriptionEn: 'Great word for both HR behavioral interviews and System Design interviews.',
    topicCategory: 'Designing a High-Concurrency Payment & Order System',
    frequencyRank: 2650,
    pronunciations: [
      { id: 'p5', accent: 'UK', ipa: '/rɪˈzɪl.i.ənt/', syllableCount: 4, stressPattern: 're-SIL-i-ent (oOoo)' },
      { id: 'p6', accent: 'US', ipa: '/rɪˈzɪl.jənt/', syllableCount: 3, stressPattern: 're-SIL-ient (oOo)' },
    ],
    synonyms: ['adaptable', 'fault-tolerant'],
    antonyms: ['fragile'],
    wordFamilies: [
      { id: 'wf3', wordForm: 'resilience', partOfSpeech: 'noun' },
      { id: 'wf4', wordForm: 'resiliently', partOfSpeech: 'adverb' },
    ],
    collocations: [
      { id: 'c5', phrase: 'resilient to failure', meaningVi: 'có khả năng chống chịu lỗi' },
      { id: 'c6', phrase: 'stay resilient under pressure', meaningVi: 'giữ vững sự kiên cường dưới áp lực' },
    ],
    examples: [
      {
        id: '30000000-0000-0000-0000-000000001008',
        sentenceEn: 'Adding automatic retry policies made our payment service much more resilient to network timeouts.',
        translationVi: 'Việc bổ sung cơ chế tự động thử lại đã giúp dịch vụ thanh toán của chúng tôi chịu lỗi mất mạng tốt hơn nhiều.',
        cefrLevel: 'B2',
      },
    ],
    grammarPatterns: [
      {
        id: 'gp3',
        pattern: 'be resilient to + noun',
        explanationVi: 'Chống chịu tốt trước điều gì',
        sampleSentence: 'The system is resilient to sudden traffic spikes.',
      },
    ],
    learningStatus: 'New',
    repetitionCount: 0,
    intervalDays: 0,
    easeFactor: 2.5,
    isCompletedToday: false,
  },
];

export const FALLBACK_VOCABULARY_CARDS = FALLBACK_FLASHCARDS;

export const vocabularyApi = {
  async synthesizePronunciationAudio(
    payload: SynthesizePronunciationAudioRequestDto
  ): Promise<SynthesizePronunciationAudioResponseDto> {
    const { data } = await httpClient.post<SynthesizePronunciationAudioResponseDto>(
      '/api/pronunciation/synthesize',
      payload
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
  async getTodayPlan(targetWordCount = 10): Promise<DailyVocabularyPlanResponseDto> {
    try {
      const timeZoneId = Intl.DateTimeFormat().resolvedOptions().timeZone || 'Asia/Ho_Chi_Minh';
      const { data } = await httpClient.get<any>(
        '/api/learning/flashcards/today',
        { params: { timeZoneId, target: targetWordCount } }
      );
      const rawCards = data?.items || data?.flashcards || [];
      if (Array.isArray(rawCards) && rawCards.length > 0) {
        const items: VocabularyFlashcardDto[] = rawCards.map((c: any) => ({
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
          learningStatus: c.learningStatus || 'New',
          repetitionCount: c.repetitionCount || 0,
          intervalDays: c.intervalDays || 0,
          easeFactor: c.easeFactor || 2.5,
          nextReviewAtUtc: c.nextReviewAt,
          lastPronunciationScore: c.latestPronunciationScore,
          isCompletedToday: c.itemStatus === 'Completed',
        }));

        return {
          planId: data.planId,
          userId: data.userId,
          planDate: data.learningDate || new Date().toISOString().slice(0, 10),
          timeZoneId: data.timeZoneId || timeZoneId,
          targetWordCount: data.newVocabularyTarget || targetWordCount,
          completedWordCount: data.completedCount || 0,
          dueReviewCount: data.pendingReviewCount || 0,
          newWordCount: data.newCount || 0,
          items,
        };
      }
    } catch {
      // Fallback when offline
    }
    return {
      planId: 'fallback-plan-today',
      userId: 'default-learner',
      planDate: new Date().toISOString().slice(0, 10),
      timeZoneId: 'Asia/Ho_Chi_Minh',
      targetWordCount,
      completedWordCount: FALLBACK_FLASHCARDS.filter((x) => x.isCompletedToday).length,
      dueReviewCount: 1,
      newWordCount: FALLBACK_FLASHCARDS.length,
      items: FALLBACK_FLASHCARDS,
    };
  },

  async submitReview(payload: SubmitVocabularyReviewRequestDto): Promise<VocabularyFlashcardDto> {
    const { data } = await httpClient.post<VocabularyFlashcardDto>(
      '/api/learning/flashcards/review',
      payload
    );
    return data;
  },

  async generateVocabularyWithAi(
    payload: GenerateVocabularyRequestDto
  ): Promise<VocabularyFlashcardDto[]> {
    const { data } = await httpClient.post<VocabularyFlashcardDto[]>(
      '/api/vocabularies/generate',
      payload,
      // Allow one minute beyond the backend's 3000-second generation timeout.
      { timeout: 3_060_000 }
    );
    return data;
  },

  async buildGenerationPrompt(
    payload: BuildVocabularyGenerationPromptRequestDto
  ): Promise<VocabularyGenerationPromptDto> {
    const { data } = await httpClient.post<VocabularyGenerationPromptDto>(
      '/api/vocabularies/generation/prompt',
      payload
    );
    return data;
  },

  async importVocabularyJson(
    payload: ImportVocabularyJsonRequestDto
  ): Promise<VocabularyFlashcardDto[]> {
    const { data } = await httpClient.post<VocabularyFlashcardDto[]>(
      '/api/vocabularies/generation/import',
      payload
    );
    return data;
  },

  async assessPronunciation(payload: {
    apiKey: string;
    userId?: string;
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
      request
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
