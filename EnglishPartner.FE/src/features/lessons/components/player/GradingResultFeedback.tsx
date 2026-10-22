import React from 'react';
import { CheckCircle2, XCircle, Clock, AlertTriangle, RefreshCw, Award, Sparkles, RotateCcw } from 'lucide-react';
import { ExerciseType, type ExerciseAnswerResultDto, type SubmittedAnswerSummaryDto } from '../../types/lessons.types';
import { Badge } from '@/shared/components/ui/Badge';
import { Button } from '@/shared/components/ui/Button';
import { getCompletionFeedback, getSpellingCorrection } from './gradingFeedback';
import { cn } from '@/shared/lib/cn';

interface GradingResultFeedbackProps {
  result?: ExerciseAnswerResultDto | SubmittedAnswerSummaryDto | null;
  exerciseType?: ExerciseType;
  gradingMode?: string;
  onRetry?: () => void;
  onStartRedo?: () => void;
  onCancelRedo?: () => void;
  isRedoMode?: boolean;
  isAttemptInProgress?: boolean;
  isRetrying?: boolean;
}

export const GradingResultFeedback: React.FC<GradingResultFeedbackProps> = ({
  result,
  exerciseType,
  gradingMode,
  onRetry,
  onStartRedo,
  onCancelRedo,
  isRedoMode = false,
  isAttemptInProgress = true,
  isRetrying = false,
}) => {
  if (!result) return null;

  const isFullResult = 'attemptProgress' in result;
  const full = isFullResult ? (result as ExerciseAnswerResultDto) : null;

  const isCompleted = result.status === 'Completed';
  const isPending = ['Pending', 'Grading', 'PendingGrading'].includes(result.status);
  const isFailed = ['Failed', 'FailedGrading'].includes(result.status);
  const isNotAssessable = result.status === 'NotAssessable';
  const completion = getCompletionFeedback(result, exerciseType, gradingMode);
  const isSpelling = exerciseType === ExerciseType.SpellingPractice;
  const spellingCorrection = isSpelling ? getSpellingCorrection(result) : null;

  let criteriaBreakdown = ('criteriaBreakdown' in result ? (result as any).criteriaBreakdown : null) ?? null;
  if ((!criteriaBreakdown || criteriaBreakdown.length === 0) && result.feedback) {
    const trimmed = result.feedback.trim();
    if (trimmed.startsWith('[')) {
      try {
        const parsed = JSON.parse(trimmed);
        if (Array.isArray(parsed) && parsed.length > 0 && (parsed[0]?.CriterionCode || parsed[0]?.criterionCode)) {
          criteriaBreakdown = parsed.map((item: any) => ({
            criterionCode: item.CriterionCode || item.criterionCode || '',
            name: item.Name || item.name || '',
            weight: item.Weight ?? item.weight ?? 0,
            rawScore: item.RawScore ?? item.rawScore ?? 0,
            descriptor: item.Descriptor || item.descriptor || '',
            feedback: item.Feedback || item.feedback || '',
          }));
        }
      } catch {
        // ignore
      }
    }
  }

  const isRawJsonFeedback = typeof result.feedback === 'string' &&
    (result.feedback.trim().startsWith('[') || result.feedback.trim().startsWith('{'));

  const renderFeedbackDetails = () => (
    <>
      {/* Basic Feedback (if available and not raw JSON) */}
      {isCompleted && spellingCorrection && (
        <div className="rounded-xl border border-rose-100 bg-rose-50 p-3 text-sm text-rose-900">
          <span className="font-bold">Đáp án đúng: </span><span>{spellingCorrection}</span>
        </div>
      )}
      {result.feedback && !spellingCorrection && !isRawJsonFeedback && (
        <div className="text-xs text-slate-700 bg-slate-50 p-3 rounded-xl border border-slate-100">
          <span className="font-semibold text-slate-900 block mb-1">Nhận xét:</span>
          {isSpelling && result.feedback === 'Correct spelling!' ? 'Bạn đã viết đúng chính tả.' : result.feedback}
        </div>
      )}

      {/* AI Rubric Criteria Breakdown */}
      {criteriaBreakdown && criteriaBreakdown.length > 0 && (
        <div className="space-y-2.5">
          <h4 className="text-xs font-bold text-slate-700 flex items-center gap-1.5 uppercase tracking-wider">
            <Sparkles className="w-3.5 h-3.5 text-amber-500" />
            Chi tiết đánh giá theo tiêu chí Rubric
          </h4>

          <div className="grid grid-cols-1 gap-2.5">
            {criteriaBreakdown.map((c: any) => (
              <div
                key={c.criterionCode}
                className="p-3 rounded-xl border border-slate-100 bg-slate-50/70 text-xs space-y-1.5"
              >
                <div className="flex items-center justify-between font-semibold text-slate-800">
                  <span>{c.name} ({c.weight}%)</span>
                  <span className="text-emerald-700 font-bold">
                    Mức {c.rawScore}/4
                  </span>
                </div>
                {c.descriptor && (
                  <p className="text-[11px] text-slate-600 italic">
                    &ldquo;{c.descriptor}&rdquo;
                  </p>
                )}
                {c.feedback && (
                  <p className="text-xs text-slate-700 font-medium bg-white/80 p-2 rounded-lg border border-slate-100">
                    {c.feedback}
                  </p>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Strengths & Improvements */}
      {(() => {
        const strengths: string[] = full?.strengths ?? ('strengths' in result ? (result as any).strengths : null) ?? [];
        const improvements: string[] = full?.improvements ?? ('improvements' in result ? (result as any).improvements : null) ?? [];
        if (!strengths.length && !improvements.length) return null;

        return (
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-1">
            {strengths.length > 0 && (
              <div className="p-3 bg-emerald-50/60 border border-emerald-100 rounded-xl text-xs space-y-1">
                <span className="font-bold text-emerald-800 block">Điểm sáng:</span>
                <ul className="list-disc list-inside text-emerald-700 space-y-0.5">
                  {strengths.map((s, idx) => (
                    <li key={idx}>{s}</li>
                  ))}
                </ul>
              </div>
            )}

            {improvements.length > 0 && (
              <div className="p-3 bg-amber-50/60 border border-amber-100 rounded-xl text-xs space-y-1">
                <span className="font-bold text-amber-800 block">Cần cải thiện:</span>
                <ul className="list-disc list-inside text-amber-700 space-y-0.5">
                  {improvements.map((imp, idx) => (
                    <li key={idx}>{imp}</li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        );
      })()}

      {/* Suggested Answer */}
      {(() => {
        const suggestedAnswer = full?.suggestedAnswer ?? ('suggestedAnswer' in result ? (result as any).suggestedAnswer : null);
        if (!suggestedAnswer) return null;

        return (
          <div className="p-3 bg-sky-50/60 border border-sky-100 rounded-xl text-xs space-y-1">
            <span className="font-bold text-sky-800 block">Đáp án gợi ý:</span>
            <p className="text-sky-900 font-medium">{suggestedAnswer}</p>
          </div>
        );
      })()}
    </>
  );

  // Redo mode: Previous result is collapsible, with clear label "Lần đã nộp trước"
  if (isRedoMode) {
    return (
      <div role="region" aria-label="Lần đã nộp trước" className="mt-4 rounded-2xl border border-indigo-200 bg-indigo-50/40 p-4 space-y-3">
        <div className="flex flex-wrap items-center justify-between gap-2 border-b border-indigo-100 pb-2.5">
          <div className="flex items-center gap-2 text-xs font-bold text-indigo-950">
            <RotateCcw className="w-4 h-4 text-indigo-600 shrink-0" />
            <span>Đang ở chế độ làm lại — Nhập câu trả lời mới và bấm nộp lại ở trên.</span>
          </div>
          {onCancelRedo && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={onCancelRedo}
              className="text-xs font-bold text-slate-700 bg-white border-slate-200 hover:bg-slate-50 rounded-xl"
            >
              Hủy làm lại (giữ bài cũ)
            </Button>
          )}
        </div>

        <details className="text-xs group">
          <summary className="cursor-pointer font-bold text-slate-700 hover:text-indigo-600 flex items-center justify-between py-1 select-none">
            <div className="flex items-center gap-2">
              <span className="text-slate-500">▶</span>
              <span>Lần đã nộp trước: {completion.label}</span>
            </div>
            {result.score100 !== null && result.score100 !== undefined && (
              <Badge variant={result.score100 >= 75 ? 'emerald' : result.score100 >= 50 ? 'amber' : 'rose'}>
                {result.score100}% {result.pointsEarned != null && `(${result.pointsEarned} đ)`}
              </Badge>
            )}
          </summary>
          <div className="pt-3 border-t border-slate-200 mt-2 space-y-3">
            {renderFeedbackDetails()}
          </div>
        </details>
      </div>
    );
  }

  return (
    <div role="status" aria-live="polite" className={cn('mt-4 rounded-2xl border bg-white p-4 sm:p-5 shadow-xs space-y-4',
      isCompleted && completion.tone === 'correct' ? 'border-emerald-300' :
      isCompleted && completion.tone === 'incorrect' ? 'border-rose-300' :
      isCompleted && completion.tone === 'partial' ? 'border-amber-300' : 'border-slate-200/80')}>
      {/* Header Status & Score */}
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 pb-3">
        <div className="flex items-center gap-2">
          {isCompleted && completion.tone === 'correct' && <CheckCircle2 className="w-5 h-5 text-emerald-600 shrink-0" />}
          {isCompleted && completion.tone === 'incorrect' && <XCircle className="w-5 h-5 text-rose-600 shrink-0" />}
          {isCompleted && completion.tone === 'partial' && <AlertTriangle className="w-5 h-5 text-amber-600 shrink-0" />}
          {isCompleted && completion.tone === 'neutral' && <Award className="w-5 h-5 text-slate-500 shrink-0" />}
          {isPending && <Clock className="w-5 h-5 text-amber-500 animate-spin shrink-0" />}
          {isFailed && <AlertTriangle className="w-5 h-5 text-rose-500 shrink-0" />}
          {isNotAssessable && <XCircle className="w-5 h-5 text-slate-400 shrink-0" />}

          <div>
            <span className="text-sm font-bold text-slate-800">
              {isCompleted && completion.label}
              {isPending && 'Đang chấm điểm qua AI...'}
              {isFailed && 'Chấm điểm không thành công'}
              {isNotAssessable && 'Bài làm không đủ điều kiện chấm'}
            </span>
            {result.maxPoints > 0 && (
              <span className="text-xs text-slate-500 ml-2">
                (Tối đa: {result.maxPoints} điểm)
              </span>
            )}
          </div>
        </div>

        <div className="flex items-center gap-2">
          {result.score100 !== null && result.score100 !== undefined && (
            <Badge variant={result.score100 >= 75 ? 'emerald' : result.score100 >= 50 ? 'amber' : 'rose'}>
              <Award className="w-3.5 h-3.5 mr-1" />
              {result.score100}% {result.pointsEarned != null && `(${result.pointsEarned} đ)`}
            </Badge>
          )}

          {/* Làm lại action for Completed exercises in active InProgress attempts */}
          {isCompleted && isAttemptInProgress && onStartRedo && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={onStartRedo}
              className="gap-1.5 text-xs text-indigo-700 border-indigo-200 hover:bg-indigo-50 font-bold rounded-xl cursor-pointer"
              title="Làm lại câu hỏi này với đáp án mới"
            >
              <RotateCcw className="w-3.5 h-3.5 text-indigo-600" />
              Làm lại
            </Button>
          )}

          {isFailed && onRetry && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={onRetry}
              disabled={isRetrying}
              className="gap-1.5 text-xs text-rose-600 border-rose-200 hover:bg-rose-50"
            >
              <RefreshCw className={`w-3.5 h-3.5 ${isRetrying ? 'animate-spin' : ''}`} />
              {isRetrying ? 'Đang thử lại...' : 'Chấm lại'}
            </Button>
          )}
        </div>
      </div>

      {renderFeedbackDetails()}
    </div>
  );
};
