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

export interface VocabularyFlashcardDto {
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

export interface SubmitVocabularyReviewRequestDto {
  userId?: string;
  vocabularyId: string;
  senseId: string;
  rating: ReviewRating;
  responseTimeMs?: number;
}

export interface CriterionScoreDto {
  criterionCode: string;
  criterionName: string;
  weight: number;
  score: number;
  weightedScore: number;
  feedbackVi: string;
}

export interface PronunciationAssessmentResultDto {
  assessmentId: string;
  overallScore: number;
  cefrPronunciationGrade: string;
  recognizedTranscript: string;
  referenceText: string;
  modelCodeUsed: string;
  rubricCode: string;
  phonemeAndStressFeedbackVi: string;
  coachingSuggestionVi: string;
  criterionScores: CriterionScoreDto[];
  assessedAtUtc: string;
}
