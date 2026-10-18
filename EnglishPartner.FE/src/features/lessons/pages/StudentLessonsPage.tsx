import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { BookOpen, Search, Clock, Award, Layers, ArrowRight, History } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { useLearnerLessonsQuery } from '../hooks/useLessonQueries';
import type { LessonSummaryDto } from '../types/lessons.types';

export const StudentLessonsPage: React.FC = () => {
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [cefrFilter, setCefrFilter] = useState('');

  const { data: lessons, isLoading } = useLearnerLessonsQuery({
    search: search.trim() || undefined,
    cefrLevel: cefrFilter || undefined,
  });

  return (
    <div className="space-y-6 max-w-6xl mx-auto px-4 py-2">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 border-b border-slate-200/80 pb-5">
        <div>
          <h1 className="text-xl sm:text-2xl font-black text-slate-900 tracking-tight flex items-center gap-2.5">
            <BookOpen className="w-6 h-6 text-emerald-600" />
            Khám phá Bài học & Luyện tập
          </h1>
          <p className="text-xs sm:text-sm text-slate-500 mt-1">
            Học tập toàn diện qua 16 loại bài tập đa kỹ năng: Từ vựng, Ngữ pháp, Đọc, Nghe, Nói và Viết.
          </p>
        </div>

        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => navigate('/student/lesson-history')}
          className="gap-2 self-start sm:self-center text-xs text-slate-700"
        >
          <History className="w-4 h-4 text-emerald-600" /> Lịch sử làm bài của tôi
        </Button>
      </div>

      {/* Search & Filter */}
      <div className="bg-white p-4 rounded-3xl border border-slate-200/80 shadow-2xs flex flex-wrap items-center justify-between gap-3">
        <div className="relative flex-1 min-w-[220px]">
          <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
          <input
            type="text"
            placeholder="Tìm bài học theo tiêu đề..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full text-xs rounded-xl border border-slate-200 pl-9 pr-3 py-2.5 bg-slate-50 focus:bg-white focus:outline-none focus:ring-2 focus:ring-emerald-500"
          />
        </div>

        <select
          value={cefrFilter}
          onChange={(e) => setCefrFilter(e.target.value)}
          className="text-xs font-semibold rounded-xl border border-slate-200 px-3 py-2.5 bg-slate-50 focus:bg-white focus:ring-2 focus:ring-emerald-500"
        >
          <option value="">Tất cả trình độ CEFR</option>
          {['A1', 'A2', 'B1', 'B2', 'C1', 'C2'].map((lvl) => (
            <option key={lvl} value={lvl}>
              Trình độ {lvl}
            </option>
          ))}
        </select>
      </div>

      {/* Lessons Grid */}
      {isLoading ? (
        <div className="text-center py-16 text-xs font-semibold text-slate-500">
          Đang tải danh sách bài học...
        </div>
      ) : !lessons || lessons.length === 0 ? (
        <div className="bg-white rounded-3xl border border-slate-200 p-12 text-center space-y-3">
          <BookOpen className="w-10 h-10 text-slate-300 mx-auto" />
          <h3 className="text-sm font-bold text-slate-800">Không có bài học nào được tìm thấy</h3>
          <p className="text-xs text-slate-500 max-w-sm mx-auto">
            Hiện chưa có bài học nào khớp với tiêu chuẩn tìm kiếm của bạn. Hãy thử thay đổi bộ lọc.
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {lessons.map((lesson: LessonSummaryDto) => (
            <div
              key={lesson.id}
              onClick={() => navigate(`/student/lessons/${lesson.id}`)}
              className="bg-white rounded-3xl border border-slate-200/90 p-6 shadow-2xs hover:shadow-md hover:border-emerald-500/50 transition-all cursor-pointer flex flex-col justify-between space-y-4 group"
            >
              <div className="space-y-3">
                <div className="flex items-center justify-between">
                  <Badge variant="outline" className="font-extrabold text-emerald-700 bg-emerald-50 border-emerald-200">
                    CEFR {lesson.cefrLevel}
                  </Badge>

                  <div className="flex items-center gap-1 text-xs text-slate-500 font-medium">
                    <Clock className="w-3.5 h-3.5" />
                    {lesson.estimatedMinutes} phút
                  </div>
                </div>

                <div>
                  <h3 className="text-base font-bold text-slate-900 group-hover:text-emerald-700 transition-colors line-clamp-1">
                    {lesson.title}
                  </h3>
                  <p className="text-xs text-slate-500 mt-1 line-clamp-1">
                    Chủ đề: <strong className="text-slate-700">{lesson.topic}</strong>
                  </p>
                </div>

                {lesson.content && (
                  <p className="text-xs text-slate-600 line-clamp-2 leading-relaxed">
                    {lesson.content}
                  </p>
                )}

                <div className="pt-2 border-t border-slate-100 flex items-center justify-between text-xs font-semibold text-slate-600">
                  <span className="flex items-center gap-1">
                    <Layers className="w-3.5 h-3.5 text-slate-400" />
                    {lesson.exerciseCount} bài tập
                  </span>
                  <span className="flex items-center gap-1 text-emerald-700 font-bold">
                    <Award className="w-3.5 h-3.5 text-emerald-600" />
                    {lesson.totalMaxPoints} điểm
                  </span>
                </div>
              </div>

              <div className="pt-2 border-t border-slate-100 flex items-center justify-between text-xs font-bold text-emerald-700 group-hover:translate-x-1 transition-transform">
                <span>Khám phá bài học</span>
                <ArrowRight className="w-4 h-4" />
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
