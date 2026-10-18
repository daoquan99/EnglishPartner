import { describe, it, expect } from 'vitest';
import {
  ExerciseType,
  EXERCISE_TYPE_CATALOG,
} from '../types/lessons.types';

describe('Lesson Exercise Types & Catalog Specification', () => {
  it('defines exactly the 16 canonical exercise types in PascalCase', () => {
    const expectedTypes = [
      'Flashcards',
      'MultipleChoice',
      'FillInTheBlanks',
      'Matching',
      'SpellingPractice',
      'SentenceUnscramble',
      'ErrorCorrection',
      'Translation',
      'TrueFalse',
      'ReadingComprehension',
      'ListeningComprehension',
      'Dictation',
      'Shadowing',
      'SpeakingPractice',
      'WritingPractice',
      'ConversationRolePlay',
    ];

    const actualTypes = Object.values(ExerciseType);
    expect(actualTypes).toHaveLength(16);
    expect(actualTypes).toEqual(expect.arrayContaining(expectedTypes));
  });

  it('catalog provides complete metadata for all 16 exercise types', () => {
    Object.values(ExerciseType).forEach((type) => {
      const meta = EXERCISE_TYPE_CATALOG[type];
      expect(meta).toBeDefined();
      expect(meta.type).toBe(type);
      expect(meta.labelVi).toBeTruthy();
      expect(meta.labelEn).toBeTruthy();
      expect(['Learn', 'Practice', 'Skills']).toContain(meta.group);
      expect([
        'Deterministic',
        'SelfAssessment',
        'RubricAssessment',
        'None',
      ]).toContain(meta.defaultGradingMode);
      expect(typeof meta.requiresMedia).toBe('boolean');
    });
  });

  it('accurately specifies media requirements for audio-dependent exercises', () => {
    // Only Listening, Dictation, Shadowing require media by contract
    expect(EXERCISE_TYPE_CATALOG[ExerciseType.ListeningComprehension].requiresMedia).toBe(true);
    expect(EXERCISE_TYPE_CATALOG[ExerciseType.Dictation].requiresMedia).toBe(true);
    expect(EXERCISE_TYPE_CATALOG[ExerciseType.Shadowing].requiresMedia).toBe(true);

    expect(EXERCISE_TYPE_CATALOG[ExerciseType.Flashcards].requiresMedia).toBe(false);
    expect(EXERCISE_TYPE_CATALOG[ExerciseType.MultipleChoice].requiresMedia).toBe(false);
    expect(EXERCISE_TYPE_CATALOG[ExerciseType.WritingPractice].requiresMedia).toBe(false);
  });

  it('correctly maps default grading modes according to specification', () => {
    expect(EXERCISE_TYPE_CATALOG[ExerciseType.Flashcards].defaultGradingMode).toBe('SelfAssessment');
    expect(EXERCISE_TYPE_CATALOG[ExerciseType.MultipleChoice].defaultGradingMode).toBe('Deterministic');
    expect(EXERCISE_TYPE_CATALOG[ExerciseType.SentenceUnscramble].defaultGradingMode).toBe('Deterministic');
    expect(EXERCISE_TYPE_CATALOG[ExerciseType.Translation].defaultGradingMode).toBe('RubricAssessment');
    expect(EXERCISE_TYPE_CATALOG[ExerciseType.SpeakingPractice].defaultGradingMode).toBe('RubricAssessment');
    expect(EXERCISE_TYPE_CATALOG[ExerciseType.WritingPractice].defaultGradingMode).toBe('RubricAssessment');
    expect(EXERCISE_TYPE_CATALOG[ExerciseType.ConversationRolePlay].defaultGradingMode).toBe('RubricAssessment');
  });
});
