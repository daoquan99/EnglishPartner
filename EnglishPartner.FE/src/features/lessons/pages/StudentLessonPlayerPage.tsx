import React, { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate, useSearchParams } from 'react-router-dom';
import {
  ChevronLeft,
  ChevronRight,
  X,
  Award,
  CheckCircle2,
  XCircle,
  MinusCircle,
  Clock,
  Sparkles,
  AlertCircle,
  HelpCircle,
  Check,
  RotateCcw,
  Volume2,
  Keyboard,
  Filter,
} from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import {
  useLessonAttemptQuery,
  useSubmitExerciseAnswerMutation,
  useRetryGradingMutation,
  useStartAttemptMutation,
} from '../hooks/useLessonQueries';
import { ExerciseRenderer } from '../components/player/ExerciseRenderer';
import { EXERCISE_TYPE_CATALOG, ExerciseType, type ExerciseAnswerResultDto, type ExerciseLearnerDto } from '../types/lessons.types';
import { cn } from '@/shared/lib/cn';
import { toast } from '@/shared/lib/toast';
import { attachPersistedFeedback, getCompletionFeedback } from '../components/player/gradingFeedback';
import {
  resolveExerciseResult,
  getExerciseNavigationStatus,
} from '../components/player/lessonAttemptResolver';
import { AiSpeechSettingsDialog } from '@/features/speech';
import { KeyboardShortcutsDialog } from '../components/player/KeyboardShortcutsDialog';
import { useLessonPlayerShortcuts } from '../hooks/useLessonPlayerShortcuts';
import { useTutorSessionStore } from '@/features/tutor';

const VALID_RESULT_STATUSES = ['all', 'correct', 'incorrect', 'partial', 'unsubmitted'] as const;
type ResultFilterStatus = typeof VALID_RESULT_STATUSES[number];

export const StudentLessonPlayerPage: React.FC = () => {
  const { attemptId } = useParams<{ attemptId: string }>();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const activeAttemptId = useRef(attemptId);
  activeAttemptId.current = attemptId;
  const exerciseContainerRef = useRef<HTMLDivElement | null>(null);

  const apiKey = useTutorSessionStore((s) => s.apiKey);
  const assessmentModelCode = useTutorSessionStore((s) => s.assessmentModelCode);

  const { data: attempt, isLoading, refetch } = useLessonAttemptQuery(attemptId);
  const submitAnswerMutation = useSubmitExerciseAnswerMutation();
  const retryGradingMutation = useRetryGradingMutation();
  const startAttemptMutation = useStartAttemptMutation();

  const [currentIndex, setCurrentIndex] = useState(0);
  const [currentRevision, setCurrentRevision] = useState<number>(0);
  const [lastResults, setLastResults] = useState<Record<string, ExerciseAnswerResultDto>>({});
  const [redoExerciseIds, setRedoExerciseIds] = useState<Record<string, boolean>>({});
  const [isSpeechSettingsOpen, setIsSpeechSettingsOpen] = useState(false);
  const [isShortcutsHelpOpen, setIsShortcutsHelpOpen] = useState(false);

  useEffect(() => {
    setCurrentIndex(0);
    setLastResults({});
    setRedoExerciseIds({});
  }, [attemptId]);

  useEffect(() => {
    if (attempt) {
      setCurrentRevision(attempt.attemptRevision);
    }
  }, [attempt]);

  // Read URL search params for type & status filters
  const rawType = searchParams.get('type') || 'All';
  const selectedType: ExerciseType | 'All' =
    rawType === 'All' || Object.values(ExerciseType).includes(rawType as ExerciseType)
      ? (rawType as ExerciseType | 'All')
      : 'All';

  const rawStatus = (searchParams.get('status') || 'all').toLowerCase();
  const selectedStatus: ResultFilterStatus = VALID_RESULT_STATUSES.includes(rawStatus as any)
    ? (rawStatus as ResultFilterStatus)
    : 'all';

  const updateFilter = (newType: ExerciseType | 'All', newStatus: ResultFilterStatus) => {
    const nextParams = new URLSearchParams(searchParams);
    if (newType === 'All') {
      nextParams.delete('type');
    } else {
      nextParams.set('type', newType);
    }

    if (newStatus === 'all') {
      nextParams.delete('status');
    } else {
      nextParams.set('status', newStatus);
    }

    setSearchParams(nextParams, { replace: true });
  };

  const handleResetFilters = () => {
    const nextParams = new URLSearchParams(searchParams);
    nextParams.delete('type');
    nextParams.delete('status');
    setSearchParams(nextParams, { replace: true });
  };

  const exercises = attempt?.exercises ?? [];
  const availableTypes = [...new Set(exercises.map((exercise) => exercise.type))];
  const submittedMap = attempt?.submittedAnswers ?? {};
  const isAttemptInProgress = attempt?.status === 'InProgress';

  // Exercises filtered by selected exercise type
  const typeFilteredExercises = exercises.filter(
    (exercise) => selectedType === 'All' || exercise.type === selectedType
  );

  // Compute status counts dynamically across typeFilteredExercises
  const statusCounts = {
    all: typeFilteredExercises.length,
    correct: 0,
    incorrect: 0,
    partial: 0,
    unsubmitted: 0,
  };

  for (const ex of typeFilteredExercises) {
    const res = resolveExerciseResult(ex, lastResults, submittedMap);
    const navStatus = getExerciseNavigationStatus(res, ex.type, ex.gradingMode, ex.content);
    if (navStatus.state === 'correct') {
      statusCounts.correct++;
    } else if (navStatus.state === 'incorrect') {
      statusCounts.incorrect++;
    } else if (navStatus.state === 'partial') {
      statusCounts.partial++;
    } else if (navStatus.state === 'unsubmitted') {
      statusCounts.unsubmitted++;
    }
  }

  // Combined AND filter
  const visibleExercises = typeFilteredExercises.filter((exercise) => {
    if (selectedStatus === 'all') return true;
    const res = resolveExerciseResult(exercise, lastResults, submittedMap);
    const navStatus = getExerciseNavigationStatus(res, exercise.type, exercise.gradingMode, exercise.content);
    return navStatus.state === selectedStatus;
  });

  // Keep index within visible exercises range whenever filters change
  useEffect(() => {
    if (visibleExercises.length === 0) return;
    const currentInVisible = visibleExercises.some((e) => exercises.indexOf(e) === currentIndex);
    if (!currentInVisible) {
      const firstVisibleIndex = exercises.indexOf(visibleExercises[0]);
      if (firstVisibleIndex >= 0) {
        setCurrentIndex(firstVisibleIndex);
      }
    }
  }, [visibleExercises, currentIndex, exercises]);

  const currentExercise = exercises[currentIndex] ?? visibleExercises[0] ?? exercises[0];
  const isRedoMode = Boolean(currentExercise && redoExerciseIds[currentExercise.id]);

  const handleStartRedo = () => {
    if (!currentExercise || !isAttemptInProgress) return;
    setRedoExerciseIds((prev) => ({
      ...prev,
      [currentExercise.id]: true,
    }));
  };

  const handleCancelRedo = () => {
    if (!currentExercise) return;
    setRedoExerciseIds((prev) => {
      const next = { ...prev };
      delete next[currentExercise.id];
      return next;
    });
  };

  const confirmDiscardRedo = (): boolean => {
    if (!isRedoMode) return true;
    const proceed = window.confirm(
      'Bạn đang ở chế độ làm lại. Chuyển câu sẽ hủy bản nháp chưa nộp. Bạn có chắc muốn tiếp tục?'
    );
    if (proceed && currentExercise) {
      handleCancelRedo();
    }
    return proceed;
  };

  const visibleIndices = visibleExercises.map((exercise) => exercises.indexOf(exercise));
  const visiblePosition = visibleIndices.indexOf(currentIndex);
  const canGoNext = visiblePosition >= 0 && visiblePosition < visibleIndices.length - 1;
  const canGoPrevious = visiblePosition > 0;

  const handleNext = () => {
    if (canGoNext) {
      if (!confirmDiscardRedo()) return;
      setCurrentIndex(visibleIndices[visiblePosition + 1]);
    }
  };

  const handlePrevious = () => {
    if (canGoPrevious) {
      if (!confirmDiscardRedo()) return;
      setCurrentIndex(visibleIndices[visiblePosition - 1]);
    }
  };

  const handleSelectIndex = (newIndex: number) => {
    if (newIndex === currentIndex) return;
    if (!confirmDiscardRedo()) return;
    setCurrentIndex(newIndex);
  };

  useLessonPlayerShortcuts({
    onNext: handleNext,
    onPrevious: handlePrevious,
    onOpenSpeechSettings: () => setIsSpeechSettingsOpen(true),
    onOpenShortcutsHelp: () => setIsShortcutsHelpOpen(true),
    canGoNext,
    canGoPrevious,
    containerRef: exerciseContainerRef,
  });

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

  if (exercises.length === 0) {
    return (
      <div className="text-center py-20 space-y-3">
        <p className="text-sm font-bold text-slate-800">Bài học này không có bài tập nào.</p>
        <Button type="button" variant="outline" size="sm" onClick={() => navigate('/student/lessons')}>
          Quay lại
        </Button>
      </div>
    );
  }

  const meta = currentExercise ? EXERCISE_TYPE_CATALOG[currentExercise.type] : undefined;
  const currentResult = currentExercise
    ? resolveExerciseResult(currentExercise, lastResults, submittedMap)
    : null;

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
          aiConfig: {
            apiKey: apiKey || null,
            assessmentModelCode: assessmentModelCode || null,
          },
        },
      });

      if (activeAttemptId.current !== attemptId) return;

      // Update attempt revision from response
      setCurrentRevision(res.attemptRevision);
      setLastResults((prev) => ({ ...prev, [currentExercise.id]: res }));

      // Clear redo state for this exercise
      setRedoExerciseIds((prev) => {
        const next = { ...prev };
        delete next[currentExercise.id];
        return next;
      });

      if (res.status === 'Failed' || res.status === 'FailedGrading') {
        toast.error(res.errorMessage || 'Chấm điểm bài tập thất bại. Vui lòng kiểm tra lại cấu hình hoặc thử lại.');
      } else if (currentExercise.type === ExerciseType.SpellingPractice && res.status === 'Completed') {
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
      const res = await retryGradingMutation.mutateAsync({
        attemptId,
        answerId,
        payload: {
          attemptRevision: currentRevision,
          aiConfig: {
            apiKey: apiKey || null,
            assessmentModelCode: assessmentModelCode || null,
          },
        },
      });
      setLastResults((prev) => ({ ...prev, [currentExercise.id]: res }));
      if (res.status === 'Failed' || res.status === 'FailedGrading') {
        toast.error(res.errorMessage || 'Chấm lại thất bại. Vui lòng thử lại.');
      } else {
        toast.success('Đã gửi yêu cầu chấm lại thành công!');
      }
      void refetch();
    } catch {
      toast.error('Không thể chấm lại bài tập.');
    }
  };

  const handleExit = () => {
    if (isRedoMode) {
      if (!window.confirm('Bạn đang ở chế độ làm lại. Rời khỏi trang sẽ hủy bản nháp chưa nộp. Bạn có chắc muốn rời khỏi?')) {
        return;
      }
    } else if (!window.confirm('Bạn có chắc muốn tạm dừng và rời khỏi bài làm? Tiến độ đã nộp sẽ được lưu tự động.')) {
      return;
    }
    navigate('/student/lessons');
  };

  const handleStartNewAttempt = async () => {
    if (!attempt?.lessonId) return;
    try {
      const res = await startAttemptMutation.mutateAsync(attempt.lessonId);
      toast.success('Đã bắt đầu lượt luyện tập mới thành công!');
      navigate(`/student/lesson-attempts/${res.attemptId}`);
    } catch (error: any) {
      toast.error(error?.response?.data?.error || 'Không thể tạo lượt làm bài mới. Vui lòng thử lại.');
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

        <div className="flex items-center gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => setIsShortcutsHelpOpen(true)}
            className="rounded-2xl text-xs font-bold gap-1.5 border-slate-200 text-slate-700 hover:bg-slate-50 cursor-pointer"
            title="Xem danh sách phím tắt luyện bài (Phím ?)"
          >
            <Keyboard className="w-4 h-4 text-indigo-600" />
            <span className="hidden sm:inline">Phím tắt</span>
          </Button>

          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => setIsSpeechSettingsOpen(true)}
            className="rounded-2xl text-xs font-bold gap-1.5 border-slate-200 text-slate-700 hover:bg-slate-50 cursor-pointer"
            title="Cấu hình AI Speech & Luyện âm (Alt + S)"
          >
            <Volume2 className="w-4 h-4 text-emerald-600" />
            <span className="hidden sm:inline">Cấu hình AI Speech</span>
          </Button>

          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleExit}
            className="rounded-full w-9 h-9 p-0 text-slate-500 hover:text-slate-900 cursor-pointer"
            title="Tạm dừng & Thoát"
          >
            <X className="w-4 h-4" />
          </Button>
        </div>
      </div>

      {/* Steps Navigation Bar */}
      <div className="flex flex-wrap items-end gap-3">
        <label className="space-y-1 flex-1 min-w-44">
          <span className="block text-xs font-bold text-slate-700">Loại bài tập</span>
          <select
            aria-label="Loại bài tập"
            value={selectedType}
            onChange={(event) => {
              if (!confirmDiscardRedo()) return;
              const type = event.target.value as ExerciseType | 'All';
              updateFilter(type, selectedStatus);
            }}
            className="w-full rounded-xl border border-slate-300 bg-white p-2.5 text-xs font-medium focus:ring-2 focus:ring-emerald-500"
          >
            <option value="All">Tất cả ({exercises.length} bài)</option>
            {availableTypes.map((type) => (
              <option key={type} value={type}>
                {EXERCISE_TYPE_CATALOG[type]?.labelVi ?? type} ({exercises.filter((exercise) => exercise.type === type).length} bài)
              </option>
            ))}
          </select>
        </label>

        <label className="space-y-1 flex-1 min-w-44">
          <span className="block text-xs font-bold text-slate-700">Kết quả</span>
          <select
            aria-label="Kết quả"
            value={selectedStatus}
            onChange={(event) => {
              if (!confirmDiscardRedo()) return;
              const status = event.target.value as ResultFilterStatus;
              updateFilter(selectedType, status);
            }}
            className="w-full rounded-xl border border-slate-300 bg-white p-2.5 text-xs font-medium focus:ring-2 focus:ring-emerald-500"
          >
            <option value="all">Tất cả ({statusCounts.all})</option>
            <option value="correct">Đã làm đúng ({statusCounts.correct})</option>
            <option value="incorrect">Đã làm sai ({statusCounts.incorrect})</option>
            <option value="partial">Đúng một phần ({statusCounts.partial})</option>
            <option value="unsubmitted">Chưa làm ({statusCounts.unsubmitted})</option>
          </select>
        </label>

        <label className="space-y-1 flex-1 min-w-48">
          <span className="block text-xs font-bold text-slate-700">Chọn bài tập</span>
          <select
            aria-label="Chọn bài tập"
            value={currentIndex}
            disabled={visibleExercises.length === 0}
            onChange={(event) => handleSelectIndex(Number(event.target.value))}
            className="w-full rounded-xl border border-slate-300 bg-white p-2.5 text-xs font-medium focus:ring-2 focus:ring-emerald-500"
          >
            {visibleExercises.length === 0 ? (
              <option value="">(Không có câu phù hợp)</option>
            ) : (
              visibleExercises.map((exercise) => {
                const res = resolveExerciseResult(exercise, lastResults, submittedMap);
                const status = getExerciseNavigationStatus(res, exercise.type, exercise.gradingMode, exercise.content);
                const origIdx = exercises.indexOf(exercise);
                return (
                  <option key={exercise.id} value={origIdx}>
                    {origIdx + 1}. {exercise.title} ({status.label})
                  </option>
                );
              })
            )}
          </select>
        </label>

        {(selectedType !== 'All' || selectedStatus !== 'all') && (
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleResetFilters}
            className="rounded-xl text-xs font-semibold h-9.5 text-slate-600 hover:text-slate-900 border-slate-300 cursor-pointer"
            title="Đặt lại tất cả bộ lọc"
          >
            <RotateCcw className="w-3.5 h-3.5 mr-1 text-slate-500" />
            Đặt lại bộ lọc
          </Button>
        )}
      </div>

      <div className="flex items-center gap-2 overflow-x-auto py-1 scrollbar-none" role="tablist" aria-label="Danh sách bài tập">
        {visibleExercises.length === 0 ? (
          <div className="text-xs text-slate-400 italic py-1">
            Không có bài tập nào phù hợp với điều kiện lọc hiện tại.
          </div>
        ) : (
          visibleExercises.map((ex: ExerciseLearnerDto) => {
            const idx = exercises.indexOf(ex);
            const res = resolveExerciseResult(ex, lastResults, submittedMap);
            const navStatus = getExerciseNavigationStatus(res, ex.type, ex.gradingMode, ex.content);
            const isCurrent = idx === currentIndex;

            return (
              <button
                key={ex.id}
                type="button"
                onClick={() => handleSelectIndex(idx)}
                aria-current={isCurrent ? 'step' : undefined}
                title={`Bài ${idx + 1}: ${ex.title} — ${navStatus.label}${isCurrent ? ' (đang xem)' : ''}`}
                aria-label={`Bài ${idx + 1}: ${ex.title} — ${navStatus.label}${isCurrent ? ' (đang xem)' : ''}`}
                className={cn(
                  'relative flex items-center justify-center w-9 h-9 rounded-2xl text-xs font-bold transition-all shrink-0 cursor-pointer shadow-2xs border',
                  // Tone colors based on state table
                  navStatus.tone === 'correct'
                    ? 'bg-emerald-50 text-emerald-800 border-emerald-300 hover:bg-emerald-100'
                    : navStatus.tone === 'incorrect'
                    ? 'bg-rose-50 text-rose-800 border-rose-300 hover:bg-rose-100'
                    : navStatus.tone === 'partial'
                    ? 'bg-amber-50 text-amber-800 border-amber-300 hover:bg-amber-100'
                    : navStatus.tone === 'failed'
                    ? 'bg-rose-100 text-rose-900 border-rose-400 hover:bg-rose-200'
                    : navStatus.tone === 'grading'
                    ? 'bg-amber-50 text-amber-800 border-amber-200'
                    : navStatus.state === 'unsubmitted'
                    ? 'bg-white border-slate-200 text-slate-600 hover:bg-slate-100'
                    : 'bg-slate-100 text-slate-800 border-slate-300 hover:bg-slate-200',
                  // Active/current ring state: INDEPENDENT of correctness!
                  isCurrent && 'ring-2 ring-emerald-500 ring-offset-2 shadow-sm font-black'
                )}
              >
                {navStatus.iconType === 'correct' ? (
                  <CheckCircle2 className="w-4 h-4 text-emerald-600" />
                ) : navStatus.iconType === 'incorrect' ? (
                  <XCircle className="w-4 h-4 text-rose-600" />
                ) : navStatus.iconType === 'partial' ? (
                  <MinusCircle className="w-4 h-4 text-amber-600" />
                ) : navStatus.iconType === 'clock' ? (
                  <Clock className="w-4 h-4 text-amber-500 animate-pulse" />
                ) : navStatus.iconType === 'alert' ? (
                  <AlertCircle className="w-4 h-4 text-rose-600" />
                ) : navStatus.iconType === 'help' ? (
                  <HelpCircle className="w-4 h-4 text-slate-500" />
                ) : navStatus.iconType === 'sparkles' ? (
                  <Sparkles className="w-4 h-4 text-indigo-600" />
                ) : navStatus.iconType === 'check' ? (
                  <Check className="w-4 h-4 text-emerald-700" />
                ) : (
                  idx + 1
                )}
              </button>
            );
          })
        )}
      </div>

      {/* Completion or Historical Banner */}
      {!isAttemptInProgress && (
        <div className="p-5 rounded-3xl bg-slate-900 text-white shadow-md flex flex-wrap items-center justify-between gap-4">
          <div className="space-y-1">
            <div className="flex items-center gap-2">
              <Sparkles className="w-5 h-5 text-amber-400" />
              <h3 className="text-base font-extrabold">
                {attempt.status === 'Completed'
                  ? 'Lượt làm bài đã hoàn thành'
                  : 'Lượt làm bài đã kết thúc'}
              </h3>
              <Badge variant="emerald" className="text-[10px] bg-emerald-950 text-emerald-200 border-emerald-700">Lịch sử cố định</Badge>
            </div>
            <p className="text-xs text-slate-300">
              Điểm tổng kết: {progress.finalScore100 !== null ? `${progress.finalScore100}%` : `${progress.currentSubtotalPoints}/${progress.totalMaxPoints} đ`}. Để luyện lại bài tập, bạn có thể bắt đầu một lượt mới.
            </p>
          </div>

          <div className="flex items-center gap-2">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => navigate('/student/lesson-history')}
              className="bg-transparent text-white hover:bg-slate-800 border-slate-700 rounded-2xl text-xs font-bold shrink-0"
            >
              Xem lịch sử
            </Button>
            <Button
              type="button"
              variant="primary"
              size="sm"
              onClick={handleStartNewAttempt}
              disabled={startAttemptMutation.isPending}
              className="bg-emerald-600 hover:bg-emerald-500 text-white rounded-2xl text-xs font-bold shrink-0 gap-1.5"
            >
              <RotateCcw className={cn('w-3.5 h-3.5', startAttemptMutation.isPending && 'animate-spin')} />
              {startAttemptMutation.isPending ? 'Đang tạo...' : 'Luyện lại toàn bộ bài học'}
            </Button>
          </div>
        </div>
      )}

      {/* Completion Banner (if all finished during InProgress) */}
      {isAttemptInProgress && isFinished && (
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

      {/* Exercise Active Box or Empty Filter State */}
      {visibleExercises.length === 0 ? (
        <div className="bg-white rounded-3xl border border-slate-200/90 p-8 sm:p-12 text-center space-y-4 shadow-2xs">
          <div className="w-12 h-12 rounded-2xl bg-slate-100 text-slate-500 mx-auto flex items-center justify-center">
            <Filter className="w-6 h-6" />
          </div>
          <div className="space-y-1">
            <h3 className="text-base font-bold text-slate-900">Không có câu phù hợp với bộ lọc</h3>
            <p className="text-xs text-slate-500 max-w-md mx-auto">
              {selectedStatus === 'correct' || selectedStatus === 'incorrect' || selectedStatus === 'partial'
                ? 'Bộ lọc đúng/sai chỉ áp dụng cho bài tập có đáp án chuẩn hoặc trắc nghiệm đóng. Các bài tự luận hoặc AI Rubric sẽ hiển thị ở bộ lọc "Tất cả".'
                : 'Không tìm thấy câu hỏi nào phù hợp với điều kiện lọc hiện tại.'}
            </p>
          </div>
          <div className="pt-2">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={handleResetFilters}
              className="rounded-2xl text-xs font-bold gap-1.5 cursor-pointer"
            >
              <RotateCcw className="w-3.5 h-3.5" />
              Đặt lại bộ lọc
            </Button>
          </div>
        </div>
      ) : (
        <div
          ref={exerciseContainerRef}
          className="bg-white rounded-3xl border border-slate-200/90 p-6 sm:p-8 shadow-2xs space-y-6"
        >
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
            key={`${attemptId}:${currentExercise.id}:${isRedoMode ? 'redo' : 'normal'}`}
            attemptId={attemptId}
            exercise={currentExercise}
            onSubmit={handleSubmitAnswer}
            result={currentResult}
            onRetry={handleRetry}
            onStartRedo={handleStartRedo}
            onCancelRedo={handleCancelRedo}
            isRedoMode={isRedoMode}
            isAttemptInProgress={isAttemptInProgress}
            isSubmitting={submitAnswerMutation.isPending}
            isRetrying={retryGradingMutation.isPending}
          />
        </div>
      )}

      {/* Footer Nav Controls */}
      <div className="flex items-center justify-between pt-2">
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={handlePrevious}
          disabled={!canGoPrevious}
          className="gap-1.5 text-xs rounded-2xl cursor-pointer"
        >
          <ChevronLeft className="w-4 h-4" /> Bài trước
          <kbd className="hidden sm:inline ml-1 text-[10px] bg-slate-100 text-slate-500 border border-slate-200 px-1.5 py-0.5 rounded font-mono">
            Alt+←
          </kbd>
        </Button>

        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={handleNext}
          disabled={!canGoNext}
          className="gap-1.5 text-xs rounded-2xl cursor-pointer"
        >
          Bài tiếp theo <ChevronRight className="w-4 h-4" />
          <kbd className="hidden sm:inline ml-1 text-[10px] bg-slate-100 text-slate-500 border border-slate-200 px-1.5 py-0.5 rounded font-mono">
            Alt+→
          </kbd>
        </Button>
      </div>
      <AiSpeechSettingsDialog
        isOpen={isSpeechSettingsOpen}
        onClose={() => setIsSpeechSettingsOpen(false)}
      />
      <KeyboardShortcutsDialog
        isOpen={isShortcutsHelpOpen}
        onClose={() => setIsShortcutsHelpOpen(false)}
      />
    </div>
  );
};
