import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  ArrowLeft,
  Plus,
  UploadCloud,
  Sparkles,
  CheckCircle2,
  Layers,
  BookOpen,
  Award,
  RefreshCw,
  Trash2,
  AlertCircle,
  RotateCcw,
} from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog';
import {
  useAdminLessonDetailQuery,
  useUpdateLessonDraftMutation,
  useAddExerciseMutation,
  useUpdateExerciseMutation,
  useDeleteExerciseMutation,
  useDeleteLessonDraftMutation,
  useReorderExercisesMutation,
  usePublishLessonMutation,
  useRevertLessonToDraftMutation,
  useCreateLessonRevisionMutation,
} from '../hooks/useLessonQueries';
import { LessonMetadataForm } from '../components/builder/LessonMetadataForm';
import { ExerciseCard } from '../components/builder/ExerciseCard';
import { ExerciseEditorModal } from '../components/builder/ExerciseEditorModal';
import { LessonImportModal } from '../components/builder/LessonImportModal';
import { AiLessonExerciseGeneratorModal } from '../components/builder/AiLessonExerciseGeneratorModal';
import type { ExerciseAuthorDto, AddExerciseRequest } from '../types/lessons.types';
import { toast } from '@/shared/lib/toast';

export const AdminLessonBuilderPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const { data: lesson, isLoading, refetch } = useAdminLessonDetailQuery(id);

  const updateDraftMutation = useUpdateLessonDraftMutation();
  const addExerciseMutation = useAddExerciseMutation();
  const updateExerciseMutation = useUpdateExerciseMutation();
  const deleteExerciseMutation = useDeleteExerciseMutation();
  const deleteDraftMutation = useDeleteLessonDraftMutation();
  const reorderExercisesMutation = useReorderExercisesMutation();
  const publishMutation = usePublishLessonMutation();
  const revertDraftMutation = useRevertLessonToDraftMutation();
  const revisionMutation = useCreateLessonRevisionMutation();

  // Dialog & drawer states
  const [isEditorOpen, setIsEditorOpen] = useState(false);
  const [editingExercise, setEditingExercise] = useState<ExerciseAuthorDto | null>(null);
  const [isImportOpen, setIsImportOpen] = useState(false);
  const [isAiGeneratorOpen, setIsAiGeneratorOpen] = useState(false);

  // Confirmation dialog states
  const [deleteExerciseTarget, setDeleteExerciseTarget] = useState<ExerciseAuthorDto | null>(null);
  const [isDeleteLessonConfirmOpen, setIsDeleteLessonConfirmOpen] = useState(false);
  const [isPublishConfirmOpen, setIsPublishConfirmOpen] = useState(false);
  const [isRevertConfirmOpen, setIsRevertConfirmOpen] = useState(false);
  const [isRevisionConfirmOpen, setIsRevisionConfirmOpen] = useState(false);

  if (isLoading) {
    return (
      <div className="text-center py-16 text-xs font-semibold text-slate-500">
        Đang tải thông tin chi tiết bài học...
      </div>
    );
  }

  if (!lesson || !id) {
    return (
      <div className="text-center py-16 space-y-3">
        <p className="text-sm font-bold text-slate-800">Không tìm thấy bài học này.</p>
        <Button type="button" variant="outline" size="sm" onClick={() => navigate('/admin/lessons')}>
          Quay lại danh sách
        </Button>
      </div>
    );
  }

  const isDraft = lesson.status === 'Draft';
  const isPublished = lesson.status === 'Published';
  const exercises = lesson.exercises ?? [];

  const handleUpdateMetadata = async (values: any) => {
    try {
      await updateDraftMutation.mutateAsync({ id, data: values });
      toast.success('Đã cập nhật thông tin bài học thành công!');
    } catch {
      toast.error('Lỗi khi cập nhật thông tin bài học.');
    }
  };

  const handleSaveExercise = async (data: AddExerciseRequest) => {
    try {
      if (editingExercise) {
        await updateExerciseMutation.mutateAsync({
          lessonId: id,
          exerciseId: editingExercise.id,
          data,
        });
        toast.success('Đã cập nhật bài tập thành công!');
      } else {
        await addExerciseMutation.mutateAsync({
          lessonId: id,
          data,
        });
        toast.success('Đã thêm bài tập mới vào bài học!');
      }
      setIsEditorOpen(false);
      setEditingExercise(null);
      void refetch();
    } catch {
      toast.error('Lỗi khi lưu bài tập.');
    }
  };

  const handleConfirmDeleteExercise = async () => {
    if (!deleteExerciseTarget || !id) return;
    try {
      await deleteExerciseMutation.mutateAsync({ lessonId: id, exerciseId: deleteExerciseTarget.id });
      toast.success(`Đã xóa bài tập "${deleteExerciseTarget.title}".`);
      setDeleteExerciseTarget(null);
      void refetch();
    } catch {
      toast.error('Lỗi khi xóa bài tập.');
    }
  };

  const handleMoveExercise = async (index: number, direction: 'up' | 'down') => {
    const targetIndex = direction === 'up' ? index - 1 : index + 1;
    if (targetIndex < 0 || targetIndex >= exercises.length) return;

    const reordered = [...exercises];
    const temp = reordered[index];
    reordered[index] = reordered[targetIndex];
    reordered[targetIndex] = temp;

    const orderedIds = reordered.map((e) => e.id);
    try {
      await reorderExercisesMutation.mutateAsync({
        lessonId: id,
        orderedExerciseIds: orderedIds,
      });
      void refetch();
    } catch {
      toast.error('Lỗi khi thay đổi thứ tự bài tập.');
    }
  };

  const handleConfirmPublish = async () => {
    if (!id || !lesson) return;
    try {
      await publishMutation.mutateAsync({ id, rowVersion: lesson.rowVersion });
      toast.success('Đã xuất bản bài học thành công!');
      setIsPublishConfirmOpen(false);
      void refetch();
    } catch {
      toast.error('Không thể xuất bản bài học.');
    }
  };

  const handleConfirmRevertLesson = async () => {
    if (!id) return;
    try {
      await revertDraftMutation.mutateAsync(id);
      toast.success('Đã chuyển bài học về bản thảo thành công! Bây giờ bạn có thể thêm bài tập.');
      setIsRevertConfirmOpen(false);
      void refetch();
    } catch {
      toast.error('Không thể chuyển bài học về bản thảo.');
    }
  };

  const handleConfirmRevision = async () => {
    if (!id) return;
    try {
      const res = await revisionMutation.mutateAsync(id);
      toast.success('Đã tạo bản sửa đổi mới thành công!');
      setIsRevisionConfirmOpen(false);
      navigate(`/admin/lessons/${res.draftLessonId}`);
    } catch {
      toast.error('Lỗi khi tạo bản sửa đổi mới.');
    }
  };

  const handleConfirmDeleteLesson = async () => {
    if (!id) return;
    try {
      await deleteDraftMutation.mutateAsync(id);
      toast.success('Đã xóa bản thảo bài học thành công!');
      setIsDeleteLessonConfirmOpen(false);
      navigate('/admin/lessons');
    } catch {
      toast.error('Lỗi khi xóa bài học.');
    }
  };

  const totalPoints = exercises.reduce((sum: number, e: ExerciseAuthorDto) => sum + (e.maxPoints || 0), 0);

  return (
    <div className="space-y-6">
      {/* Header bar */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 border-b border-slate-200/80 pb-4">
        <div className="flex items-center gap-3">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => navigate('/admin/lessons')}
            className="rounded-full w-9 h-9 p-0 text-slate-600"
          >
            <ArrowLeft className="w-4 h-4" />
          </Button>

          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-xl sm:text-2xl font-black text-slate-900 tracking-tight">
                {lesson.title}
              </h1>
              <Badge
                variant={isPublished ? 'emerald' : isDraft ? 'amber' : 'default'}
              >
                {isPublished ? 'Đã xuất bản' : isDraft ? 'Bản thảo' : 'Đã lưu trữ'}
              </Badge>
              <span className="text-xs font-mono font-bold text-slate-500">v{lesson.version}</span>
            </div>
            <p className="text-xs text-slate-500 mt-0.5">
              Chủ đề: <strong className="text-slate-700">{lesson.topic}</strong> • CEFR:{' '}
              <strong className="text-emerald-700">{lesson.cefrLevel}</strong>
            </p>
          </div>
        </div>

        {/* Action Buttons */}
        <div className="flex items-center gap-2 flex-wrap">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => setIsAiGeneratorOpen(true)}
            className="gap-1.5 text-xs text-amber-700 border-amber-200 hover:border-amber-400 hover:bg-amber-50 font-semibold"
          >
            <Sparkles className="w-3.5 h-3.5 text-amber-500" /> Tạo bài tập bằng AI
          </Button>

          {isDraft && (
            <>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => setIsImportOpen(true)}
                className="gap-1.5 text-xs text-slate-700 border-slate-200 hover:border-emerald-500 hover:text-emerald-700 hover:bg-emerald-50"
              >
                <UploadCloud className="w-3.5 h-3.5 text-emerald-600" /> Nhập từ JSON
              </Button>

              <Button
                type="button"
                variant="primary"
                size="sm"
                onClick={() => setIsPublishConfirmOpen(true)}
                disabled={publishMutation.isPending || exercises.length === 0}
                className="gap-1.5 text-xs font-semibold"
                title={exercises.length === 0 ? 'Cần thêm ít nhất 1 bài tập trước khi xuất bản bài học' : 'Xuất bản bài học'}
              >
                <CheckCircle2 className="w-3.5 h-3.5" />
                Xuất bản bài học
              </Button>

              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => setIsDeleteLessonConfirmOpen(true)}
                disabled={deleteDraftMutation.isPending}
                className="gap-1.5 text-xs text-rose-600 hover:text-rose-700 hover:bg-rose-50 border-slate-200 hover:border-rose-200 font-semibold"
                title="Xóa bản thảo bài học"
              >
                <Trash2 className="w-3.5 h-3.5" /> Xóa bài học
              </Button>
            </>
          )}

          {isPublished && (
            <>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => setIsRevertConfirmOpen(true)}
                disabled={revertDraftMutation.isPending}
                className="gap-1.5 text-xs text-amber-700 border-amber-300 hover:border-amber-400 hover:bg-amber-50 font-semibold"
                title="Chuyển về bản thảo để tiếp tục thêm và sửa bài tập"
              >
                <RotateCcw className="w-3.5 h-3.5" /> Chuyển về bản thảo
              </Button>

              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => setIsRevisionConfirmOpen(true)}
                disabled={revisionMutation.isPending}
                className="gap-1.5 text-xs text-slate-700 font-semibold"
              >
                <RefreshCw className="w-3.5 h-3.5" /> Tạo bản sửa đổi mới
              </Button>
            </>
          )}
        </div>
      </div>

      {/* Lesson Metadata Form */}
      {isDraft ? (
        <LessonMetadataForm
          initialValues={{
            title: lesson.title,
            cefrLevel: lesson.cefrLevel,
            topic: lesson.topic,
            content: lesson.content,
            estimatedMinutes: lesson.estimatedMinutes,
            learningObjectives: lesson.learningObjectives,
            targetVocabulary: lesson.targetVocabulary,
            targetGrammar: lesson.targetGrammar,
            rowVersion: lesson.rowVersion,
          }}
          onSave={handleUpdateMetadata}
          isSaving={updateDraftMutation.isPending}
        />
      ) : (
        <div className="bg-slate-50 p-5 rounded-3xl border border-slate-200/80 text-xs text-slate-700 space-y-3">
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
            <div>
              <span className="font-bold text-slate-900 block text-sm">
                Bài học đã xuất bản (Chế độ xem)
              </span>
              <p className="text-slate-500 mt-0.5">
                Chuyển bài học về Bản thảo nếu bạn muốn tiếp tục thêm mới, sửa hoặc xóa các bài tập trong giáo án này.
              </p>
            </div>
            <div className="flex items-center gap-2">
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => setIsRevertConfirmOpen(true)}
                disabled={revertDraftMutation.isPending}
                className="gap-1.5 text-xs text-amber-700 border-amber-300 hover:border-amber-400 hover:bg-amber-50 font-semibold"
              >
                <RotateCcw className="w-3.5 h-3.5" /> Chuyển về bản thảo
              </Button>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => setIsRevisionConfirmOpen(true)}
                disabled={revisionMutation.isPending}
                className="gap-1.5 text-xs text-slate-700 font-semibold"
              >
                <RefreshCw className="w-3.5 h-3.5" /> Tạo bản sửa mới
              </Button>
            </div>
          </div>
          {lesson.content && (
            <p className="pt-2 border-t border-slate-200/80 text-slate-600 leading-relaxed">
              {lesson.content}
            </p>
          )}
        </div>
      )}

      {/* Exercise List Section */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <h3 className="text-sm font-bold text-slate-900 uppercase tracking-wider flex items-center gap-1.5">
              <Layers className="w-4 h-4 text-emerald-600" />
              Các bài tập trong giáo án ({exercises.length})
            </h3>
            <Badge variant="outline" className="font-bold text-emerald-700">
              Tổng điểm: {totalPoints}
            </Badge>
          </div>

          {isDraft && (
            <Button
              type="button"
              variant="primary"
              size="sm"
              onClick={() => {
                setEditingExercise(null);
                setIsEditorOpen(true);
              }}
              className="gap-1.5 text-xs"
            >
              <Plus className="w-4 h-4" /> Thêm bài tập
            </Button>
          )}
        </div>

        {exercises.length === 0 ? (
          <div className="bg-white rounded-3xl border border-slate-200 p-10 text-center space-y-3">
            <Layers className="w-8 h-8 text-slate-300 mx-auto" />
            <p className="text-xs font-semibold text-slate-600">
              Bài học này chưa có bài tập nào. Hãy thêm bài tập thủ công hoặc tạo bằng AI để có thể xuất bản cho học viên.
            </p>
            {isDraft && (
              <div className="flex flex-wrap items-center justify-center gap-2 pt-1">
                <Button
                  type="button"
                  variant="primary"
                  size="sm"
                  onClick={() => setIsEditorOpen(true)}
                  className="gap-1.5 text-xs font-semibold"
                >
                  <Plus className="w-4 h-4" /> Thêm bài tập thủ công
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => setIsAiGeneratorOpen(true)}
                  className="gap-1.5 text-xs text-amber-700 border-amber-200 hover:border-amber-400 hover:bg-amber-50 font-semibold"
                >
                  <Sparkles className="w-4 h-4 text-amber-500" /> Tạo bài tập bằng AI
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => setIsImportOpen(true)}
                  className="gap-1.5 text-xs text-slate-700 border-slate-200 hover:border-emerald-500 hover:text-emerald-700 hover:bg-emerald-50"
                >
                  <UploadCloud className="w-4 h-4 text-emerald-600" /> Nhập từ JSON
                </Button>
              </div>
            )}
          </div>
        ) : (
          <div className="space-y-3">
            {exercises.map((ex: ExerciseAuthorDto, index: number) => (
              <ExerciseCard
                key={ex.id}
                exercise={ex}
                index={index}
                totalCount={exercises.length}
                onMoveUp={() => handleMoveExercise(index, 'up')}
                onMoveDown={() => handleMoveExercise(index, 'down')}
                onEdit={() => {
                  setEditingExercise(ex);
                  setIsEditorOpen(true);
                }}
                onDelete={() => setDeleteExerciseTarget(ex)}
                isMoving={reorderExercisesMutation.isPending}
              />
            ))}
          </div>
        )}
      </div>

      {/* Exercise Editor Modal */}
      <ExerciseEditorModal
        isOpen={isEditorOpen}
        onClose={() => {
          setIsEditorOpen(false);
          setEditingExercise(null);
        }}
        onSave={handleSaveExercise}
        initialExercise={editingExercise}
        isSaving={addExerciseMutation.isPending || updateExerciseMutation.isPending}
      />

      {/* Lesson Import Modal */}
      <LessonImportModal
        isOpen={isImportOpen}
        onClose={() => setIsImportOpen(false)}
        targetLessonId={id}
        onSuccess={() => void refetch()}
      />

      {/* AI Lesson Exercise Generator Modal */}
      <AiLessonExerciseGeneratorModal
        isOpen={isAiGeneratorOpen}
        onClose={() => setIsAiGeneratorOpen(false)}
        targetLessonId={id}
        lessonMetadata={{
          title: lesson.title,
          cefrLevel: lesson.cefrLevel,
          topic: lesson.topic,
          targetVocabulary: lesson.targetVocabulary,
          targetGrammar: lesson.targetGrammar,
        }}
        onSuccess={() => void refetch()}
      />

      {/* Delete Exercise Confirmation Dialog */}
      <ConfirmDialog
        isOpen={Boolean(deleteExerciseTarget)}
        onClose={() => setDeleteExerciseTarget(null)}
        onConfirm={() => void handleConfirmDeleteExercise()}
        isLoading={deleteExerciseMutation.isPending}
        title="Xóa bài tập khỏi giáo án"
        targetName={deleteExerciseTarget ? deleteExerciseTarget.title : ''}
        targetLabel="Bài tập cần xóa:"
        description="Bạn có chắc chắn muốn xóa bài tập này khỏi giáo án? Hành động này không thể hoàn tác."
        confirmLabel="Xóa bài tập"
        variant="danger"
      />

      {/* Delete Lesson Confirmation Dialog */}
      <ConfirmDialog
        isOpen={isDeleteLessonConfirmOpen}
        onClose={() => setIsDeleteLessonConfirmOpen(false)}
        onConfirm={() => void handleConfirmDeleteLesson()}
        isLoading={deleteDraftMutation.isPending}
        title="Xóa bài học khỏi hệ thống"
        targetName={lesson.title}
        targetLabel="Bài học cần xóa:"
        description="Bạn có chắc chắn muốn xóa bài học này? Toàn bộ các bài tập đã tạo trong bài học sẽ bị xóa vĩnh viễn và không thể khôi phục."
        confirmLabel="Xóa bài học"
        variant="danger"
      />

      {/* Publish Lesson Confirmation Dialog */}
      <ConfirmDialog
        isOpen={isPublishConfirmOpen}
        onClose={() => setIsPublishConfirmOpen(false)}
        onConfirm={() => void handleConfirmPublish()}
        isLoading={publishMutation.isPending}
        variant="primary"
        title="Xác nhận xuất bản bài học"
        targetName={`${lesson.title} (v${lesson.version})`}
        targetLabel="Giáo án xuất bản:"
        description={`Giáo án hiện có ${exercises.length} bài tập (tổng ${totalPoints} điểm). Khi xuất bản, học viên sẽ có thể truy cập làm bài ngay lập tức.`}
        confirmLabel="Xuất bản ngay"
        confirmIcon={<CheckCircle2 className="w-4 h-4" />}
      />

      {/* Create Revision Confirmation Dialog */}
      <ConfirmDialog
        isOpen={isRevisionConfirmOpen}
        onClose={() => setIsRevisionConfirmOpen(false)}
        onConfirm={() => void handleConfirmRevision()}
        isLoading={revisionMutation.isPending}
        variant="primary"
        title="Tạo bản sửa đổi mới (Draft Revision)"
        targetName={`${lesson.title} (v${lesson.version})`}
        targetLabel="Giáo án gốc:"
        description="Hệ thống sẽ nhân bản giáo án hiện tại thành một bản thảo mới (Draft Revision). Bản hiện tại vẫn tiếp tục phục vụ học viên cho đến khi bản mới được xuất bản."
        confirmLabel="Tạo bản sửa đổi"
        confirmIcon={<Sparkles className="w-4 h-4" />}
      />

      {/* Revert Lesson Confirmation Dialog */}
      <ConfirmDialog
        isOpen={isRevertConfirmOpen}
        onClose={() => setIsRevertConfirmOpen(false)}
        onConfirm={() => void handleConfirmRevertLesson()}
        isLoading={revertDraftMutation.isPending}
        variant="warning"
        title="Chuyển bài học về bản thảo (Draft)"
        targetName={`${lesson.title} (v${lesson.version})`}
        targetLabel="Bài học cần chuyển:"
        description="Khi chuyển về bản thảo, bài học sẽ tạm thời ẩn khỏi học viên để bạn có thể tiếp tục thêm, sửa, xóa các bài tập và cấu hình giáo án. Bạn có thể xuất bản lại bất cứ lúc nào."
        confirmLabel="Chuyển về bản thảo"
        confirmIcon={<RotateCcw className="w-4 h-4" />}
      />
    </div>
  );
};
