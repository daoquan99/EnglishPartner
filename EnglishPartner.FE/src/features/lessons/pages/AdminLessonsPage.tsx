import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Plus, Search, BookOpen, Layers, Award, Sparkles, CheckCircle2, FileText, ArrowRight, Trash2, RotateCcw } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog';
import { CreateLessonModal } from '../components/builder/CreateLessonModal';
import { AiLessonExerciseGeneratorModal } from '../components/builder/AiLessonExerciseGeneratorModal';
import {
  useAdminLessonsQuery,
  useCreateLessonDraftMutation,
  usePublishLessonMutation,
  useRevertLessonToDraftMutation,
  useCreateLessonRevisionMutation,
  useDeleteLessonDraftMutation,
} from '../hooks/useLessonQueries';
import type { CefrLevel, LessonSummaryDto, CreateLessonDraftRequest } from '../types/lessons.types';
import { toast } from '@/shared/lib/toast';

export const AdminLessonsPage: React.FC = () => {
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [cefrFilter, setCefrFilter] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [pageNumber, setPageNumber] = useState(1);
  const pageSize = 12;

  // Dialog states
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [isAiGeneratorOpen, setIsAiGeneratorOpen] = useState(false);
  const [publishTarget, setPublishTarget] = useState<LessonSummaryDto | null>(null);
  const [revertTarget, setRevertTarget] = useState<LessonSummaryDto | null>(null);
  const [revisionTarget, setRevisionTarget] = useState<LessonSummaryDto | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<LessonSummaryDto | null>(null);

  const { data: lessons, isLoading, refetch } = useAdminLessonsQuery({
    search: search.trim() || undefined,
    cefrLevel: cefrFilter || undefined,
    status: statusFilter || undefined,
    pageNumber,
    pageSize,
  });

  const createDraftMutation = useCreateLessonDraftMutation();
  const publishMutation = usePublishLessonMutation();
  const revertDraftMutation = useRevertLessonToDraftMutation();
  const revisionMutation = useCreateLessonRevisionMutation();
  const deleteDraftMutation = useDeleteLessonDraftMutation();

  const handleCreateLessonSubmit = async (data: CreateLessonDraftRequest) => {
    try {
      const res = await createDraftMutation.mutateAsync(data);
      toast.success('Đã tạo bản thảo bài học mới thành công!');
      setIsCreateOpen(false);
      navigate(`/admin/lessons/${res.lessonId}`);
    } catch {
      toast.error('Lỗi khi tạo bản thảo bài học.');
    }
  };

  const handleConfirmPublish = async () => {
    if (!publishTarget) return;
    try {
      await publishMutation.mutateAsync({
        id: publishTarget.id,
        rowVersion: publishTarget.rowVersion,
      });
      toast.success(`Đã xuất bản bài học "${publishTarget.title}" thành công!`);
      setPublishTarget(null);
      void refetch();
    } catch {
      toast.error('Không thể xuất bản bài học (kiểm tra lại số lượng bài tập và media).');
    }
  };

  const handleConfirmRevert = async () => {
    if (!revertTarget) return;
    try {
      await revertDraftMutation.mutateAsync(revertTarget.id);
      toast.success(`Đã chuyển bài học "${revertTarget.title}" về bản thảo! Bây giờ bạn có thể tiếp tục thêm/sửa bài tập.`);
      setRevertTarget(null);
      void refetch();
    } catch {
      toast.error('Không thể chuyển bài học về bản thảo.');
    }
  };

  const handleConfirmRevision = async () => {
    if (!revisionTarget) return;
    try {
      const res = await revisionMutation.mutateAsync(revisionTarget.id);
      toast.success(`Đã tạo bản sửa đổi mới từ bài học "${revisionTarget.title}"!`);
      setRevisionTarget(null);
      navigate(`/admin/lessons/${res.draftLessonId}`);
    } catch {
      toast.error('Không thể tạo bản sửa đổi mới.');
    }
  };

  const handleConfirmDelete = async () => {
    if (!deleteTarget) return;
    try {
      await deleteDraftMutation.mutateAsync(deleteTarget.id);
      toast.success(`Đã xóa bài học "${deleteTarget.title}" thành công!`);
      setDeleteTarget(null);
      void refetch();
    } catch {
      toast.error('Không thể xóa bài học này.');
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 border-b border-slate-200/80 pb-5">
        <div>
          <h1 className="text-xl sm:text-2xl font-black text-slate-900 tracking-tight flex items-center gap-2.5">
            <BookOpen className="w-6 h-6 text-emerald-600" />
            Quản trị Giáo án & Bài học (Lesson Builder)
          </h1>
          <p className="text-xs sm:text-sm text-slate-500 mt-1">
            Xây dựng và quản lý các bài học tích hợp đầy đủ 16 loại bài tập tương tác chuẩn CEFR.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Button
            type="button"
            variant="outline"
            onClick={() => setIsAiGeneratorOpen(true)}
            className="gap-2 shadow-2xs text-amber-700 border-amber-200 hover:border-amber-400 hover:bg-amber-50 font-semibold"
          >
            <Sparkles className="w-4 h-4 text-amber-500" />
            Tạo bài học bằng AI
          </Button>

          <Button
            type="button"
            variant="primary"
            onClick={() => setIsCreateOpen(true)}
            className="gap-2 shadow-xs font-semibold"
          >
            <Plus className="w-4 h-4" />
            Tạo bài học mới
          </Button>
        </div>
      </div>

      {/* Filters & Search */}
      <div className="bg-white p-4 rounded-2xl border border-slate-200/80 shadow-2xs flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2 flex-1 min-w-[240px]">
          <div className="relative flex-1">
            <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              placeholder="Tìm kiếm theo tiêu đề bài học..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="w-full text-xs rounded-xl border border-slate-200 pl-9 pr-3 py-2 bg-slate-50 focus:bg-white focus:outline-none focus:ring-2 focus:ring-emerald-500"
            />
          </div>
        </div>

        <div className="flex items-center gap-2 flex-wrap">
          <select
            value={cefrFilter}
            onChange={(e) => setCefrFilter(e.target.value)}
            className="text-xs rounded-xl border border-slate-200 px-3 py-2 bg-slate-50 focus:bg-white focus:ring-2 focus:ring-emerald-500"
          >
            <option value="">Tất cả CEFR</option>
            {['A1', 'A2', 'B1', 'B2', 'C1', 'C2'].map((lvl) => (
              <option key={lvl} value={lvl}>
                Level {lvl}
              </option>
            ))}
          </select>

          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            className="text-xs rounded-xl border border-slate-200 px-3 py-2 bg-slate-50 focus:bg-white focus:ring-2 focus:ring-emerald-500"
          >
            <option value="">Tất cả trạng thái</option>
            <option value="Draft">Bản thảo (Draft)</option>
            <option value="Published">Đã xuất bản (Published)</option>
            <option value="Archived">Đã lưu trữ (Archived)</option>
          </select>
        </div>
      </div>

      {/* Lessons List Grid */}
      {isLoading ? (
        <div className="text-center py-12 text-xs font-semibold text-slate-500">
          Đang tải danh sách bài học...
        </div>
      ) : !lessons || lessons.length === 0 ? (
        <div className="bg-white rounded-3xl border border-slate-200 p-12 text-center space-y-3">
          <BookOpen className="w-10 h-10 text-slate-300 mx-auto" />
          <h3 className="text-sm font-bold text-slate-800">Không tìm thấy bài học nào</h3>
          <p className="text-xs text-slate-500 max-w-sm mx-auto">
            Chưa có bài học nào khớp với bộ lọc. Hãy tạo bài học đầu tiên hoặc điều chỉnh tiêu chí tìm kiếm.
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          {lessons.map((lesson: LessonSummaryDto) => {
            const isDraft = lesson.status === 'Draft';
            const isPublished = lesson.status === 'Published';

            return (
              <div
                key={lesson.id}
                className="bg-white rounded-3xl border border-slate-200/90 p-5 shadow-2xs hover:shadow-sm transition-all flex flex-col justify-between space-y-4"
              >
                <div className="space-y-3">
                  <div className="flex items-center justify-between gap-2">
                    <div className="flex items-center gap-1.5">
                      <Badge variant="outline" className="font-extrabold text-emerald-700 bg-emerald-50 border-emerald-200">
                        {lesson.cefrLevel}
                      </Badge>
                      <span className="text-[11px] font-bold text-slate-500 font-mono">
                        v{lesson.version}
                      </span>
                    </div>

                    <Badge
                      variant={isPublished ? 'emerald' : isDraft ? 'amber' : 'default'}
                    >
                      {isPublished ? 'Đã xuất bản' : isDraft ? 'Bản thảo' : 'Đã lưu trữ'}
                    </Badge>
                  </div>

                  <div>
                    <h3 className="text-base font-bold text-slate-900 line-clamp-1 hover:text-emerald-700 transition-colors">
                      {lesson.title}
                    </h3>
                    <p className="text-xs text-slate-500 mt-0.5 line-clamp-1">
                      Chủ đề: <strong className="text-slate-700">{lesson.topic}</strong>
                    </p>
                  </div>

                  {lesson.content && (
                    <p className="text-xs text-slate-600 line-clamp-2 leading-relaxed">
                      {lesson.content}
                    </p>
                  )}

                  <div className="flex items-center gap-4 text-xs font-medium text-slate-500 pt-2 border-t border-slate-100">
                    <span className="flex items-center gap-1">
                      <Layers className="w-3.5 h-3.5 text-slate-400" />
                      {lesson.exerciseCount} bài tập
                    </span>
                    <span className="flex items-center gap-1">
                      <Award className="w-3.5 h-3.5 text-emerald-600" />
                      {lesson.totalMaxPoints} điểm
                    </span>
                  </div>
                </div>

                {/* Card Actions */}
                <div className="pt-2 border-t border-slate-100 flex items-center justify-between gap-2">
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => navigate(`/admin/lessons/${lesson.id}`)}
                    className="gap-1.5 text-xs flex-1"
                  >
                    Xem & Sửa <ArrowRight className="w-3.5 h-3.5" />
                  </Button>

                  {isDraft && (
                    <>
                      <Button
                        type="button"
                        variant="primary"
                        size="sm"
                        onClick={() => setPublishTarget(lesson)}
                        disabled={publishMutation.isPending || lesson.exerciseCount === 0}
                        className="gap-1 text-xs font-semibold"
                        title={lesson.exerciseCount === 0 ? 'Cần có ít nhất 1 bài tập trước khi xuất bản' : 'Xuất bản bài học cho học viên'}
                      >
                        <CheckCircle2 className="w-3.5 h-3.5" /> Xuất bản
                      </Button>

                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        onClick={() => setDeleteTarget(lesson)}
                        disabled={deleteDraftMutation.isPending}
                        className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 border-slate-200 hover:border-rose-200"
                        title="Xóa bản thảo bài học"
                      >
                        <Trash2 className="w-4 h-4" />
                      </Button>
                    </>
                  )}

                  {isPublished && (
                    <div className="flex items-center gap-1.5">
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        onClick={() => setRevertTarget(lesson)}
                        disabled={revertDraftMutation.isPending}
                        className="gap-1 text-xs text-amber-700 hover:text-amber-800 hover:bg-amber-50 border-amber-300 font-semibold"
                        title="Chuyển về bản thảo để tiếp tục thêm và sửa bài tập"
                      >
                        <RotateCcw className="w-3.5 h-3.5" /> Về bản thảo
                      </Button>

                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        onClick={() => setRevisionTarget(lesson)}
                        disabled={revisionMutation.isPending}
                        className="text-xs text-slate-700 font-semibold"
                        title="Tạo bản sửa đổi mới"
                      >
                        Tạo bản sửa
                      </Button>
                    </div>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Create Lesson Modal */}
      <CreateLessonModal
        isOpen={isCreateOpen}
        onClose={() => setIsCreateOpen(false)}
        onSubmit={handleCreateLessonSubmit}
        isSubmitting={createDraftMutation.isPending}
      />

      {/* AI Lesson Generator Modal */}
      <AiLessonExerciseGeneratorModal
        isOpen={isAiGeneratorOpen}
        onClose={() => setIsAiGeneratorOpen(false)}
        onSuccess={() => void refetch()}
      />

      {/* Delete Lesson Confirmation Dialog */}
      <ConfirmDialog
        isOpen={Boolean(deleteTarget)}
        onClose={() => setDeleteTarget(null)}
        onConfirm={() => void handleConfirmDelete()}
        isLoading={deleteDraftMutation.isPending}
        variant="danger"
        title="Xác nhận xóa bài học"
        targetName={deleteTarget ? `${deleteTarget.title} (v${deleteTarget.version})` : ''}
        targetLabel="Bài học cần xóa:"
        description="Bạn có chắc chắn muốn xóa bài học này? Toàn bộ bài tập và dữ liệu liên quan sẽ bị xóa vĩnh viễn và không thể khôi phục."
        confirmLabel="Xóa bài học"
      />

      {/* Publish Lesson Confirmation Dialog */}
      <ConfirmDialog
        isOpen={Boolean(publishTarget)}
        onClose={() => setPublishTarget(null)}
        onConfirm={() => void handleConfirmPublish()}
        isLoading={publishMutation.isPending}
        variant="primary"
        title="Xác nhận xuất bản bài học"
        targetName={publishTarget ? `${publishTarget.title} (v${publishTarget.version})` : ''}
        targetLabel="Giáo án xuất bản:"
        description={`Giáo án này gồm ${publishTarget?.exerciseCount} bài tập (${publishTarget?.totalMaxPoints} điểm). Sau khi xuất bản, học viên có thể học và làm bài ngay lập tức.`}
        confirmLabel="Xuất bản ngay"
        confirmIcon={<CheckCircle2 className="w-4 h-4" />}
      />

      {/* Create Revision Confirmation Dialog */}
      <ConfirmDialog
        isOpen={Boolean(revisionTarget)}
        onClose={() => setRevisionTarget(null)}
        onConfirm={() => void handleConfirmRevision()}
        isLoading={revisionMutation.isPending}
        variant="primary"
        title="Tạo bản sửa đổi mới (Draft Revision)"
        targetName={revisionTarget ? `${revisionTarget.title} (v${revisionTarget.version})` : ''}
        targetLabel="Giáo án gốc:"
        description="Hệ thống sẽ nhân bản bài học thành một bản thảo mới (draft revision). Bản hiện tại vẫn phục vụ học viên bình thường cho đến khi bản mới được xuất bản."
        confirmLabel="Tạo bản sửa đổi"
        confirmIcon={<Sparkles className="w-4 h-4" />}
      />

      {/* Revert Lesson Confirmation Dialog */}
      <ConfirmDialog
        isOpen={Boolean(revertTarget)}
        onClose={() => setRevertTarget(null)}
        onConfirm={() => void handleConfirmRevert()}
        isLoading={revertDraftMutation.isPending}
        variant="warning"
        title="Chuyển bài học về bản thảo (Draft)"
        targetName={revertTarget ? `${revertTarget.title} (v${revertTarget.version})` : ''}
        targetLabel="Bài học cần chuyển:"
        description="Khi chuyển về bản thảo, bài học sẽ tạm thời ẩn khỏi danh sách học viên để bạn có thể tiếp tục thêm bài tập, sửa nội dung hoặc xóa câu hỏi. Bạn có thể xuất bản lại bất kỳ lúc nào."
        confirmLabel="Chuyển về bản thảo"
        confirmIcon={<RotateCcw className="w-4 h-4" />}
      />
    </div>
  );
};
