import {
  ExerciseType,
  type ExerciseAnswerResultDto,
  type ExerciseLearnerDto,
  type LessonAttemptResumeDto,
  type SubmittedAnswerSummaryDto,
} from '../../types/lessons.types';
import { normalizeQuestions } from './lessonContent';

export type UnifiedGradingResult = ExerciseAnswerResultDto | SubmittedAnswerSummaryDto;

export interface ExerciseNavStatus {
  state:
    | 'unsubmitted'
    | 'grading'
    | 'failed'
    | 'not_assessable'
    | 'correct'
    | 'incorrect'
    | 'partial'
    | 'self_assessed'
    | 'ai_evaluated'
    | 'completed_neutral';
  tone: 'neutral' | 'correct' | 'incorrect' | 'partial' | 'failed' | 'grading';
  label: string;
  iconType: 'number' | 'correct' | 'incorrect' | 'partial' | 'clock' | 'alert' | 'help' | 'sparkles' | 'check';
}

/**
 * Resolves the authoritative result for an exercise from local immediate submit results and persisted resume answers.
 * Merges missing fields (answerPayload, feedback, criteriaBreakdown) if both point to the same submission.
 */
export function resolveExerciseResult(
  exercise: ExerciseLearnerDto,
  lastResults: Record<string, ExerciseAnswerResultDto>,
  submittedAnswers?: Record<string, SubmittedAnswerSummaryDto>
): UnifiedGradingResult | null {
  const map = submittedAnswers ?? {};
  const last = lastResults[exercise.id];

  // 1. Find corresponding persisted summary (by root exerciseId, compound key, or exerciseId property)
  let persisted: SubmittedAnswerSummaryDto | undefined = map[exercise.id];
  if (!persisted) {
    const prefix = `${exercise.id}_`;
    const compoundKey = Object.keys(map).find((k) => k.startsWith(prefix));
    if (compoundKey) {
      persisted = map[compoundKey];
    } else {
      persisted = Object.values(map).find((a) => a.exerciseId === exercise.id);
    }
  }

  // 2. Reconcile immediate submit result with persisted summary
  if (last && persisted) {
    if (last.answerId === persisted.answerId) {
      return {
        ...last,
        answerPayload: (last.answerPayload ?? persisted.answerPayload) as Record<string, unknown> | null,
        feedback: last.feedback ?? persisted.feedback,
        criteriaBreakdown: last.criteriaBreakdown ?? persisted.criteriaBreakdown,
        aiReview: last.aiReview ?? persisted.aiReview,
      };
    }
    // If persisted is newer or different after a fresh refetch/retry, prefer persisted
    if (last.attemptRevision && persisted.status && last.status !== persisted.status) {
      return persisted;
    }
    return last;
  }

  return last ?? persisted ?? null;
}

/**
 * Derives normalized 0-100 score from score100 or pointsEarned/maxPoints.
 * Returns null if not deterministically measurable.
 */
export function getDeterministicScore(result: UnifiedGradingResult): number | null {
  if (typeof result.score100 === 'number' && Number.isFinite(result.score100)) {
    return Math.max(0, Math.min(100, result.score100));
  }
  if (
    typeof result.pointsEarned === 'number' &&
    Number.isFinite(result.pointsEarned) &&
    typeof result.maxPoints === 'number' &&
    result.maxPoints > 0
  ) {
    const calc = (result.pointsEarned / result.maxPoints) * 100;
    return Math.max(0, Math.min(100, Math.round(calc * 100) / 100));
  }
  return null;
}

/**
 * Determines whether a composite exercise (Reading / Listening) contains only closed-ended deterministic questions.
 */
function isClosedEndedComposite(content: unknown): boolean {
  if (!content || typeof content !== 'object') return false;
  const questions = normalizeQuestions((content as any).subQuestions);
  if (questions.length === 0) return false;
  return questions.every((q) =>
    ['SingleChoice', 'MultipleChoice', 'TrueFalse'].includes(q.questionKind)
  );
}

/**
 * Computes navigation status, icon type, and accessible label according to Section 2 state table.
 */
export function getExerciseNavigationStatus(
  result: UnifiedGradingResult | null,
  exerciseType: ExerciseType,
  gradingMode: string,
  content?: unknown
): ExerciseNavStatus {
  if (!result) {
    return {
      state: 'unsubmitted',
      tone: 'neutral',
      label: 'Chưa làm',
      iconType: 'number',
    };
  }

  // 1. Pending / Grading
  if (result.status === 'Pending' || result.status === 'Grading') {
    return {
      state: 'grading',
      tone: 'grading',
      label: 'Đang chấm điểm',
      iconType: 'clock',
    };
  }

  // 2. Failed / FailedGrading
  if (result.status === 'Failed' || result.status === 'FailedGrading') {
    return {
      state: 'failed',
      tone: 'failed',
      label: 'Lỗi chấm điểm',
      iconType: 'alert',
    };
  }

  // 3. NotAssessable
  if (result.status === 'NotAssessable') {
    return {
      state: 'not_assessable',
      tone: 'neutral',
      label: 'Không thể đánh giá',
      iconType: 'help',
    };
  }

  // 4. Completed
  if (result.status === 'Completed') {
    // 4.1 Self-assessment
    if (exerciseType === ExerciseType.Flashcards || gradingMode === 'SelfAssessment') {
      return {
        state: 'self_assessed',
        tone: 'neutral',
        label: 'Đã tự đánh giá',
        iconType: 'check',
      };
    }

    // 4.2 AI Rubric evaluation
    if (
      gradingMode === 'AiRubric' ||
      exerciseType === ExerciseType.SpeakingPractice ||
      exerciseType === ExerciseType.WritingPractice ||
      exerciseType === ExerciseType.Translation
    ) {
      return {
        state: 'ai_evaluated',
        tone: 'neutral',
        label: 'Đã chấm điểm AI',
        iconType: 'sparkles',
      };
    }

    // 4.3 Composite (Reading / Listening)
    if (
      exerciseType === ExerciseType.ReadingComprehension ||
      exerciseType === ExerciseType.ListeningComprehension
    ) {
      const closedOnly = isClosedEndedComposite(content);
      if (closedOnly) {
        const score = getDeterministicScore(result);
        if (score === 100) {
          return { state: 'correct', tone: 'correct', label: 'Chính xác', iconType: 'correct' };
        }
        if (score === 0) {
          return { state: 'incorrect', tone: 'incorrect', label: 'Chưa đúng', iconType: 'incorrect' };
        }
        if (score !== null && score > 0 && score < 100) {
          return { state: 'partial', tone: 'partial', label: 'Đúng một phần', iconType: 'partial' };
        }
      }
      return {
        state: 'completed_neutral',
        tone: 'neutral',
        label: 'Đã nộp bài đọc/nghe',
        iconType: 'check',
      };
    }

    // 4.4 Deterministic (Spelling, MCQ, Fill, Matching, Unscramble, ErrorCorrection, TrueFalse, Dictation)
    const score = getDeterministicScore(result);
    if (score === 100) {
      return {
        state: 'correct',
        tone: 'correct',
        label: 'Chính xác',
        iconType: 'correct',
      };
    }
    if (score === 0) {
      return {
        state: 'incorrect',
        tone: 'incorrect',
        label: 'Chưa đúng',
        iconType: 'incorrect',
      };
    }
    if (score !== null && score > 0 && score < 100) {
      return {
        state: 'partial',
        tone: 'partial',
        label: 'Đúng một phần',
        iconType: 'partial',
      };
    }

    // Completed with unknown or null score
    return {
      state: 'completed_neutral',
      tone: 'neutral',
      label: 'Đã hoàn thành',
      iconType: 'check',
    };
  }

  return {
    state: 'unsubmitted',
    tone: 'neutral',
    label: 'Chưa làm',
    iconType: 'number',
  };
}

/**
 * Extracts comprehension answers dictionary `{ [questionId]: value }` from stored `subQuestionAnswers` payload.
 */
export function extractComprehensionAnswers(
  answerPayload: unknown,
  questions: unknown
): Record<string, unknown> {
  const result: Record<string, unknown> = {};
  if (!answerPayload || typeof answerPayload !== 'object') return result;

  const subAnswers = (answerPayload as any).subQuestionAnswers;
  if (!Array.isArray(subAnswers)) return result;

  const normalized = normalizeQuestions(questions);
  const questionMap = new Map(normalized.map((q) => [q.id, q]));

  for (const item of subAnswers) {
    if (!item || typeof item !== 'object' || !item.questionId) continue;
    const qId = String(item.questionId);
    const q = questionMap.get(qId);

    if (item.selectedOptionIds !== undefined) {
      if (Array.isArray(item.selectedOptionIds)) {
        if (q?.questionKind === 'MultipleChoice') {
          result[qId] = item.selectedOptionIds;
        } else {
          result[qId] = item.selectedOptionIds[0] ?? '';
        }
      }
    } else if (item.answer !== undefined) {
      result[qId] = Boolean(item.answer);
    } else if (item.textAnswer !== undefined) {
      result[qId] = String(item.textAnswer);
    }
  }

  return result;
}
