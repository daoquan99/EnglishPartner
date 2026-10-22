import { ExerciseType, type ExerciseAnswerResultDto, type LessonAttemptResumeDto, type SubmittedAnswerSummaryDto } from '../../types/lessons.types';

type GradingResult = ExerciseAnswerResultDto | SubmittedAnswerSummaryDto;

export function getCompletionFeedback(result: GradingResult, exerciseType?: ExerciseType, gradingMode?: string) {
  const mode = gradingMode ?? ('gradingMode' in result ? result.gradingMode : undefined);
  if (exerciseType === ExerciseType.Flashcards || mode === 'SelfAssessment') {
    return { label: 'Đã ghi nhận tự đánh giá', tone: 'neutral' as const };
  }
  const deterministic = mode === 'Deterministic' || exerciseType === ExerciseType.SpellingPractice;
  const score = typeof result.score100 === 'number' && Number.isFinite(result.score100)
    ? result.score100
    : typeof result.pointsEarned === 'number' && Number.isFinite(result.pointsEarned) && result.maxPoints > 0
      ? result.pointsEarned / result.maxPoints * 100
      : null;
  if (deterministic && score !== null && score >= 0 && score <= 100) {
    if (score === 100) return { label: 'Chính xác!', tone: 'correct' as const };
    if (score === 0) return { label: 'Chưa đúng', tone: 'incorrect' as const };
    return { label: 'Đúng một phần', tone: 'partial' as const };
  }
  return { label: 'Đã chấm điểm', tone: 'neutral' as const };
}

// The existing deterministic spelling grader returns its correction in this feedback message.
// Only read it from a completed, persisted result, never from the learner question payload.
export function getSpellingCorrection(result: GradingResult): string | null {
  if (result.status !== 'Completed' || !result.feedback) return null;
  return /^Expected spelling:\s*(.+)\.$/s.exec(result.feedback.trim())?.[1] ?? null;
}

export function attachPersistedFeedback(result: ExerciseAnswerResultDto, attempt?: LessonAttemptResumeDto): ExerciseAnswerResultDto {
  if (!attempt || attempt.attemptId !== result.attemptId || attempt.attemptRevision < result.attemptRevision) return result;
  const key = result.subQuestionId ? `${result.exerciseId}_${result.subQuestionId}` : result.exerciseId;
  const saved = attempt.submittedAnswers[key] ?? attempt.submittedAnswers[result.exerciseId];
  if (!saved || (saved.answerId && saved.answerId !== result.answerId)) return result;
  return {
    ...result,
    answerPayload: (result.answerPayload ?? saved.answerPayload) as Record<string, unknown> | null,
    feedback: result.feedback ?? saved.feedback,
    criteriaBreakdown: result.criteriaBreakdown ?? saved.criteriaBreakdown,
    aiReview: result.aiReview ?? saved.aiReview,
  };
}
