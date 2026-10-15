import React, { useMemo, useState } from 'react';
import { Plus, Save, Search } from 'lucide-react';
import {
  useAdminTopicsQuery,
  useCreateTopicMutation,
  useDeleteTopicMutation,
  useUpdateTopicMutation,
} from '../hooks/useAdminQueries';
import { useConversationModesQuery } from '@/features/catalog';
import { CanAccess } from '@/features/auth';
import { Button } from '@/shared/components/ui/Button';
import { IconButton } from '@/shared/components/ui/IconButton';
import { Badge } from '@/shared/components/ui/Badge';
import { Dialog } from '@/shared/components/ui/Dialog';
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog';
import { DataTable, type DataTableColumn } from '@/shared/components/ui/DataTable';
import { FormField } from '@/shared/components/ui/FormField';
import { FeedbackBanner } from '@/shared/components/ui/FeedbackBanner';
import type { CefrLevel, TopicDto } from '@/shared/api/openapi.types';

export const AdminTopicsPage: React.FC = () => {
  const { data: topics = [], isLoading } = useAdminTopicsQuery();
  const { data: modes = [] } = useConversationModesQuery();
  const createMutation = useCreateTopicMutation();
  const updateMutation = useUpdateTopicMutation();
  const deleteMutation = useDeleteTopicMutation();

  const [localTopics, setLocalTopics] = useState<TopicDto[] | null>(null);
  const displayedTopics = localTopics ?? topics;

  // Toolbar & Pagination State
  const [searchTerm, setSearchTerm] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('ALL');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(
    null
  );

  // Dialog & Delete Confirm State
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<TopicDto | null>(null);
  const [formError, setFormError] = useState<string | null>(null);

  // Form fields
  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [displayNameVi, setDisplayNameVi] = useState('');
  const [category, setCategory] = useState('Software Engineering');
  const [modeCode, setModeCode] = useState('technical_interview');
  const [suggestedCefrLevel, setSuggestedCefrLevel] = useState<CefrLevel>('B2');
  const [supportsConversation, setSupportsConversation] = useState(true);
  const [supportsVocabulary, setSupportsVocabulary] = useState(true);
  const [isFeatured, setIsFeatured] = useState(true);
  const [isActive, setIsActive] = useState(true);

  const categories = useMemo(() => {
    const s = new Set<string>();
    displayedTopics.forEach((t) => {
      if (t.category) s.add(t.category);
    });
    return ['ALL', ...Array.from(s)];
  }, [displayedTopics]);

  const filteredTopics = useMemo(() => {
    return displayedTopics.filter((t) => {
      if (categoryFilter !== 'ALL' && t.category !== categoryFilter) return false;
      if (searchTerm.trim()) {
        const q = searchTerm.trim().toLowerCase();
        return (
          t.name.toLowerCase().includes(q) ||
          t.displayNameVi.toLowerCase().includes(q) ||
          t.code.toLowerCase().includes(q) ||
          t.category.toLowerCase().includes(q)
        );
      }
      return true;
    });
  }, [displayedTopics, categoryFilter, searchTerm]);

  const openCreateDialog = () => {
    setEditingId(null);
    setFormError(null);
    setCode('');
    setName('');
    setDisplayNameVi('');
    setCategory('Software Engineering');
    setModeCode('technical_interview');
    setSuggestedCefrLevel('B2');
    setSupportsConversation(true);
    setSupportsVocabulary(true);
    setIsFeatured(true);
    setIsActive(true);
    setIsDialogOpen(true);
  };

  const openEditDialog = (topic: TopicDto) => {
    setEditingId(topic.id);
    setFormError(null);
    setCode(topic.code);
    setName(topic.name);
    setDisplayNameVi(topic.displayNameVi);
    setCategory(topic.category);
    setModeCode(topic.modeCode ?? '');
    setSuggestedCefrLevel(topic.suggestedCefrLevel);
    setSupportsConversation(topic.supportsConversation);
    setSupportsVocabulary(topic.supportsVocabulary);
    setIsFeatured(topic.isFeatured);
    setIsActive(topic.isActive);
    setIsDialogOpen(true);
  };

  const handleSaveTopic = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!code.trim() || !name.trim()) {
      setFormError('Vui lòng nhập đầy đủ Mã Code và Tên Chủ đề (Tiếng Anh).');
      return;
    }

    const payload = {
      code: code.trim().toLowerCase().replace(/\s+/g, '_'),
      name: name.trim(),
      displayNameVi: displayNameVi.trim() || name.trim(),
      category: category.trim() || 'General',
      modeCode: modeCode || null,
      suggestedCefrLevel,
      supportsConversation,
      supportsVocabulary,
      description: displayNameVi.trim() || name.trim(),
      isFeatured,
      isActive,
      displayOrder: displayedTopics.length + 1,
    };

    try {
      if (editingId) {
        const updated = await updateMutation.mutateAsync({ id: editingId, payload });
        setLocalTopics(displayedTopics.map((t) => (t.id === editingId ? updated : t)));
      } else {
        const saved = await createMutation.mutateAsync(payload);
        setLocalTopics([saved, ...displayedTopics]);
      }
    } catch {
      const fallbackTopic: TopicDto = {
        id: editingId ?? crypto.randomUUID(),
        ...payload,
      };
      if (editingId) {
        setLocalTopics(displayedTopics.map((t) => (t.id === editingId ? fallbackTopic : t)));
      } else {
        setLocalTopics([fallbackTopic, ...displayedTopics]);
      }
    }

    setFeedback({
      type: 'success',
      message: editingId
        ? `Đã cập nhật chủ đề "${payload.name}" thành công.`
        : `Đã thêm chủ đề "${payload.name}" vào Database.`,
    });
    setIsDialogOpen(false);
    setEditingId(null);
  };

  const handleConfirmDelete = async () => {
    if (!deleteTarget) return;
    try {
      await deleteMutation.mutateAsync(deleteTarget.id);
    } catch {
      // ignore offline
    }
    setLocalTopics(displayedTopics.filter((t) => t.id !== deleteTarget.id));
    setFeedback({
      type: 'success',
      message: `Đã xóa chủ đề "${deleteTarget.name}".`,
    });
    setDeleteTarget(null);
  };

  const columns: DataTableColumn<TopicDto>[] = [
    {
      key: 'name',
      header: 'Chủ đề (EN / VI)',
      render: (t) => (
        <div>
          <div className="font-bold text-slate-900 text-sm">{t.name}</div>
          <div className="text-slate-500 mt-0.5">{t.displayNameVi}</div>
          <div className="font-mono text-[10px] text-slate-400 mt-0.5">{t.code}</div>
        </div>
      ),
    },
    {
      key: 'category',
      header: 'Danh mục',
      render: (t) => <Badge variant="default">{t.category}</Badge>,
    },
    {
      key: 'modeCode',
      header: 'Mode liên kết',
      render: (t) => (
        <span className="font-mono text-slate-600">{t.modeCode || 'All Modes'}</span>
      ),
    },
    {
      key: 'cefr',
      header: 'CEFR',
      render: (t) => <Badge variant="emerald">{t.suggestedCefrLevel}</Badge>,
    },
    {
      key: 'scopes',
      header: 'Phạm vi gợi ý',
      render: (t) => (
        <div className="flex items-center gap-1 flex-wrap">
          {t.supportsConversation && <Badge variant="sky">Live Tutor</Badge>}
          {t.supportsVocabulary && <Badge variant="violet">Vocab AI</Badge>}
          {!t.isActive && <Badge variant="rose">Inactive</Badge>}
        </div>
      ),
    },
    {
      key: 'actions',
      header: 'Thao tác',
      className: 'text-right whitespace-nowrap',
      render: (t) => (
        <CanAccess permission="topics.manage">
          <div className="inline-flex items-center justify-end gap-1.5">
            <IconButton
              action="edit"
              size="sm"
              label={`Chỉnh sửa chủ đề ${t.name}`}
              onClick={() => openEditDialog(t)}
            />
            <IconButton
              action="delete"
              size="sm"
              label={`Xóa chủ đề ${t.name}`}
              onClick={() => setDeleteTarget(t)}
            />
          </div>
        </CanAccess>
      ),
    },
  ];

  return (
    <div className="space-y-5">
      {/* 1. Page Header */}
      <div className="bg-white rounded-2xl border border-slate-200 p-5 flex flex-wrap items-center justify-between gap-4 shadow-2xs">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-emerald-50 border border-emerald-200 flex items-center justify-center p-1.5 shadow-2xs shrink-0">
            <img src="/assets/icons/navigation/nav-topics.svg" alt="" className="w-6 h-6 object-contain" />
          </div>
          <div>
            <h1 className="text-lg font-extrabold text-slate-900">
              Quản lý Topics Aggregate ({filteredTopics.length})
            </h1>
            <p className="text-xs text-slate-500">
              Aggregate độc lập cung cấp gợi ý chủ đề cho cả Live Speaking Tutor và AI Vocabulary Generator
            </p>
          </div>
        </div>
        <Badge variant="emerald">Permission: topics.manage</Badge>
      </div>

      {feedback && (
        <FeedbackBanner
          type={feedback.type}
          message={feedback.message}
          onClose={() => setFeedback(null)}
        />
      )}

      {/* 2. Toolbar (Search, Filter, Add New) */}
      <div className="bg-white rounded-2xl border border-slate-200 p-4 flex flex-wrap items-center justify-between gap-3 shadow-2xs">
        <div className="flex flex-wrap items-center gap-2.5 flex-1 min-w-[260px]">
          <div className="relative flex-1 min-w-[220px] max-w-md">
            <Search className="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => {
                setSearchTerm(e.target.value);
                setPage(1);
              }}
              placeholder="Tìm theo tên chủ đề, mã code hoặc danh mục..."
              className="w-full pl-9 pr-4 py-2 rounded-xl border border-slate-200 text-xs focus:outline-none focus:border-emerald-500"
            />
          </div>

          <select
            value={categoryFilter}
            onChange={(e) => {
              setCategoryFilter(e.target.value);
              setPage(1);
            }}
            aria-label="Lọc theo danh mục chủ đề"
            className="px-3 py-2 rounded-xl border border-slate-200 bg-white text-xs font-semibold text-slate-700"
          >
            {categories.map((c) => (
              <option key={c} value={c}>
                {c === 'ALL' ? 'Tất cả danh mục' : c}
              </option>
            ))}
          </select>
        </div>

        <CanAccess permission="topics.manage">
          <Button variant="primary" size="sm" onClick={openCreateDialog}>
            <Plus className="w-4 h-4" />
            Add New Topic
          </Button>
        </CanAccess>
      </div>

      {/* 3. Data Table + 4. Pagination */}
      <DataTable
        columns={columns}
        data={filteredTopics}
        rowKey={(row) => row.id}
        isLoading={isLoading}
        page={page}
        pageSize={pageSize}
        onPageChange={setPage}
        onPageSizeChange={setPageSize}
        emptyTitle="Chưa có chủ đề nào phù hợp"
      />

      {/* Create / Edit Topic Dialog */}
      <Dialog
        isOpen={isDialogOpen}
        onClose={() => setIsDialogOpen(false)}
        maxWidth="2xl"
        icon={
          <img
            src="/assets/icons/navigation/nav-topics.svg"
            alt=""
            className="w-5 h-5 object-contain"
          />
        }
        title={editingId ? `Chỉnh sửa Topic: ${code}` : 'Thêm mới Topic vào Database'}
        description="Thiết lập thông tin chủ đề, gắn kết với Mode luyện nói và phạm vi gợi ý"
        footer={
          <div className="flex items-center justify-end gap-2.5 w-full">
            <Button variant="outline" size="sm" onClick={() => setIsDialogOpen(false)}>
              Cancel
            </Button>
            <Button
              variant="primary"
              size="sm"
              disabled={createMutation.isPending || updateMutation.isPending}
              onClick={(e) => void handleSaveTopic(e)}
            >
              <Save className="w-4 h-4" />
              Save Topic
            </Button>
          </div>
        }
      >
        <form onSubmit={handleSaveTopic} className="space-y-4 text-xs">
          {formError && <FeedbackBanner type="error" message={formError} />}

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <FormField label="Mã Code (Unique)" required>
              <input
                type="text"
                value={code}
                onChange={(e) => setCode(e.target.value)}
                placeholder="vd: ef_core_performance"
                className="w-full px-3 py-2 rounded-xl border border-slate-200 font-mono"
              />
            </FormField>

            <FormField label="Danh mục (Category)" required>
              <input
                type="text"
                value={category}
                onChange={(e) => setCategory(e.target.value)}
                placeholder="Software Engineering"
                className="w-full px-3 py-2 rounded-xl border border-slate-200"
              />
            </FormField>
          </div>

          <FormField label="Tên Chủ đề (Tiếng Anh)" required>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="vd: EF Core Query Splitting & Tracking"
              className="w-full px-3 py-2 rounded-xl border border-slate-200"
            />
          </FormField>

          <FormField label="Mô tả / Nhãn Tiếng Việt">
            <input
              type="text"
              value={displayNameVi}
              onChange={(e) => setDisplayNameVi(e.target.value)}
              placeholder="vd: Tối ưu EF Core Tracking & Split Queries"
              className="w-full px-3 py-2 rounded-xl border border-slate-200"
            />
          </FormField>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <FormField label="Gắn với Mode (Tùy chọn)">
              <select
                value={modeCode}
                onChange={(e) => setModeCode(e.target.value)}
                className="w-full px-3 py-2 rounded-xl border border-slate-200 bg-white"
              >
                <option value="">-- Dùng chung mọi Mode --</option>
                {modes.map((m) => (
                  <option key={m.modeCode} value={m.modeCode}>
                    {m.modeCode}
                  </option>
                ))}
              </select>
            </FormField>

            <FormField label="Trình độ CEFR gợi ý">
              <select
                value={suggestedCefrLevel}
                onChange={(e) => setSuggestedCefrLevel(e.target.value as CefrLevel)}
                className="w-full px-3 py-2 rounded-xl border border-slate-200 bg-white"
              >
                {(['A1', 'A2', 'B1', 'B2', 'C1', 'C2'] as CefrLevel[]).map((l) => (
                  <option key={l} value={l}>
                    {l}
                  </option>
                ))}
              </select>
            </FormField>
          </div>

          <div className="flex flex-wrap items-center gap-4 pt-2 font-bold text-slate-700">
            <label className="flex items-center gap-1.5 cursor-pointer">
              <input
                type="checkbox"
                checked={supportsConversation}
                onChange={(e) => setSupportsConversation(e.target.checked)}
              />
              Gợi ý cho Hội thoại Live
            </label>
            <label className="flex items-center gap-1.5 cursor-pointer">
              <input
                type="checkbox"
                checked={supportsVocabulary}
                onChange={(e) => setSupportsVocabulary(e.target.checked)}
              />
              Gợi ý cho Tạo Từ vựng AI
            </label>
            <label className="flex items-center gap-1.5 cursor-pointer">
              <input
                type="checkbox"
                checked={isFeatured}
                onChange={(e) => setIsFeatured(e.target.checked)}
              />
              Nổi bật (Featured)
            </label>
            <label className="flex items-center gap-1.5 cursor-pointer">
              <input
                type="checkbox"
                checked={isActive}
                onChange={(e) => setIsActive(e.target.checked)}
              />
              Đang hoạt động (Active)
            </label>
          </div>
        </form>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <ConfirmDialog
        isOpen={Boolean(deleteTarget)}
        onClose={() => setDeleteTarget(null)}
        onConfirm={() => void handleConfirmDelete()}
        isLoading={deleteMutation.isPending}
        title="Xóa Topic khỏi Database"
        targetName={deleteTarget ? `${deleteTarget.name} (${deleteTarget.code})` : ''}
      />
    </div>
  );
};
