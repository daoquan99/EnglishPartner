import React from 'react';
import { ExerciseType, type ExerciseLearnerDto, type ExerciseAnswerResultDto, type SubmittedAnswerSummaryDto } from '../../types/lessons.types';
import { FlashcardPlayer } from './FlashcardPlayer';
import { MultipleChoicePlayer } from './MultipleChoicePlayer';
import { FillInTheBlanksPlayer } from './FillInTheBlanksPlayer';
import { MatchingPlayer } from './MatchingPlayer';
import { SpellingPracticePlayer } from './SpellingPracticePlayer';
import { SentenceUnscramblePlayer } from './SentenceUnscramblePlayer';
import { ErrorCorrectionPlayer } from './ErrorCorrectionPlayer';
import { TranslationPlayer } from './TranslationPlayer';
import { TrueFalsePlayer } from './TrueFalsePlayer';
import { ReadingComprehensionPlayer } from './ReadingComprehensionPlayer';
import { ListeningComprehensionPlayer } from './ListeningComprehensionPlayer';
import { DictationPlayer } from './DictationPlayer';
import { ShadowingPlayer } from './ShadowingPlayer';
import { SpeakingPracticePlayer } from './SpeakingPracticePlayer';
import { WritingPracticePlayer } from './WritingPracticePlayer';
import { ConversationRolePlayPlayer } from './ConversationRolePlayPlayer';
import { GradingResultFeedback } from './GradingResultFeedback';
import { LessonAudioContext } from './LessonAudioButton';

interface ExerciseRendererProps {
  attemptId?: string;
  exercise: ExerciseLearnerDto;
  onSubmit: (answerPayload: Record<string, unknown>, subQuestionId?: string | null) => void;
  result?: ExerciseAnswerResultDto | SubmittedAnswerSummaryDto | null;
  onRetry?: () => void;
  isSubmitting?: boolean;
  isRetrying?: boolean;
}

export const ExerciseRenderer: React.FC<ExerciseRendererProps> = ({
  attemptId,
  exercise,
  onSubmit,
  result,
  onRetry,
  isSubmitting = false,
  isRetrying = false,
}) => {
  const isSubmitted = Boolean(result && !['Failed', 'FailedGrading', 'NotAssessable'].includes(result.status));
  const content = exercise.content ?? {};

  const renderPlayer = () => {
    switch (exercise.type) {
      case ExerciseType.Flashcards:
        return (
          <FlashcardPlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
          />
        );

      case ExerciseType.MultipleChoice:
        return (
          <MultipleChoicePlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
          />
        );

      case ExerciseType.FillInTheBlanks:
        return (
          <FillInTheBlanksPlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
          />
        );

      case ExerciseType.Matching:
        return (
          <MatchingPlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
          />
        );

      case ExerciseType.SpellingPractice:
        return (
          <SpellingPracticePlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
          />
        );

      case ExerciseType.SentenceUnscramble:
        return (
          <SentenceUnscramblePlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
          />
        );

      case ExerciseType.ErrorCorrection:
        return (
          <ErrorCorrectionPlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
          />
        );

      case ExerciseType.Translation:
        return (
          <TranslationPlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
          />
        );

      case ExerciseType.TrueFalse:
        return (
          <TrueFalsePlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
          />
        );

      case ExerciseType.ReadingComprehension:
        return (
          <ReadingComprehensionPlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
          />
        );

      case ExerciseType.ListeningComprehension:
        return (
          <ListeningComprehensionPlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
          />
        );

      case ExerciseType.Dictation:
        return (
          <DictationPlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
          />
        );

      case ExerciseType.Shadowing:
        return (
          <ShadowingPlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
          />
        );

      case ExerciseType.SpeakingPractice:
        return (
          <SpeakingPracticePlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
          />
        );

      case ExerciseType.WritingPractice:
        return (
          <WritingPracticePlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
          />
        );

      case ExerciseType.ConversationRolePlay:
        return (
          <ConversationRolePlayPlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
          />
        );

      default: {
        // Compile-time check: ensures all 16 ExerciseType members are explicitly handled
        const unhandled: never = exercise.type;
        return (
          <div className="p-4 bg-rose-50 border border-rose-200 text-rose-800 rounded-2xl text-xs font-semibold">
            Loại bài tập không được nhận diện: {String(unhandled)}. Vui lòng kiểm tra lại cấu hình.
          </div>
        );
      }
    }
  };

  return (
    <LessonAudioContext.Provider value={attemptId ? { attemptId, exerciseId: exercise.id } : null}>
    <div className="space-y-4">
      {renderPlayer()}

      {/* Shared Feedback & Grading Outcomes */}
      <GradingResultFeedback
        result={result}
        exerciseType={exercise.type}
        gradingMode={exercise.gradingMode}
        onRetry={onRetry}
        isRetrying={isRetrying}
      />
    </div>
    </LessonAudioContext.Provider>
  );
};
