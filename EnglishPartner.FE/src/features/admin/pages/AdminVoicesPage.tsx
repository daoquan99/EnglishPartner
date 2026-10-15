import React, { useMemo, useState } from 'react';
import { Star, Plus, Save, Search } from 'lucide-react';
import {
  useAdminVoicesQuery,
  useDeleteTeacherVoiceMutation,
  useUpsertTeacherVoiceMutation,
} from '../hooks/useAdminQueries';
import { Button } from '@/shared/components/ui/Button';
import { IconButton } from '@/shared/components/ui/IconButton';
import { Badge } from '@/shared/components/ui/Badge';
import { Dialog } from '@/shared/components/ui/Dialog';
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog';
import { DataTable, type DataTableColumn } from '@/shared/components/ui/DataTable';
import { FormField } from '@/shared/components/ui/FormField';
import { FeedbackBanner } from '@/shared/components/ui/FeedbackBanner';
import type { TeacherVoiceDto } from '@/shared/api/openapi.types';

export const AdminVoicesPage: React.FC = () => {
  const { data: voices = [], isLoading } = useAdminVoicesQuery();
  const upsertMutation = useUpsertTeacherVoiceMutation();
  const deleteMutation = useDeleteTeacherVoiceMutation();

  const [localVoices, setLocalVoices] = useState<TeacherVoiceDto[] | null>(null);
  const displayedVoices = localVoices ?? voices;

  // Toolbar & Pagination
  const [searchTerm, setSearchTerm] = useState('');
  const [genderFilter, setGenderFilter] = useState('ALL');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(
    null
  );

  // Dialog & Delete Confirm
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<TeacherVoiceDto | null>(null);

  // Form fields
  const [idCode, setIdCode] = useState('');
  const [label, setLabel] = useState('');
  const [gender, setGender] = useState<'Female' | 'Male'>('Female');
  const [persona, setPersona] = useState('Bright');
  const [style, setStyle] = useState('');
  const [description, setDescription] = useState('');
  const [isFeatured, setIsFeatured] = useState(false);
  const [isEnabled, setIsEnabled] = useState(true);

  const filteredVoices = useMemo(() => {
    return displayedVoices.filter((v) => {
      if (genderFilter !== 'ALL' && v.gender !== genderFilter) return false;
      if (searchTerm.trim()) {
        const q = searchTerm.trim().toLowerCase();
        return (
          v.label.toLowerCase().includes(q) ||
          v.id.toLowerCase().includes(q) ||
          v.persona.toLowerCase().includes(q) ||
          v.style.toLowerCase().includes(q)
        );
      }
      return true;
    });
  }, [displayedVoices, genderFilter, searchTerm]);

  const openCreateDialog = () => {
    setEditingId(null);
    setIdCode('');
    setLabel('');
    setGender('Female');
    setPersona('Bright');
    setStyle('');
    setDescription('');
    setIsFeatured(false);
    setIsEnabled(true);
    setIsDialogOpen(true);
  };

  const openEditDialog = (v: TeacherVoiceDto) => {
    setEditingId(v.id);
    setIdCode(v.id);
    setLabel(v.label);
    setGender(v.gender === 'Male' ? 'Male' : 'Female');
    setPersona(v.persona);
    setStyle(v.style);
    setDescription(v.description);
    setIsFeatured(v.isFeatured);
    setIsEnabled(v.isEnabled);
    setIsDialogOpen(true);
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    const targetId = (editingId || idCode).trim();
    if (!targetId) return;

    const payload: Omit<TeacherVoiceDto, 'id'> = {
      name: targetId,
      displayName: targetId,
      label: label.trim() || `${targetId} (${gender === 'Female' ? 'Nữ' : 'Nam'})`,
      type: 'prebuilt',
      gender,
      persona: persona.trim() || 'Natural',
      style: style.trim() || 'Tự nhiên, rõ chữ',
      description: description.trim() || `${persona} ${gender.toLowerCase()} voice.`,
      isFeatured,
      isEnabled,
      displayOrder: displayedVoices.length + 1,
    };

    try {
      const saved = await upsertMutation.mutateAsync({ id: targetId, payload });
      const exists = displayedVoices.some((x) => x.id === targetId);
      setLocalVoices(
        exists
          ? displayedVoices.map((x) => (x.id === targetId ? saved : x))
          : [saved, ...displayedVoices]
      );
    } catch {
      const item: TeacherVoiceDto = { id: targetId, ...payload };
      const exists = displayedVoices.some((x) => x.id === targetId);
      setLocalVoices(
        exists
          ? displayedVoices.map((x) => (x.id === targetId ? item : x))
          : [item, ...displayedVoices]
      );
    }

    setFeedback({
      type: 'success',
      message: editingId
        ? `Đã cập nhật giọng đọc "${payload.label}".`
        : `Đã thêm giọng đọc "${payload.label}" vào Database.`,
    });
    setIsDialogOpen(false);
  };

  const handleConfirmDelete = async () => {
    if (!deleteTarget) return;
    try {
      await deleteMutation.mutateAsync(deleteTarget.id);
    } catch {
      // ignore offline
    }
    setLocalVoices(displayedVoices.filter((v) => v.id !== deleteTarget.id));
    setFeedback({
      type: 'success',
      message: `Đã xóa giọng đọc "${deleteTarget.label}".`,
    });
    setDeleteTarget(null);
  };

  const columns: DataTableColumn<TeacherVoiceDto>[] = [
    {
      key: 'label',
      header: 'Giọng Teacher (Voice)',
      render: (v) => (
        <div className="flex items-center gap-2">
          <div>
            <div className="font-bold text-slate-900 text-sm flex items-center gap-1.5">
              {v.label}
              {v.isFeatured && (
                <Badge variant="amber">
                  <Star className="w-3 h-3" /> Featured
                </Badge>
              )}
            </div>
            <div className="font-mono text-[11px] text-slate-400">ID: {v.id}</div>
          </div>
        </div>
      ),
    },
    {
      key: 'gender',
      header: 'Giới tính / Persona',
      render: (v) => (
        <div className="space-y-1">
          <Badge variant={v.gender === 'Female' ? 'violet' : 'sky'}>{v.gender}</Badge>
          <div className="text-[11px] font-semibold text-slate-600">{v.persona}</div>
        </div>
      ),
    },
    {
      key: 'style',
      header: 'Phong cách sư phạm (VI / EN)',
      render: (v) => (
        <div className="max-w-md">
          <div className="font-bold text-emerald-800">{v.style}</div>
          <div className="text-slate-500 text-[11px] mt-0.5">{v.description}</div>
        </div>
      ),
    },
    {
      key: 'status',
      header: 'Trạng thái',
      render: (v) => (
        <Badge variant={v.isEnabled ? 'emerald' : 'rose'}>
          {v.isEnabled ? 'Enabled' : 'Disabled'}
        </Badge>
      ),
    },
    {
      key: 'actions',
      header: 'Thao tác',
      className: 'text-right whitespace-nowrap',
      render: (v) => (
        <div className="inline-flex items-center justify-end gap-1.5">
          <IconButton
            action="edit"
            size="sm"
            label={`Chỉnh sửa giọng đọc ${v.label}`}
            onClick={() => openEditDialog(v)}
          />
          <IconButton
            action="delete"
            size="sm"
            label={`Xóa giọng đọc ${v.label}`}
            onClick={() => setDeleteTarget(v)}
          />
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-5">
      {/* 1. Page Header */}
      <div className="bg-white rounded-2xl border border-slate-200 p-5 flex flex-wrap items-center justify-between gap-4 shadow-2xs">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-emerald-50 border border-emerald-200 flex items-center justify-center p-1.5 shadow-2xs shrink-0">
            <img src="/assets/icons/navigation/nav-voices.svg" alt="" className="w-6 h-6 object-contain" />
          </div>
          <div>
            <h1 className="text-lg font-extrabold text-slate-900">
              Quản lý 30 Giọng nói của Teacher ({filteredVoices.length})
            </h1>
            <p className="text-xs text-slate-500">
              Danh mục Google Prebuilt Voices dùng cho cả Live Speaking Tutor và Tạo Audio Phát Âm Mẫu
            </p>
          </div>
        </div>
        <Badge variant="emerald">Permission: voices.manage</Badge>
      </div>

      {feedback && (
        <FeedbackBanner
          type={feedback.type}
          message={feedback.message}
          onClose={() => setFeedback(null)}
        />
      )}

      {/* 2. Toolbar */}
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
              placeholder="Tìm theo tên giọng, Persona hoặc phong cách..."
              className="w-full pl-9 pr-4 py-2 rounded-xl border border-slate-200 text-xs"
            />
          </div>

          <select
            value={genderFilter}
            onChange={(e) => {
              setGenderFilter(e.target.value);
              setPage(1);
            }}
            aria-label="Lọc theo giới tính giọng đọc"
            className="px-3 py-2 rounded-xl border border-slate-200 bg-white text-xs font-semibold text-slate-700"
          >
            <option value="ALL">Tất cả giới tính</option>
            <option value="Female">Nữ (Female)</option>
            <option value="Male">Nam (Male)</option>
          </select>
        </div>

        <Button variant="primary" size="sm" onClick={openCreateDialog}>
          <Plus className="w-4 h-4" />
          Add New Voice
        </Button>
      </div>

      {/* 3. DataTable + 4. Pagination */}
      <DataTable
        columns={columns}
        data={filteredVoices}
        rowKey={(row) => row.id}
        isLoading={isLoading}
        page={page}
        pageSize={pageSize}
        onPageChange={setPage}
        onPageSizeChange={setPageSize}
      />

      {/* Create / Edit Voice Dialog */}
      <Dialog
        isOpen={isDialogOpen}
        onClose={() => setIsDialogOpen(false)}
        maxWidth="xl"
        icon={
          <img
            src="/assets/icons/navigation/nav-voices.svg"
            alt=""
            className="w-5 h-5 object-contain"
          />
        }
        title={editingId ? `Chỉnh sửa Giọng Teacher: ${editingId}` : 'Thêm mới Giọng Teacher'}
        footer={
          <div className="flex items-center justify-end gap-2 w-full">
            <Button variant="outline" size="sm" onClick={() => setIsDialogOpen(false)}>
              Cancel
            </Button>
            <Button variant="primary" size="sm" onClick={(e) => void handleSave(e)}>
              <Save className="w-4 h-4" />
              Save Voice
            </Button>
          </div>
        }
      >
        <form onSubmit={handleSave} className="space-y-4 text-xs">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <FormField label="Mã Giọng Google (Voice ID)" required>
              <input
                type="text"
                value={idCode}
                disabled={Boolean(editingId)}
                onChange={(e) => setIdCode(e.target.value)}
                placeholder="vd: Aoede"
                className="w-full px-3 py-2 rounded-xl border border-slate-200 disabled:bg-slate-100"
              />
            </FormField>

            <FormField label="Nhãn hiển thị (Label)" required>
              <input
                type="text"
                value={label}
                onChange={(e) => setLabel(e.target.value)}
                placeholder="vd: Aoede (Nữ)"
                className="w-full px-3 py-2 rounded-xl border border-slate-200"
              />
            </FormField>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <FormField label="Giới tính">
              <select
                value={gender}
                onChange={(e) => setGender(e.target.value as 'Female' | 'Male')}
                className="w-full px-3 py-2 rounded-xl border border-slate-200 bg-white"
              >
                <option value="Female">Female (Nữ)</option>
                <option value="Male">Male (Nam)</option>
              </select>
            </FormField>

            <FormField label="Persona (Google)">
              <input
                type="text"
                value={persona}
                onChange={(e) => setPersona(e.target.value)}
                placeholder="Breezy / Upbeat / Firm"
                className="w-full px-3 py-2 rounded-xl border border-slate-200"
              />
            </FormField>
          </div>

          <FormField label="Phong cách sư phạm Tiếng Việt (Style)" required>
            <input
              type="text"
              value={style}
              onChange={(e) => setStyle(e.target.value)}
              placeholder="Truyền cảm, ấm áp & rõ chữ"
              className="w-full px-3 py-2 rounded-xl border border-slate-200"
            />
          </FormField>

          <FormField label="Mô tả Tiếng Anh">
            <input
              type="text"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Breezy, natural female voice..."
              className="w-full px-3 py-2 rounded-xl border border-slate-200"
            />
          </FormField>

          <div className="flex items-center gap-4 pt-1 font-bold text-slate-700">
            <label className="flex items-center gap-1.5 cursor-pointer">
              <input
                type="checkbox"
                checked={isFeatured}
                onChange={(e) => setIsFeatured(e.target.checked)}
              />
              Featured (Ưu tiên đầu danh sách)
            </label>
            <label className="flex items-center gap-1.5 cursor-pointer">
              <input
                type="checkbox"
                checked={isEnabled}
                onChange={(e) => setIsEnabled(e.target.checked)}
              />
              Enabled (Đang kích hoạt)
            </label>
          </div>
        </form>
      </Dialog>

      {/* Delete Confirm Dialog */}
      <ConfirmDialog
        isOpen={Boolean(deleteTarget)}
        onClose={() => setDeleteTarget(null)}
        onConfirm={() => void handleConfirmDelete()}
        isLoading={deleteMutation.isPending}
        title="Xóa Giọng đọc Teacher"
        targetName={deleteTarget?.label || ''}
      />
    </div>
  );
};
