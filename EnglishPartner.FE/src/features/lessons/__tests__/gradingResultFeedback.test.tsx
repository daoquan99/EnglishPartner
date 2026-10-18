import React from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { describe, expect, it } from 'vitest';
import { GradingResultFeedback } from '../components/player/GradingResultFeedback';
import { attachPersistedFeedback } from '../components/player/gradingFeedback';
import { ExerciseType, type ExerciseAnswerResultDto, type LessonAttemptResumeDto, type SubmittedAnswerSummaryDto } from '../types/lessons.types';

const summary = (overrides: Partial<SubmittedAnswerSummaryDto> = {}): SubmittedAnswerSummaryDto => ({
  answerId: 'answer-1', status: 'Completed', score100: 100, pointsEarned: 1, maxPoints: 1, ...overrides,
});
const renderSpelling = (result: SubmittedAnswerSummaryDto) => renderToStaticMarkup(
  <GradingResultFeedback result={result} exerciseType={ExerciseType.SpellingPractice} gradingMode="Deterministic" />
);

describe('Grading feedback distinguishes completion from correctness', () => {
  it('shows a correct spelling result with explicit wording and a green result border', () => {
    const html = renderSpelling(summary({ feedback: 'Correct spelling!' }));
    expect(html).toContain('Chính xác!');
    expect(html).toContain('Bạn đã viết đúng chính tả.');
    expect(html).toContain('border-emerald-300');
    expect(html).not.toContain('Đã hoàn thành &amp; Chấm điểm');
  });

  it('shows an incorrect result and the server-provided correction after submission', () => {
    const html = renderSpelling(summary({ score100: 0, pointsEarned: 0, feedback: 'Expected spelling: schedule.' }));
    expect(html).toContain('Chưa đúng');
    expect(html).toContain('Đáp án đúng:');
    expect(html).toContain('schedule');
    expect(html).toContain('border-rose-300');
    expect(html).toContain('(0 đ)');
    expect(html).not.toContain('Chính xác!');
  });

  it('distinguishes partial deterministic credit from a fully correct answer', () => {
    const html = renderToStaticMarkup(<GradingResultFeedback
      result={summary({ score100: 50, pointsEarned: 1, maxPoints: 2 })}
      exerciseType={ExerciseType.FillInTheBlanks} gradingMode="Deterministic" />);
    expect(html).toContain('Đúng một phần');
    expect(html).toContain('border-amber-300');
  });

  it('does not turn a completed result with null scores into an incorrect result', () => {
    const html = renderSpelling(summary({ score100: null, pointsEarned: null }));
    expect(html).toContain('Đã chấm điểm');
    expect(html).not.toContain('Chưa đúng');
    expect(html).not.toContain('Chính xác!');
  });

  it.each(['Pending', 'Grading', 'Failed', 'NotAssessable'])('does not announce correctness or reveal a correction for %s', (status) => {
    const html = renderSpelling(summary({ status, score100: null, pointsEarned: null, feedback: null }));
    expect(html).not.toContain('Chính xác!');
    expect(html).not.toContain('Chưa đúng');
    expect(html).not.toContain('Đáp án đúng:');
  });

  it('does not interpret an AI score as a binary correct or incorrect answer', () => {
    const html = renderToStaticMarkup(<GradingResultFeedback
      result={summary({ score100: 80, pointsEarned: 4, maxPoints: 5 })}
      exerciseType={ExerciseType.WritingPractice} gradingMode="AiRubric" />);
    expect(html).toContain('Đã chấm điểm');
    expect(html).not.toContain('Chính xác!');
  });

  it('keeps self-assessment separate from scored correctness', () => {
    const html = renderToStaticMarkup(<GradingResultFeedback
      result={summary({ score100: 100, pointsEarned: 0, maxPoints: 0 })}
      exerciseType={ExerciseType.Flashcards} gradingMode="SelfAssessment" />);
    expect(html).toContain('Đã ghi nhận tự đánh giá');
    expect(html).not.toContain('Chính xác!');
  });
});

describe('Immediate submission results retain persisted spelling feedback', () => {
  const progress = { completedExercisesCount: 1, totalExercisesCount: 1, pendingGradingCount: 0,
    failedGradingCount: 0, currentSubtotalPoints: 0, totalMaxPoints: 1, finalScore100: 0 };
  const result: ExerciseAnswerResultDto = { answerId: 'answer-1', attemptId: 'attempt-1', exerciseId: 'exercise-1',
    attemptRevision: 2, status: 'Completed', gradingMode: 'Deterministic', score100: 0, pointsEarned: 0,
    maxPoints: 1, attemptProgress: progress };
  const attempt: LessonAttemptResumeDto = { attemptId: 'attempt-1', lessonId: 'lesson-1', lessonTitle: 'Spelling',
    pinnedLessonVersion: 1, attemptRevision: 2, status: 'InProgress', startedAt: '2026-10-18T10:00:00Z',
    progress, exercises: [], submittedAnswers: {
      'exercise-1': summary({ score100: 0, pointsEarned: 0, feedback: 'Expected spelling: schedule.' }),
    } };

  it('attaches the explanation available only in resume to the immediate submit result', () => {
    const hydrated = attachPersistedFeedback(result, attempt);
    expect(hydrated.feedback).toBe('Expected spelling: schedule.');
    expect(renderSpelling(hydrated)).toContain('Đáp án đúng:');
    expect(result.feedback).toBeUndefined();
  });

  it('does not attach stale or unrelated attempt feedback', () => {
    expect(attachPersistedFeedback(result, { ...attempt, attemptRevision: 1 })).toBe(result);
    expect(attachPersistedFeedback(result, { ...attempt, attemptId: 'another-attempt' })).toBe(result);
    expect(attachPersistedFeedback(result, { ...attempt, submittedAnswers: { 'exercise-1': summary({ answerId: 'another-answer' }) } })).toBe(result);
  });
});
