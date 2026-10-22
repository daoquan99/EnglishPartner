import React from 'react';
import { RotateCcw } from 'lucide-react';
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
import { extractComprehensionAnswers } from './lessonAttemptResolver';

interface ExerciseRendererProps {
  attemptId?: string;
  exercise: ExerciseLearnerDto;
  onSubmit: (answerPayload: Record<string, unknown>, subQuestionId?: string | null) => void;
  result?: ExerciseAnswerResultDto | SubmittedAnswerSummaryDto | null;
  onRetry?: () => void;
  onStartRedo?: () => void;
  onCancelRedo?: () => void;
  isRedoMode?: boolean;
  isAttemptInProgress?: boolean;
  isSubmitting?: boolean;
  isRetrying?: boolean;
}

export const ExerciseRenderer: React.FC<ExerciseRendererProps> = ({
  attemptId,
  exercise,
  onSubmit,
  result,
  onRetry,
  onStartRedo,
  onCancelRedo,
  isRedoMode = false,
  isAttemptInProgress = true,
  isSubmitting = false,
  isRetrying = false,
}) => {
  const isSubmitted = isRedoMode
    ? false
    : Boolean(result && !['Failed', 'FailedGrading', 'NotAssessable'].includes(result.status));
  const content = exercise.content ?? {};
  const answerPayload = isRedoMode
    ? undefined
    : ((result as any)?.answerPayload as Record<string, any> | undefined);
  const comprehensionAnswers = isRedoMode
    ? undefined
    : extractComprehensionAnswers(
        answerPayload,
        (content as any)?.subQuestions
      );

  const renderPlayer = () => {
    switch (exercise.type) {
      case ExerciseType.Flashcards:
        return (
          <FlashcardPlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
            initialRatings={answerPayload?.ratings}
          />
        );

      case ExerciseType.MultipleChoice:
        return (
          <MultipleChoicePlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
            initialSelectedIds={answerPayload?.selectedOptionIds}
          />
        );

      case ExerciseType.FillInTheBlanks:
        return (
          <FillInTheBlanksPlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
            initialAnswers={answerPayload?.answers ?? answerPayload?.blankAnswers}
          />
        );

      case ExerciseType.Matching:
        return (
          <MatchingPlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
            initialPairs={answerPayload?.pairs}
          />
        );

      case ExerciseType.SpellingPractice:
        return (
          <SpellingPracticePlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
            initialSpelledWord={answerPayload?.spelledWord}
          />
        );

      case ExerciseType.SentenceUnscramble:
        return (
          <SentenceUnscramblePlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
            initialOrderedTokenIds={answerPayload?.orderedTokenIds}
          />
        );

      case ExerciseType.ErrorCorrection:
        return (
          <ErrorCorrectionPlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
            initialCorrectedSentence={answerPayload?.correctedSentence}
          />
        );

      case ExerciseType.Translation:
        return (
          <TranslationPlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
            initialTranslatedText={answerPayload?.translatedText}
          />
        );

      case ExerciseType.TrueFalse:
        return (
          <TrueFalsePlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
            initialAnswers={answerPayload?.answers}
          />
        );

      case ExerciseType.ReadingComprehension:
        return (
          <ReadingComprehensionPlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
            initialAnswers={comprehensionAnswers}
          />
        );

      case ExerciseType.ListeningComprehension:
        return (
          <ListeningComprehensionPlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
            initialAnswers={comprehensionAnswers}
          />
        );

      case ExerciseType.Dictation:
        return (
          <DictationPlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
            initialTranscribedText={answerPayload?.transcribedText}
            attemptId={attemptId}
            answerId={isRedoMode ? undefined : result?.answerId}
            result={isRedoMode ? undefined : result}
          />
        );

      case ExerciseType.Shadowing:
        return (
          <ShadowingPlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
            initialAudioAssetId={answerPayload?.audioAssetId}
            initialSpokenText={answerPayload?.spokenText}
          />
        );

      case ExerciseType.SpeakingPractice:
        return (
          <SpeakingPracticePlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
            initialSpokenNotes={answerPayload?.spokenText}
            initialAudioAssetId={answerPayload?.audioAssetId}
          />
        );

      case ExerciseType.WritingPractice:
        return (
          <WritingPracticePlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
            initialWrittenText={answerPayload?.writtenText}
          />
        );

      case ExerciseType.ConversationRolePlay:
        return (
          <ConversationRolePlayPlayer
            content={content as any}
            onSubmit={(payload) => onSubmit(payload)}
            isSubmitting={isSubmitting}
            isSubmitted={isSubmitted}
            initialTurns={answerPayload?.turns}
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
        {isRedoMode && (
          <div className="flex items-center justify-between gap-3 p-3.5 rounded-2xl bg-indigo-50 border border-indigo-200 text-indigo-950 text-xs font-medium">
            <div className="flex items-center gap-2 min-w-0">
              <RotateCcw className="w-4 h-4 text-indigo-600 shrink-0" />
              <span>
                <strong>Chế độ làm lại câu hỏi:</strong> Hãy chọn/nhập đáp án mới và bấm nộp bài. Lần nộp trước được lưu an toàn.
              </span>
            </div>
            {onCancelRedo && (
              <button
                type="button"
                onClick={onCancelRedo}
                className="shrink-0 px-2.5 py-1 text-xs font-bold text-slate-700 bg-white border border-slate-200 hover:bg-slate-50 rounded-xl cursor-pointer"
              >
                Hủy làm lại
              </button>
            )}
          </div>
        )}

        {renderPlayer()}

        {/* Shared Feedback & Grading Outcomes */}
        <GradingResultFeedback
          result={result}
          exerciseType={exercise.type}
          gradingMode={exercise.gradingMode}
          onRetry={onRetry}
          onStartRedo={onStartRedo}
          onCancelRedo={onCancelRedo}
          isRedoMode={isRedoMode}
          isAttemptInProgress={isAttemptInProgress}
          isRetrying={isRetrying}
        />
      </div>
    </LessonAudioContext.Provider>
  );
};
