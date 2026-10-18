import React from 'react';
import { useNavigate } from 'react-router-dom';
import { History, ArrowLeft, ArrowRight, Award, CheckCircle2, Clock } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { useLearnerAttemptsQuery } from '../hooks/useLessonQueries';
import type { LessonAttemptSummaryDto } from '../types/lessons.types';

export const StudentLessonHistoryPage: React.FC = () => {
  const navigate = useNavigate();
  const { data: attempts, isLoading } = useLearnerAttemptsQuery();

  return (
    <div className="max-w-4xl mx-auto space-y-6 px-4 py-2">
      {/* Header */}
      <div className="flex items-center justify-between border-b border-slate-200/80 pb-5">
        <div className="flex items-center gap-3">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => navigate('/student/lessons')}
            className="rounded-full w-9 h-9 p-0 text-slate-600"
          >
            <ArrowLeft className="w-4 h-4" />
          </Button>

          <div>
            <h1 className="text-xl sm:text-2xl font-black text-slate-900 tracking-tight flex items-center gap-2">
              <History className="w-6 h-6 text-emerald-600" />
              Lịch sử Làm bài & Đánh giá
            </h1>
            <p className="text-xs sm:text-sm text-slate-500 mt-0.5">
              Xem lại quá trình làm bài, điểm số tích lũy và đánh giá chi tiết theo từng bài học.
            </p>
          </div>
        </div>
      </div>

      {/* Attempts List */}
      {isLoading ? (
        <div className="text-center py-16 text-xs font-semibold text-slate-500">
          Đang tải lịch sử làm bài...
        </div>
      ) : !attempts || attempts.length === 0 ? (
        <div className="bg-white rounded-3xl border border-slate-200 p-12 text-center space-y-3">
          <History className="w-10 h-10 text-slate-300 mx-auto" />
          <h3 className="text-sm font-bold text-slate-800">Bạn chưa có lượt làm bài nào</h3>
          <p className="text-xs text-slate-500 max-w-sm mx-auto">
            Hãy bắt đầu một bài học từ danh sách để luyện tập và nhận đánh giá từ AI.
          </p>
          <Button
            type="button"
            variant="primary"
            size="sm"
            onClick={() => navigate('/student/lessons')}
            className="gap-1.5"
          >
            Khám phá bài học ngay
          </Button>
        </div>
      ) : (
        <div className="space-y-3.5">
          {attempts.map((att: LessonAttemptSummaryDto) => {
            const isCompleted = att.status === 'Completed';

            return (
              <div
                key={att.attemptId}
                className="bg-white p-5 rounded-3xl border border-slate-200/90 shadow-2xs hover:shadow-xs transition-shadow flex flex-col sm:flex-row sm:items-center justify-between gap-4"
              >
                <div className="space-y-1.5 min-w-0">
                  <div className="flex items-center gap-2">
                    <h3 className="text-base font-bold text-slate-900 truncate">
                      {att.lessonTitle}
                    </h3>
                    <span className="text-[11px] font-mono font-bold text-slate-400">
                      v{att.pinnedLessonVersion}
                    </span>
                    <Badge variant={isCompleted ? 'emerald' : 'amber'}>
                      {isCompleted ? 'Đã hoàn thành' : 'Đang làm dở'}
                    </Badge>
                  </div>

                  <div className="flex items-center gap-3 text-xs text-slate-500">
                    <span className="flex items-center gap-1">
                      <Clock className="w-3.5 h-3.5" />
                      Bắt đầu: {new Date(att.startedAt).toLocaleDateString('vi-VN')}
                    </span>
                    {att.completedAt && (
                      <span>
                        • Hoàn thành: {new Date(att.completedAt).toLocaleDateString('vi-VN')}
                      </span>
                    )}
                  </div>
                </div>

                <div className="flex items-center gap-4 shrink-0">
                  <div className="text-right">
                    <div className="text-sm font-black text-emerald-700 flex items-center justify-end gap-1">
                      <Award className="w-4 h-4 text-emerald-600" />
                      {att.currentSubtotalPoints} / {att.totalMaxPoints} đ
                    </div>
                    {att.finalScore100 !== null && att.finalScore100 !== undefined && (
                      <span className="text-xs font-bold text-slate-500">
                        {att.finalScore100}%
                      </span>
                    )}
                  </div>

                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => navigate(`/student/lesson-attempts/${att.attemptId}`)}
                    className="gap-1.5 text-xs rounded-2xl"
                  >
                    {isCompleted ? 'Xem lại bài' : 'Làm tiếp'}
                    <ArrowRight className="w-3.5 h-3.5" />
                  </Button>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};
