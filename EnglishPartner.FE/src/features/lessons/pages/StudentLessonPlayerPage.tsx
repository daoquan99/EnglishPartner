import React, { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  ChevronLeft,
  ChevronRight,
  X,
  Award,
  CheckCircle2,
  Clock,
  Sparkles,
  RotateCcw,
} from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import {
  useLessonAttemptQuery,
  useSubmitExerciseAnswerMutation,
  useRetryGradingMutation,
} from '../hooks/useLessonQueries';
import { ExerciseRenderer } from '../components/player/ExerciseRenderer';
import { EXERCISE_TYPE_CATALOG, ExerciseType, type ExerciseAnswerResultDto, type ExerciseLearnerDto } from '../types/lessons.types';
import { cn } from '@/shared/lib/cn';
import { toast } from '@/shared/lib/toast';
import { attachPersistedFeedback, getCompletionFeedback } from '../components/player/gradingFeedback';

export const StudentLessonPlayerPage: React.FC = () => {
  const { attemptId } = useParams<{ attemptId: string }>();
  const navigate = useNavigate();
  const activeAttemptId = useRef(attemptId);
  activeAttemptId.current = attemptId;

  const { data: attempt, isLoading, refetch } = useLessonAttemptQuery(attemptId);
  const submitAnswerMutation = useSubmitExerciseAnswerMutation();
  const retryGradingMutation = useRetryGradingMutation();

  const [currentIndex, setCurrentIndex] = useState(0);
  const [selectedType, setSelectedType] = useState<ExerciseType | 'All'>('All');
  const [currentRevision, setCurrentRevision] = useState<number>(0);
  const [lastResults, setLastResults] = useState<Record<string, ExerciseAnswerResultDto>>({});

  useEffect(() => {
    setCurrentIndex(0);
    setSelectedType('All');
    setLastResults({});
  }, [attemptId]);

  useEffect(() => {
    if (attempt) {
      setCurrentRevision(attempt.attemptRevision);
    }
  }, [attempt]);

  if (isLoading) {
    return (
      <div className="text-center py-20 text-xs font-semibold text-slate-500">
        Đang tải lượt làm bài học...
      </div>
    );
  }

  if (!attempt || !attemptId) {
    return (
      <div className="text-center py-20 space-y-3">
        <p className="text-sm font-bold text-slate-800">Không tìm thấy lượt làm bài này.</p>
        <Button type="button" variant="outline" size="sm" onClick={() => navigate('/student/lessons')}>
          Quay lại danh sách
        </Button>
      </div>
    );
  }

  const exercises = attempt.exercises ?? [];
  const availableTypes = [...new Set(exercises.map((exercise) => exercise.type))];
  const visibleExercises = exercises.filter((exercise) => selectedType === 'All' || exercise.type === selectedType);
  const visibleIndices = visibleExercises.map((exercise) => exercises.indexOf(exercise));
  const visiblePosition = visibleIndices.indexOf(currentIndex);
  const currentExercise = exercises[currentIndex];
  const submittedMap = attempt.submittedAnswers ?? {};

  if (!currentExercise) {
    return (
      <div className="text-center py-20 space-y-3">
        <p className="text-sm font-bold text-slate-800">Bài học này không có bài tập nào.</p>
        <Button type="button" variant="outline" size="sm" onClick={() => navigate('/student/lessons')}>
          Quay lại
        </Button>
      </div>
    );
  }

  const meta = EXERCISE_TYPE_CATALOG[currentExercise.type];
  const currentResult =
    lastResults[currentExercise.id] ?? submittedMap[currentExercise.id] ?? null;

  const handleSubmitAnswer = async (
    answerPayload: Record<string, unknown>,
    subQuestionId?: string | null
  ) => {
    try {
      const res = await submitAnswerMutation.mutateAsync({
        attemptId,
        data: {
          exerciseId: currentExercise.id,
          subQuestionId: subQuestionId ?? null,
          attemptRevision: currentRevision,
          answerPayload,
        },
      });

      if (activeAttemptId.current !== attemptId) return;

      // Update attempt revision from response
      setCurrentRevision(res.attemptRevision);
      setLastResults((prev) => ({ ...prev, [currentExercise.id]: res }));
      if (currentExercise.type === ExerciseType.SpellingPractice && res.status === 'Completed') {
        const feedback = getCompletionFeedback(res, currentExercise.type, currentExercise.gradingMode);
        if (feedback.tone === 'correct') toast.success('Chính xác! Bạn đã viết đúng chính tả.');
        else if (feedback.tone === 'incorrect') toast.warning('Chưa đúng. Xem đáp án và nhận xét bên dưới.');
        else toast.info(feedback.label);
      } else {
        toast.success('Đã nộp bài tập thành công!');
      }
      // Deterministic explanations are persisted by BE and exposed by resume, but the submit DTO omits them.
      const refreshed = await refetch();
      if (activeAttemptId.current !== attemptId) return;
      const withFeedback = attachPersistedFeedback(res, refreshed.data);
      setLastResults((prev) => prev[currentExercise.id]?.attemptRevision === res.attemptRevision
        ? { ...prev, [currentExercise.id]: withFeedback } : prev);
    } catch (error: any) {
      toast.error(error?.response?.data?.error || error?.response?.data?.detail || 'Không nộp được câu trả lời. Vui lòng thử lại.');
    }
  };

  const handleRetry = async () => {
    const answerId = currentResult?.answerId;
    if (!answerId) return;

    try {
      const res = await retryGradingMutation.mutateAsync({ attemptId, answerId });
      setLastResults((prev) => ({ ...prev, [currentExercise.id]: res }));
      toast.success('Đã gửi yêu cầu chấm lại thành công!');
      void refetch();
    } catch {
      toast.error('Không thể chấm lại bài tập.');
    }
  };

  const handleExit = () => {
    if (window.confirm('Bạn có chắc muốn tạm dừng và rời khỏi bài làm? Tiến độ đã nộp sẽ được lưu tự động.')) {
      navigate('/student/lessons');
    }
  };

  const progress = attempt.progress;
  const isFinished = progress.completedExercisesCount === progress.totalExercisesCount && progress.totalExercisesCount > 0;

  return (
    <div className="max-w-4xl mx-auto space-y-6 px-4 py-3">
      {/* Top Bar: Title, Pinned Version & Exit */}
      <div className="flex items-center justify-between border-b border-slate-200/80 pb-4">
        <div>
          <div className="flex items-center gap-2">
            <h2 className="text-base sm:text-lg font-black text-slate-900 truncate">
              {attempt.lessonTitle}
            </h2>
            <span className="text-[11px] font-mono font-bold text-slate-400">
              (v{attempt.pinnedLessonVersion})
            </span>
          </div>

          <div className="flex items-center gap-3 text-xs text-slate-500 font-medium mt-0.5">
            <span>
              Đã làm: <strong>{progress.completedExercisesCount}/{progress.totalExercisesCount}</strong>
            </span>
            <span>•</span>
            <span className="text-emerald-700 font-bold">
              Điểm tạm tính: <strong>{progress.currentSubtotalPoints}/{progress.totalMaxPoints} đ</strong>
            </span>
          </div>
        </div>

        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={handleExit}
          className="rounded-full w-9 h-9 p-0 text-slate-500 hover:text-slate-900"
          title="Tạm dừng & Thoát"
        >
          <X className="w-4 h-4" />
        </Button>
      </div>

      {/* Steps Navigation Bar */}
      <div className="flex flex-wrap items-end gap-3">
        <label className="space-y-1 flex-1 min-w-56">
          <span className="block text-sm font-bold text-slate-700">Loại bài tập</span>
          <select aria-label="Loại bài tập" value={selectedType}
            onChange={(event) => {
              const type = event.target.value as ExerciseType | 'All';
              setSelectedType(type);
              setCurrentIndex(Math.max(0, exercises.findIndex((exercise) => type === 'All' || exercise.type === type)));
            }} className="w-full rounded-xl border border-slate-300 bg-white p-3 text-sm focus:ring-2 focus:ring-emerald-500">
            <option value="All">Tất cả ({exercises.length} bài)</option>
            {availableTypes.map((type) => <option key={type} value={type}>
              {EXERCISE_TYPE_CATALOG[type]?.labelVi ?? type} ({exercises.filter((exercise) => exercise.type === type).length} bài)
            </option>)}
          </select>
        </label>
        <label className="space-y-1 flex-1 min-w-56">
          <span className="block text-sm font-bold text-slate-700">Chọn bài tập</span>
          <select aria-label="Chọn bài tập" value={currentIndex}
            onChange={(event) => setCurrentIndex(Number(event.target.value))}
            className="w-full rounded-xl border border-slate-300 bg-white p-3 text-sm focus:ring-2 focus:ring-emerald-500">
            {visibleExercises.map((exercise, index) => <option key={exercise.id} value={exercises.indexOf(exercise)}>
              {index + 1}. {exercise.title}
            </option>)}
          </select>
        </label>
      </div>
      <div className="flex items-center gap-2 overflow-x-auto py-1 scrollbar-none">
        {visibleExercises.map((ex: ExerciseLearnerDto) => {
          const idx = exercises.indexOf(ex);
          const isDone = Boolean(submittedMap[ex.id] || lastResults[ex.id]);
          const isCurrent = idx === currentIndex;

          return (
            <button
              key={ex.id}
              type="button"
              onClick={() => setCurrentIndex(idx)}
              className={cn(
                'flex items-center justify-center w-9 h-9 rounded-2xl text-xs font-bold transition-all shrink-0 cursor-pointer shadow-2xs',
                isCurrent
                  ? 'bg-emerald-600 text-white ring-2 ring-emerald-400/40 shadow-xs'
                  : isDone
                  ? 'bg-emerald-100 text-emerald-800 border border-emerald-200'
                  : 'bg-white border border-slate-200 text-slate-600 hover:bg-slate-100'
              )}
            >
              {isDone && !isCurrent ? (
                <CheckCircle2 className="w-4 h-4 text-emerald-600" />
              ) : (
                idx + 1
              )}
            </button>
          );
        })}
      </div>

      {/* Completion Banner (if all finished) */}
      {isFinished && (
        <div className="p-5 rounded-3xl bg-gradient-to-r from-emerald-600 to-teal-600 text-white shadow-md flex items-center justify-between gap-4">
          <div className="space-y-1">
            <h3 className="text-base font-extrabold flex items-center gap-2">
              <Sparkles className="w-5 h-5 text-amber-300" />
              Chúc mừng! Bạn đã hoàn thành toàn bộ bài tập!
            </h3>
            <p className="text-xs text-emerald-100">
              Điểm tổng kết: {progress.finalScore100 !== null ? `${progress.finalScore100}%` : `${progress.currentSubtotalPoints}/${progress.totalMaxPoints} điểm`}
            </p>
          </div>

          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => navigate('/student/lesson-history')}
            className="bg-white text-emerald-800 hover:bg-emerald-50 border-transparent rounded-2xl text-xs font-bold shrink-0"
          >
            Xem lịch sử làm bài
          </Button>
        </div>
      )}

      {/* Exercise Active Box */}
      <div className="bg-white rounded-3xl border border-slate-200/90 p-6 sm:p-8 shadow-2xs space-y-6">
        {/* Exercise Header */}
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 pb-4">
          <div className="space-y-1">
            <div className="flex items-center gap-2">
              <Badge variant="outline" className="font-bold text-emerald-700 bg-emerald-50 border-emerald-200">
                {meta?.labelVi ?? currentExercise.type}
              </Badge>
              <span className="text-xs font-bold text-slate-400">
                Bài {currentIndex + 1} / {exercises.length}
              </span>
            </div>
            <h3 className="text-lg font-bold text-slate-900">{currentExercise.title}</h3>
            {currentExercise.instructions && (
              <p className="text-xs text-slate-500">{currentExercise.instructions}</p>
            )}
          </div>

          <Badge variant="outline" className="font-extrabold text-emerald-800">
            <Award className="w-3.5 h-3.5 mr-1 text-emerald-600" />
            {currentExercise.maxPoints} điểm
          </Badge>
        </div>

        {/* Dynamic Exercise Renderer for the 16 types */}
        <ExerciseRenderer
          key={`${attemptId}:${currentExercise.id}`}
          attemptId={attemptId}
          exercise={currentExercise}
          onSubmit={handleSubmitAnswer}
          result={currentResult}
          onRetry={handleRetry}
          isSubmitting={submitAnswerMutation.isPending}
          isRetrying={retryGradingMutation.isPending}
        />
      </div>

      {/* Footer Nav Controls */}
      <div className="flex items-center justify-between pt-2">
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => setCurrentIndex(visibleIndices[Math.max(0, visiblePosition - 1)])}
          disabled={visiblePosition <= 0}
          className="gap-1.5 text-xs rounded-2xl"
        >
          <ChevronLeft className="w-4 h-4" /> Bài trước
        </Button>

        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => setCurrentIndex(visibleIndices[Math.min(visibleIndices.length - 1, visiblePosition + 1)])}
          disabled={visiblePosition >= visibleIndices.length - 1}
          className="gap-1.5 text-xs rounded-2xl"
        >
          Bài tiếp theo <ChevronRight className="w-4 h-4" />
        </Button>
      </div>
    </div>
  );
};
