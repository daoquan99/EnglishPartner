import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  ArrowLeft,
  Play,
  Clock,
  Award,
  Layers,
  BookOpen,
  CheckCircle,
  Sparkles,
} from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import {
  useLearnerLessonDetailQuery,
  useStartAttemptMutation,
} from '../hooks/useLessonQueries';
import { EXERCISE_TYPE_CATALOG } from '../types/lessons.types';
import type { ExerciseLearnerDto } from '../types/lessons.types';
import { toast } from '@/shared/lib/toast';

export const StudentLessonDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const { data: lesson, isLoading } = useLearnerLessonDetailQuery(id);
  const startAttemptMutation = useStartAttemptMutation();

  if (isLoading) {
    return (
      <div className="text-center py-16 text-xs font-semibold text-slate-500">
        Đang tải thông tin bài học...
      </div>
    );
  }

  if (!lesson || !id) {
    return (
      <div className="text-center py-16 space-y-3">
        <p className="text-sm font-bold text-slate-800">Không tìm thấy bài học này.</p>
        <Button type="button" variant="outline" size="sm" onClick={() => navigate('/student/lessons')}>
          Quay lại danh sách
        </Button>
      </div>
    );
  }

  const handleStart = async () => {
    try {
      const res = await startAttemptMutation.mutateAsync(id);
      toast.success('Bắt đầu lượt học mới!');
      navigate(`/student/lesson-attempts/${res.attemptId}`);
    } catch {
      toast.error('Lỗi khi khởi tạo lượt làm bài.');
    }
  };

  const exercises = lesson.exercises ?? [];
  const totalPoints = exercises.reduce((sum: number, e: ExerciseLearnerDto) => sum + (e.maxPoints || 0), 0);

  return (
    <div className="max-w-4xl mx-auto space-y-6 px-4 py-2">
      {/* Back button & Action */}
      <div className="flex items-center justify-between">
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => navigate('/student/lessons')}
          className="rounded-full gap-1.5 text-xs text-slate-600"
        >
          <ArrowLeft className="w-4 h-4" /> Danh sách bài học
        </Button>

        <Button
          type="button"
          variant="primary"
          onClick={handleStart}
          disabled={startAttemptMutation.isPending || exercises.length === 0}
          className="rounded-2xl gap-2 shadow-xs px-6 py-2.5 text-sm"
        >
          <Play className="w-4 h-4 fill-current" />
          {startAttemptMutation.isPending ? 'Đang khởi tạo...' : 'Bắt đầu làm bài'}
        </Button>
      </div>

      {/* Hero Overview Card */}
      <div className="bg-white rounded-3xl border border-slate-200/90 p-6 sm:p-8 shadow-2xs space-y-5">
        <div className="flex flex-wrap items-center gap-3">
          <Badge variant="outline" className="font-extrabold text-emerald-700 bg-emerald-50 border-emerald-200 text-sm">
            CEFR {lesson.cefrLevel}
          </Badge>

          <div className="flex items-center gap-1.5 text-xs text-slate-500 font-medium">
            <Clock className="w-4 h-4" />
            Thời lượng: <strong>{lesson.estimatedMinutes} phút</strong>
          </div>

          <div className="flex items-center gap-1.5 text-xs text-slate-500 font-medium">
            <Layers className="w-4 h-4" />
            Cấu trúc: <strong>{exercises.length} bài tập</strong>
          </div>

          <div className="flex items-center gap-1.5 text-xs text-emerald-700 font-bold">
            <Award className="w-4 h-4 text-emerald-600" />
            Tổng điểm: <strong>{totalPoints} điểm</strong>
          </div>
        </div>

        <div>
          <h1 className="text-2xl sm:text-3xl font-black text-slate-900 tracking-tight leading-snug">
            {lesson.title}
          </h1>
          <p className="text-sm font-semibold text-slate-500 mt-1">
            Chủ đề: <strong className="text-slate-800">{lesson.topic}</strong>
          </p>
        </div>

        {lesson.content && (
          <p className="text-sm text-slate-700 leading-relaxed bg-slate-50/80 p-4 rounded-2xl border border-slate-100 font-medium">
            {lesson.content}
          </p>
        )}

        {/* Objectives & Targets */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 pt-2">
          {lesson.learningObjectives?.length > 0 && (
            <div className="p-4 bg-emerald-50/40 border border-emerald-100 rounded-2xl space-y-1.5">
              <span className="text-xs font-bold text-emerald-900 block flex items-center gap-1">
                <CheckCircle className="w-3.5 h-3.5 text-emerald-600" /> Mục tiêu bài học
              </span>
              <ul className="text-xs text-emerald-800 space-y-1 list-disc list-inside">
                {lesson.learningObjectives.map((obj: string, i: number) => (
                  <li key={i}>{obj}</li>
                ))}
              </ul>
            </div>
          )}

          {lesson.targetVocabulary?.length > 0 && (
            <div className="p-4 bg-sky-50/40 border border-sky-100 rounded-2xl space-y-1.5">
              <span className="text-xs font-bold text-sky-900 block flex items-center gap-1">
                <Sparkles className="w-3.5 h-3.5 text-sky-600" /> Từ vựng trọng tâm
              </span>
              <div className="flex flex-wrap gap-1">
                {lesson.targetVocabulary.map((v: string, i: number) => (
                  <span
                    key={i}
                    className="text-[11px] font-medium bg-white px-2 py-0.5 rounded-lg border border-sky-200 text-sky-800"
                  >
                    {v}
                  </span>
                ))}
              </div>
            </div>
          )}

          {lesson.targetGrammar?.length > 0 && (
            <div className="p-4 bg-amber-50/40 border border-amber-100 rounded-2xl space-y-1.5">
              <span className="text-xs font-bold text-amber-900 block flex items-center gap-1">
                <BookOpen className="w-3.5 h-3.5 text-amber-600" /> Ngữ pháp trọng tâm
              </span>
              <div className="flex flex-wrap gap-1">
                {lesson.targetGrammar.map((g: string, i: number) => (
                  <span
                    key={i}
                    className="text-[11px] font-medium bg-white px-2 py-0.5 rounded-lg border border-amber-200 text-amber-800"
                  >
                    {g}
                  </span>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Exercises Outline */}
      <div className="space-y-3">
        <h3 className="text-sm font-bold text-slate-800 uppercase tracking-wider flex items-center gap-2">
          <Layers className="w-4 h-4 text-emerald-600" />
          Các phần bài tập trong bài học ({exercises.length}):
        </h3>

        <div className="space-y-2.5">
          {exercises.map((ex: ExerciseLearnerDto, idx: number) => {
            const meta = EXERCISE_TYPE_CATALOG[ex.type];
            return (
              <div
                key={ex.id}
                className="bg-white p-4 rounded-2xl border border-slate-200/90 shadow-2xs flex items-center justify-between gap-4"
              >
                <div className="flex items-center gap-3 min-w-0">
                  <span className="w-7 h-7 rounded-xl bg-slate-100 text-slate-600 font-mono font-bold text-xs flex items-center justify-center shrink-0">
                    {idx + 1}
                  </span>

                  <div className="min-w-0">
                    <div className="text-sm font-bold text-slate-900 truncate">
                      {ex.title}
                    </div>
                    <div className="text-xs text-slate-500 truncate">
                      {meta?.labelVi ?? ex.type} • {ex.instructions}
                    </div>
                  </div>
                </div>

                <div className="flex items-center gap-2 shrink-0">
                  <Badge variant="outline" className="text-emerald-700 font-bold">
                    {ex.maxPoints} đ
                  </Badge>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
};
