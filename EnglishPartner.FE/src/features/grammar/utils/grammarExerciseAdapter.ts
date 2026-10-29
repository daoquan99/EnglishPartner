import { ExerciseType, type ExerciseLearnerDto, type ExerciseAnswerResultDto } from '@/features/lessons/types/lessons.types';
import type {
  GrammarExerciseLearnerDto,
  GrammarAttemptAnswerDto,
  GrammarAnswerResultDto,
} from '../types/grammar.types';

export function mapGrammarExerciseToLearnerDto(exercise: GrammarExerciseLearnerDto): ExerciseLearnerDto {
  return {
    id: exercise.id,
    key: exercise.key || exercise.id,
    type: exercise.type as ExerciseType,
    title: exercise.title || '',
    instructions: exercise.instructions || '',
    order: exercise.order,
    maxPoints: exercise.maxPoints,
    gradingMode: exercise.gradingMode as any,
    content: (exercise.content as Record<string, unknown>) || {},
  };
}

export function mapGrammarAnswerToExerciseResult(
  answer: GrammarAttemptAnswerDto | GrammarAnswerResultDto,
  exercise?: GrammarExerciseLearnerDto
): ExerciseAnswerResultDto {
  let feedbackStr: string | null = null;
  let parsedFeedback: any = null;

  if (answer.feedback) {
    if (typeof answer.feedback === 'string') {
      try {
        parsedFeedback = JSON.parse(answer.feedback);
      } catch {
        parsedFeedback = null;
        feedbackStr = answer.feedback;
      }
    } else {
      parsedFeedback = answer.feedback;
    }

    if (parsedFeedback?.explanation && typeof parsedFeedback.explanation === 'string') {
      feedbackStr = parsedFeedback.explanation;
    } else if (parsedFeedback && typeof parsedFeedback === 'object' && !Array.isArray(parsedFeedback)) {
      if (typeof parsedFeedback.feedback === 'string') {
        feedbackStr = parsedFeedback.feedback;
      }
    } else if (typeof answer.feedback === 'string') {
      feedbackStr = answer.feedback;
    }
  }

  const suggestedAnswer =
    ('suggestedAnswer' in answer && answer.suggestedAnswer) ||
    parsedFeedback?.suggestedAnswer ||
    null;

  const criteriaBreakdown =
    parsedFeedback?.criteria ||
    (Array.isArray(parsedFeedback) ? parsedFeedback : null);

  const strengths = parsedFeedback?.strengths || null;
  const improvements = parsedFeedback?.improvements || null;

  const status = (answer.status || 'Completed') as any;

  return {
    answerId: ('attemptId' in answer ? `${answer.attemptId}_${answer.exerciseId}` : answer.exerciseId),
    attemptId: ('attemptId' in answer ? answer.attemptId : ''),
    attemptRevision: 1,
    exerciseId: answer.exerciseId,
    status,
    gradingMode: (exercise?.gradingMode || 'Deterministic') as any,
    score100: answer.score100 ?? null,
    pointsEarned: answer.pointsEarned ?? null,
    maxPoints: answer.maxPoints,
    feedback: feedbackStr,
    suggestedAnswer,
    criteriaBreakdown,
    strengths,
    improvements,
    answerPayload: (answer.answerPayload as Record<string, unknown>) || null,
    attemptProgress: {
      completedExercisesCount: 0,
      totalExercisesCount: 0,
      pendingGradingCount: 0,
      failedGradingCount: 0,
      currentSubtotalPoints: 0,
      totalMaxPoints: answer.maxPoints,
      finalScore100: answer.score100 ?? null,
    },
  };
}
