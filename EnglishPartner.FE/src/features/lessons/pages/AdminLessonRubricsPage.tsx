import React, { useState } from 'react';
import { Award, Plus, Search, Eye, PowerOff, ShieldCheck } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog';
import {
  useLessonRubricsQuery,
  useCreateLessonRubricMutation,
  useDeactivateLessonRubricMutation,
} from '../hooks/useLessonQueries';
import { LessonRubricDetailModal } from '../components/rubrics/LessonRubricDetailModal';
import { CreateLessonRubricModal } from '../components/rubrics/CreateLessonRubricModal';
import { ExerciseType, EXERCISE_TYPE_CATALOG } from '../types/lessons.types';
import type { LessonRubricSummaryDto } from '../types/lessons.types';
import { toast } from '@/shared/lib/toast';

export const AdminLessonRubricsPage: React.FC = () => {
  const [exerciseTypeFilter, setExerciseTypeFilter] = useState<string>('');
  const [cefrFilter, setCefrFilter] = useState<string>('');
  const [selectedRubricId, setSelectedRubricId] = useState<string | null>(null);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [deactivateTarget, setDeactivateTarget] = useState<LessonRubricSummaryDto | null>(null);

  const { data: rubrics, isLoading, refetch } = useLessonRubricsQuery({
    exerciseType: exerciseTypeFilter || undefined,
    cefrLevel: cefrFilter || undefined,
  });

  const createRubricMutation = useCreateLessonRubricMutation();
  const deactivateRubricMutation = useDeactivateLessonRubricMutation();

  const handleCreateRubric = async (data: any) => {
    try {
      await createRubricMutation.mutateAsync(data);
      toast.success('Đã tạo mới Rubric đánh giá thành công!');
      setIsCreateOpen(false);
      void refetch();
    } catch {
      toast.error('Lỗi khi tạo Rubric mới.');
    }
  };

  const handleConfirmDeactivate = async () => {
    if (!deactivateTarget) return;
    try {
      await deactivateRubricMutation.mutateAsync(deactivateTarget.id);
      toast.success(`Đã vô hiệu hóa rubric "${deactivateTarget.name}".`);
      setDeactivateTarget(null);
      void refetch();
    } catch {
      toast.error('Lỗi khi vô hiệu hóa rubric.');
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 border-b border-slate-200/80 pb-5">
        <div>
          <h1 className="text-xl sm:text-2xl font-black text-slate-900 tracking-tight flex items-center gap-2.5">
            <Award className="w-6 h-6 text-emerald-600" />
            Quản lý Rubrics Đánh giá Bài tập (Lesson Rubrics)
          </h1>
          <p className="text-xs sm:text-sm text-slate-500 mt-1">
            Thiết lập tiêu chuẩn chấm điểm AI (CEFR Descriptors 0–4) cho kỹ năng Viết, Nói, Dịch và Hội thoại.
          </p>
        </div>

        <Button
          type="button"
          variant="primary"
          onClick={() => setIsCreateOpen(true)}
          className="gap-2 shadow-xs"
        >
          <Plus className="w-4 h-4" /> Tạo Rubric mới
        </Button>
      </div>

      {/* Filter Bar */}
      <div className="bg-white p-4 rounded-2xl border border-slate-200/80 shadow-2xs flex flex-wrap items-center gap-3">
        <select
          value={exerciseTypeFilter}
          onChange={(e) => setExerciseTypeFilter(e.target.value)}
          className="text-xs rounded-xl border border-slate-200 px-3 py-2 bg-slate-50 focus:bg-white focus:ring-2 focus:ring-emerald-500"
        >
          <option value="">Tất cả loại bài tập</option>
          {Object.values(ExerciseType).map((t) => (
            <option key={t as string} value={t as string}>
              {EXERCISE_TYPE_CATALOG[t as ExerciseType]?.labelVi ?? (t as string)}
            </option>
          ))}
        </select>

        <select
          value={cefrFilter}
          onChange={(e) => setCefrFilter(e.target.value)}
          className="text-xs rounded-xl border border-slate-200 px-3 py-2 bg-slate-50 focus:bg-white focus:ring-2 focus:ring-emerald-500"
        >
          <option value="">Tất cả cấp độ CEFR</option>
          <option value="All">All</option>
          {['A1', 'A2', 'B1', 'B2', 'C1', 'C2'].map((lvl) => (
            <option key={lvl} value={lvl}>
              Level {lvl}
            </option>
          ))}
        </select>
      </div>

      {/* Rubrics Grid */}
      {isLoading ? (
        <div className="text-center py-12 text-xs font-semibold text-slate-500">
          Đang tải danh sách rubric...
        </div>
      ) : !rubrics || rubrics.length === 0 ? (
        <div className="bg-white rounded-3xl border border-slate-200 p-12 text-center space-y-3">
          <Award className="w-10 h-10 text-slate-300 mx-auto" />
          <h3 className="text-sm font-bold text-slate-800">Chưa có Rubric nào khớp với bộ lọc</h3>
          <p className="text-xs text-slate-500 max-w-sm mx-auto">
            Hệ thống đã nạp sẵn 10 bộ rubric chuẩn. Hãy điều chỉnh bộ lọc để xem.
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          {rubrics.map((r: LessonRubricSummaryDto) => (
            <div
              key={r.id}
              className="bg-white rounded-3xl border border-slate-200/90 p-5 shadow-2xs hover:shadow-sm transition-all flex flex-col justify-between space-y-4"
            >
              <div className="space-y-3">
                <div className="flex items-center justify-between gap-2">
                  <span className="text-xs font-mono font-bold text-emerald-800 bg-emerald-50 px-2.5 py-1 rounded-xl border border-emerald-200">
                    {r.code}
                  </span>

                  <Badge variant={r.isActive ? 'emerald' : 'default'}>
                    {r.isActive ? 'Hoạt động' : 'Vô hiệu hóa'}
                  </Badge>
                </div>

                <div>
                  <h3 className="text-base font-bold text-slate-900 line-clamp-1">{r.name}</h3>
                  <p className="text-xs text-slate-500 mt-0.5">
                    Áp dụng: <strong className="text-slate-700">{r.exerciseType ?? 'Toàn bộ'}</strong> • CEFR:{' '}
                    <strong className="text-emerald-700">{r.cefrLevel}</strong>
                  </p>
                </div>

                {r.description && (
                  <p className="text-xs text-slate-600 line-clamp-2 leading-relaxed">
                    {r.description}
                  </p>
                )}

                <div className="pt-2 border-t border-slate-100 flex items-center justify-between text-xs text-slate-500 font-medium">
                  <span>{r.criteriaCount} tiêu chí đánh giá</span>
                  <span className="font-mono">Phiên bản v{r.version}</span>
                </div>
              </div>

              {/* Actions */}
              <div className="pt-2 border-t border-slate-100 flex items-center justify-between gap-2">
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => setSelectedRubricId(r.id)}
                  className="gap-1.5 text-xs flex-1"
                >
                  <Eye className="w-3.5 h-3.5" /> Xem chi tiết 0–4
                </Button>

                {r.isActive && (
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => setDeactivateTarget(r)}
                    disabled={deactivateRubricMutation.isPending}
                    className="text-xs text-rose-600 border-rose-200 hover:bg-rose-50"
                  >
                    <PowerOff className="w-3.5 h-3.5" />
                  </Button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Rubric Detail Modal */}
      <LessonRubricDetailModal
        isOpen={Boolean(selectedRubricId)}
        onClose={() => setSelectedRubricId(null)}
        rubricId={selectedRubricId}
      />

      {/* Create Rubric Modal */}
      <CreateLessonRubricModal
        isOpen={isCreateOpen}
        onClose={() => setIsCreateOpen(false)}
        onSubmit={handleCreateRubric}
        isSubmitting={createRubricMutation.isPending}
      />

      {/* Deactivate Rubric Confirmation Dialog */}
      <ConfirmDialog
        isOpen={Boolean(deactivateTarget)}
        onClose={() => setDeactivateTarget(null)}
        onConfirm={() => void handleConfirmDeactivate()}
        isLoading={deactivateRubricMutation.isPending}
        title="Vô hiệu hóa Rubric đánh giá"
        targetName={deactivateTarget ? `${deactivateTarget.name} (${deactivateTarget.code})` : ''}
        targetLabel="Rubric bị vô hiệu hóa:"
        description="Sau khi vô hiệu hóa, rubric này sẽ không hiển thị khi chọn tiêu chí chấm cho bài tập mới. Các bài học đã ghim rubric này sẽ không bị ảnh hưởng."
        confirmLabel="Vô hiệu hóa"
        variant="danger"
        confirmIcon={<PowerOff className="w-4 h-4" />}
      />
    </div>
  );
};
