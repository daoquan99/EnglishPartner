import type { CefrLevel } from '@/shared/api/openapi.types';

export type ReviewRating = 'Again' | 'Hard' | 'Good' | 'Easy';

export interface VocabularyPronunciationDto {
  id: string;
  accent: 'UK' | 'US' | string;
  ipa: string;
  audioUrl?: string | null;
  syllableCount?: number | null;
  stressPattern?: string | null;
}

export interface VocabularyWordFamilyDto {
  id: string;
  wordForm: string;
  partOfSpeech: string;
  note?: string | null;
}

export interface VocabularyCollocationDto {
  id: string;
  phrase: string;
  meaningVi: string;
  exampleSentence?: string | null;
}

export interface VocabularyExampleDto {
  id: string;
  sentenceEn: string;
  translationVi: string;
  cefrLevel: CefrLevel;
}

export interface VocabularyGrammarPatternDto {
  id: string;
  pattern: string;
  explanationVi: string;
  sampleSentence?: string | null;
}

export interface UserLearningSettingsDto {
  dailyVocabularyTarget: number;
  cefrLevel: CefrLevel;
  isPersisted: boolean;
  effectiveFromLearningDate: string;
  timeZoneId: string;
}

export interface UpdateLearningSettingsRequestDto {
  dailyVocabularyTarget: number;
  cefrLevel: CefrLevel;
}

export interface VocabularyFlashcardDto {
  dailyItemId?: string;
  vocabularyId: string;
  senseId: string;
  word: string;
  partOfSpeech: string;
  cefrLevel: CefrLevel;
  definitionEn: string;
  meaningVi: string;
  descriptionEn: string;
  topicCategory?: string | null;
  frequencyRank?: number | null;
  pronunciations: VocabularyPronunciationDto[];
  synonyms: string[];
  antonyms: string[];
  wordFamilies: VocabularyWordFamilyDto[];
  collocations: VocabularyCollocationDto[];
  examples: VocabularyExampleDto[];
  grammarPatterns: VocabularyGrammarPatternDto[];
  itemType?: 'New' | 'Review' | 'CarryOver';
  itemStatus?: 'Pending' | 'InProgress' | 'Completed';
  learningStatus: string;
  repetitionCount: number;
  intervalDays: number;
  easeFactor: number;
  nextReviewAtUtc?: string | null;
  lastPronunciationScore?: number | null;
  isCompletedToday: boolean;
}

export interface DailyVocabularyPlanResponseDto {
  planId: string;
  userId: string;
  planDate: string;
  timeZoneId: string;
  targetWordCount: number;
  completedWordCount: number;
  dueReviewCount: number;
  newWordCount: number;
  targetCefrLevel?: CefrLevel | string;
  firstLearningAssignedCount: number;
  firstLearningCompletedCount: number;
  firstLearningRemainingCount: number;
  remainingReviewCount: number;
  shortfallCount: number;
  overflowCount: number;
  carryOverCount: number;
  isLegacyPlan?: boolean;
  items: VocabularyFlashcardDto[];
}

export interface GenerateVocabularyRequestDto {
  apiKey: string;
  cefrLevel: CefrLevel;
  topic: string;
  count: number;
  modelCode: string;
  userId?: string;
}

export interface BuildVocabularyGenerationPromptRequestDto {
  requestedCount: number;
  cefrLevel: CefrLevel;
  topic?: string | null;
  excludedWords?: string[];
}

export interface VocabularyGenerationPromptDto {
  prompt: string;
  promptVersion: string;
  requestedCount: number;
  cefrLevel: CefrLevel;
  topic: string;
  excludedWords: string[];
}

export interface ImportVocabularyJsonRequestDto {
  jsonText: string;
  modelCode?: string;
  cefrLevel: CefrLevel;
  topic?: string | null;
  assignToTodayPlan?: boolean;
}

export interface SubmitVocabularyReviewRequestDto {
  userId?: string;
  vocabularyId?: string;
  senseId?: string;
  rating: ReviewRating;
  responseTimeMs?: number;
  timeZoneId?: string;
  clientRequestId?: string;
}

export interface CriterionScoreDto {
  criterionCode: string;
  criterionName: string;
  weight: number;
  score: number;
  weightedScore: number;
  feedbackVi: string;
}

export interface SubmitPronunciationAssessmentRequestDto {
  apiKey: string;
  vocabularyId: string;
  senseId: string;
  targetType: 'Word' | 'Sentence';
  targetId?: string;
  referenceText: string;
  accent: 'US' | 'UK';
  assessmentModelCode: string;
  base64Audio: string;
  contentType: string;
  durationMilliseconds: number;
}

export interface PronunciationAssessmentApiResultDto {
  assessmentId: string;
  vocabularyId?: string | null;
  senseId?: string | null;
  targetType?: string | null;
  targetId?: string | null;
  accent?: string | null;
  overallScore: number | null;
  expectedText: string;
  recognizedText: string | null;
  modelCode: string;
  rubricCode: string;
  rubricVersion?: number | null;
  confidence?: number | null;
  isAssessable: boolean;
  status: string;
  criteria: {
    code: string;
    name: string;
    weight: number;
    score: number;
    weightedScore: number;
    feedback: string;
  }[];
  strengths: string[];
  improvements: string[];
  suggestedPractice: string[];
  createdAt: string;
}

export interface PronunciationAssessmentResultDto {
  assessmentId: string;
  vocabularyId?: string | null;
  senseId?: string | null;
  targetType?: string | null;
  targetId?: string | null;
  accent?: string | null;
  overallScore: number | null;
  isAssessable: boolean;
  status: string;
  recognizedTranscript: string;
  referenceText: string;
  modelCodeUsed: string;
  rubricCode: string;
  phonemeAndStressFeedbackVi: string;
  coachingSuggestionVi: string;
  criterionScores: CriterionScoreDto[];
  assessedAtUtc: string;
}

export interface PronunciationHistoryItemDto {
  assessmentId: string;
  vocabularyId?: string | null;
  senseId?: string | null;
  targetType: 'Word' | 'Sentence' | string;
  targetId?: string | null;
  expectedText: string;
  recognizedText: string | null;
  accent: string;
  modelCode: string;
  rubricCode: string;
  rubricVersion?: number | null;
  overallScore: number | null;
  confidence?: number | null;
  isAssessable: boolean;
  status: string;
  criteria: {
    code: string;
    name: string;
    weight: number;
    score: number;
    weightedScore: number;
    feedback: string;
  }[];
  strengths: string[];
  improvements: string[];
  suggestedPractice: string[];
  createdAt: string;
}

export interface FlashcardReviewHistoryItemDto {
  id: string;
  vocabularyId: string;
  senseId: string;
  rating: ReviewRating;
  reviewType: string;
  reviewedAtUtc: string;
  nextReviewAtUtc?: string | null;
}

