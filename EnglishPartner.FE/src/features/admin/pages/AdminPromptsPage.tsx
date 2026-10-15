import React, { useMemo, useState } from 'react';
import {
  Eye,
  Sparkles,
  Plus,
  Save,
  Search,
} from 'lucide-react';
import {
  useCefrLevelPromptsQuery,
  useConversationModesQuery,
  useTeacherVoicesQuery,
} from '@/features/catalog';
import {
  useAdminSpeechSpeedsQuery,
  useCreateSpeechSpeedMutation,
  useDeleteCefrPromptMutation,
  useDeleteModePromptMutation,
  useDeleteSpeechSpeedMutation,
  useUpdateSpeechSpeedMutation,
  useUpsertCefrPromptMutation,
  useUpsertModePromptMutation,
} from '../hooks/useAdminQueries';
import { adminApi } from '../api/adminApi';
import { Button } from '@/shared/components/ui/Button';
import { IconButton } from '@/shared/components/ui/IconButton';
import { Badge } from '@/shared/components/ui/Badge';
import { Dialog } from '@/shared/components/ui/Dialog';
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog';
import { DataTable, type DataTableColumn } from '@/shared/components/ui/DataTable';
import { FormField } from '@/shared/components/ui/FormField';
import { FeedbackBanner } from '@/shared/components/ui/FeedbackBanner';
import type {
  CefrLevel,
  CefrLevelPromptDto,
  ConversationModePromptDto,
  SpeechSpeedOptionDto,
} from '@/shared/api/openapi.types';

export const AdminPromptsPage: React.FC = () => {
  const { data: cefrPrompts = [] } = useCefrLevelPromptsQuery();
  const { data: modePrompts = [] } = useConversationModesQuery();
  const { data: speechSpeeds = [] } = useAdminSpeechSpeedsQuery();
  const { data: voices = [] } = useTeacherVoicesQuery();

  const upsertCefrMutation = useUpsertCefrPromptMutation();
  const deleteCefrMutation = useDeleteCefrPromptMutation();
  const upsertModeMutation = useUpsertModePromptMutation();
  const deleteModeMutation = useDeleteModePromptMutation();
  const createSpeedMutation = useCreateSpeechSpeedMutation();
  const updateSpeedMutation = useUpdateSpeechSpeedMutation();
  const deleteSpeedMutation = useDeleteSpeechSpeedMutation();

  const [activeTab, setActiveTab] = useState<'modes' | 'cefr' | 'speeds'>('modes');
  const [searchTerm, setSearchTerm] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(
    null
  );

  const [localCefr, setLocalCefr] = useState<CefrLevelPromptDto[] | null>(null);
  const [localModes, setLocalModes] = useState<ConversationModePromptDto[] | null>(null);
  const [localSpeeds, setLocalSpeeds] = useState<SpeechSpeedOptionDto[] | null>(null);

  const displayedCefr = localCefr ?? cefrPrompts;
  const displayedModes = localModes ?? modePrompts;
  const displayedSpeeds = localSpeeds ?? speechSpeeds;

  // Dialog Open States
  const [isModeDialogOpen, setIsModeDialogOpen] = useState(false);
  const [isCefrDialogOpen, setIsCefrDialogOpen] = useState(false);
  const [isSpeedDialogOpen, setIsSpeedDialogOpen] = useState(false);
  const [isPreviewDialogOpen, setIsPreviewDialogOpen] = useState(false);

  // Delete Confirm Targets
  const [deleteModeTarget, setDeleteModeTarget] = useState<ConversationModePromptDto | null>(null);
  const [deleteCefrTarget, setDeleteCefrTarget] = useState<CefrLevelPromptDto | null>(null);
  const [deleteSpeedTarget, setDeleteSpeedTarget] = useState<SpeechSpeedOptionDto | null>(null);

  // Mode Form State
  const [editingModeCode, setEditingModeCode] = useState<string | null>(null);
  const [modeCode, setModeCode] = useState('');
  const [modeCategory, setModeCategory] = useState('Interview & Coaching');
  const [modeDisplayName, setModeDisplayName] = useState('');
  const [modeDirective, setModeDirective] = useState('');
  const [modeOrder, setModeOrder] = useState(20);
  const [modeIsDefault, setModeIsDefault] = useState(false);

  // CEFR Form State
  const [cefrCode, setCefrCode] = useState<CefrLevel>('B2');
  const [cefrDisplayName, setCefrDisplayName] = useState('CEFR B2 (Upper-Intermediate)');
  const [cefrGuidance, setCefrGuidance] = useState('');
  const [cefrOrder, setCefrOrder] = useState(4);

  // Speech Speed Form State
  const [editingSpeedId, setEditingSpeedId] = useState<string | null>(null);
  const [speedCode, setSpeedCode] = useState('');
  const [speedValue, setSpeedValue] = useState(1.0);
  const [speedLabel, setSpeedLabel] = useState('');
  const [speedDescVi, setSpeedDescVi] = useState('');
  const [speedCefrRange, setSpeedCefrRange] = useState('B1 - B2');
  const [speedIsDefault, setSpeedIsDefault] = useState(false);

  // Previewer State
  const [previewCefr, setPreviewCefr] = useState('B2');
  const [previewMode, setPreviewMode] = useState('technical_interview');
  const [previewVoice, setPreviewVoice] = useState('Aoede');
  const [previewTopic, setPreviewTopic] = useState('.NET 10, Clean Architecture, DDD & CQRS');
  const [previewOutput, setPreviewOutput] = useState<string>('');

  const filteredModes = useMemo(() => {
    if (!searchTerm.trim()) return displayedModes;
    const q = searchTerm.trim().toLowerCase();
    return displayedModes.filter(
      (m) =>
        m.displayName.toLowerCase().includes(q) ||
        m.modeCode.toLowerCase().includes(q) ||
        m.category.toLowerCase().includes(q)
    );
  }, [displayedModes, searchTerm]);

  const filteredCefr = useMemo(() => {
    if (!searchTerm.trim()) return displayedCefr;
    const q = searchTerm.trim().toLowerCase();
    return displayedCefr.filter(
      (c) =>
        c.cefrLevel.toLowerCase().includes(q) ||
        c.displayName.toLowerCase().includes(q) ||
        c.levelGuidance.toLowerCase().includes(q)
    );
  }, [displayedCefr, searchTerm]);

  const filteredSpeeds = useMemo(() => {
    if (!searchTerm.trim()) return displayedSpeeds;
    const q = searchTerm.trim().toLowerCase();
    return displayedSpeeds.filter(
      (s) =>
        s.code.toLowerCase().includes(q) ||
        s.label.toLowerCase().includes(q) ||
        s.descriptionVi.toLowerCase().includes(q)
    );
  }, [displayedSpeeds, searchTerm]);

  const openAddByActiveTab = () => {
    if (activeTab === 'modes') {
      setEditingModeCode(null);
      setModeCode('');
      setModeCategory('Interview & Coaching');
      setModeDisplayName('');
      setModeDirective('');
      setModeOrder(displayedModes.length + 1);
      setModeIsDefault(false);
      setIsModeDialogOpen(true);
    } else if (activeTab === 'cefr') {
      setCefrCode('B2');
      setCefrDisplayName('CEFR B2 (Upper-Intermediate)');
      setCefrGuidance('');
      setCefrOrder(4);
      setIsCefrDialogOpen(true);
    } else {
      setEditingSpeedId(null);
      setSpeedCode('');
      setSpeedValue(1.0);
      setSpeedLabel('');
      setSpeedDescVi('');
      setSpeedCefrRange('B1 - B2');
      setSpeedIsDefault(false);
      setIsSpeedDialogOpen(true);
    }
  };

  const handleSaveMode = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!modeCode.trim() || !modeDisplayName.trim()) return;
    const normCode = modeCode.trim().toLowerCase().replace(/\s+/g, '_');
    const payload = {
      modeCode: normCode,
      category: modeCategory,
      displayName: modeDisplayName.trim(),
      directiveTemplate: modeDirective.trim(),
      displayOrder: modeOrder,
      isDefault: modeIsDefault,
      isActive: true,
    };
    try {
      const saved = await upsertModeMutation.mutateAsync(payload);
      const exists = displayedModes.some((x) => x.modeCode === normCode);
      setLocalModes(
        exists
          ? displayedModes.map((x) => (x.modeCode === normCode ? saved : x))
          : [saved, ...displayedModes]
      );
    } catch {
      const item: ConversationModePromptDto = { id: crypto.randomUUID(), ...payload };
      const exists = displayedModes.some((x) => x.modeCode === normCode);
      setLocalModes(
        exists
          ? displayedModes.map((x) => (x.modeCode === normCode ? item : x))
          : [item, ...displayedModes]
      );
    }
    setFeedback({ type: 'success', message: `Đã lưu chế độ "${payload.displayName}".` });
    setIsModeDialogOpen(false);
  };

  const handleSaveCefr = async (e: React.FormEvent) => {
    e.preventDefault();
    const payload = {
      displayName: cefrDisplayName.trim() || `CEFR ${cefrCode}`,
      levelGuidance: cefrGuidance.trim(),
      displayOrder: cefrOrder,
      isActive: true,
    };
    try {
      const saved = await upsertCefrMutation.mutateAsync({ cefrLevel: cefrCode, payload });
      const exists = displayedCefr.some((x) => x.cefrLevel === cefrCode);
      setLocalCefr(
        exists
          ? displayedCefr.map((x) => (x.cefrLevel === cefrCode ? saved : x))
          : [...displayedCefr, saved]
      );
    } catch {
      const item: CefrLevelPromptDto = { id: crypto.randomUUID(), cefrLevel: cefrCode, ...payload };
      const exists = displayedCefr.some((x) => x.cefrLevel === cefrCode);
      setLocalCefr(
        exists
          ? displayedCefr.map((x) => (x.cefrLevel === cefrCode ? item : x))
          : [...displayedCefr, item]
      );
    }
    setFeedback({ type: 'success', message: `Đã cập nhật hướng dẫn CEFR ${cefrCode}.` });
    setIsCefrDialogOpen(false);
  };

  const handleSaveSpeed = async (e: React.FormEvent) => {
    e.preventDefault();
    const codeStr = speedCode.trim() || `${speedValue.toFixed(2)}x`;
    const payload = {
      code: codeStr,
      value: speedValue,
      label: speedLabel.trim() || `${codeStr} — Tùy chỉnh`,
      descriptionVi: speedDescVi.trim() || 'Tốc độ nói của Teacher lưu trong Database',
      recommendedCefrRange: speedCefrRange.trim() || 'A1 - C2',
      isDefault: speedIsDefault,
      isActive: true,
      displayOrder: displayedSpeeds.length + 1,
    };

    try {
      if (editingSpeedId) {
        const updated = await updateSpeedMutation.mutateAsync({ id: editingSpeedId, payload });
        setLocalSpeeds(displayedSpeeds.map((s) => (s.id === editingSpeedId ? updated : s)));
      } else {
        const created = await createSpeedMutation.mutateAsync(payload);
        setLocalSpeeds([...displayedSpeeds, created]);
      }
    } catch {
      const fallback: SpeechSpeedOptionDto = {
        id: editingSpeedId ?? crypto.randomUUID(),
        ...payload,
      };
      if (editingSpeedId) {
        setLocalSpeeds(displayedSpeeds.map((s) => (s.id === editingSpeedId ? fallback : s)));
      } else {
        setLocalSpeeds([...displayedSpeeds, fallback]);
      }
    }

    setFeedback({ type: 'success', message: `Đã lưu tốc độ nói "${payload.label}".` });
    setIsSpeedDialogOpen(false);
  };

  const handleRunPreview = async () => {
    const res = await adminApi.previewPrompt({
      cefrLevel: previewCefr,
      conversationMode: previewMode,
      voiceName: previewVoice,
      speechSpeed: 1.0,
      topic: previewTopic,
      extendedThinking: true,
    });
    setPreviewOutput(res);
  };

  const modeColumns: DataTableColumn<ConversationModePromptDto>[] = [
    {
      key: 'name',
      header: 'Chế độ Luyện nói (Mode)',
      render: (m) => (
        <div>
          <div className="font-bold text-slate-900">{m.displayName}</div>
          <div className="font-mono text-[11px] text-emerald-700">{m.modeCode}</div>
        </div>
      ),
    },
    {
      key: 'category',
      header: 'Nhóm',
      render: (m) => (
        <Badge variant={m.category === 'Interview & Coaching' ? 'amber' : 'sky'}>
          {m.category}
        </Badge>
      ),
    },
    {
      key: 'directive',
      header: 'Directive Template',
      render: (m) => (
        <p className="text-slate-600 font-mono text-[11px] line-clamp-2 max-w-md">
          {m.directiveTemplate}
        </p>
      ),
    },
    {
      key: 'actions',
      header: 'Thao tác',
      className: 'text-right whitespace-nowrap',
      render: (m) => (
        <div className="inline-flex items-center justify-end gap-1.5">
          <IconButton
            action="edit"
            size="sm"
            label={`Chỉnh sửa Mode ${m.displayName}`}
            onClick={() => {
              setEditingModeCode(m.modeCode);
              setModeCode(m.modeCode);
              setModeCategory(m.category);
              setModeDisplayName(m.displayName);
              setModeDirective(m.directiveTemplate);
              setModeOrder(m.displayOrder);
              setModeIsDefault(m.isDefault);
              setIsModeDialogOpen(true);
            }}
          />
          <IconButton
            action="delete"
            size="sm"
            label={`Xóa Mode ${m.displayName}`}
            onClick={() => setDeleteModeTarget(m)}
          />
        </div>
      ),
    },
  ];

  const cefrColumns: DataTableColumn<CefrLevelPromptDto>[] = [
    {
      key: 'level',
      header: 'Cấp độ CEFR',
      render: (c) => <Badge variant="emerald">{c.cefrLevel}</Badge>,
    },
    {
      key: 'displayName',
      header: 'Tên hiển thị',
      render: (c) => <span className="font-bold text-slate-900">{c.displayName}</span>,
    },
    {
      key: 'guidance',
      header: 'Level Guidance',
      render: (c) => (
        <p className="text-slate-600 font-mono text-[11px] max-w-xl">{c.levelGuidance}</p>
      ),
    },
    {
      key: 'actions',
      header: 'Thao tác',
      className: 'text-right whitespace-nowrap',
      render: (c) => (
        <div className="inline-flex items-center justify-end gap-1.5">
          <IconButton
            action="edit"
            size="sm"
            label={`Chỉnh sửa CEFR ${c.cefrLevel}`}
            onClick={() => {
              setCefrCode(c.cefrLevel);
              setCefrDisplayName(c.displayName);
              setCefrGuidance(c.levelGuidance);
              setCefrOrder(c.displayOrder);
              setIsCefrDialogOpen(true);
            }}
          />
          <IconButton
            action="delete"
            size="sm"
            label={`Xóa CEFR ${c.cefrLevel}`}
            onClick={() => setDeleteCefrTarget(c)}
          />
        </div>
      ),
    },
  ];

  const speedColumns: DataTableColumn<SpeechSpeedOptionDto>[] = [
    {
      key: 'code',
      header: 'Mã / Hệ số tốc độ',
      render: (s) => (
        <div className="flex items-center gap-2">
          <span className="font-mono font-bold text-emerald-700">
            {s.code} ({s.value.toFixed(2)}x)
          </span>
          {s.isDefault && <Badge variant="amber">Default</Badge>}
        </div>
      ),
    },
    {
      key: 'label',
      header: 'Nhãn hiển thị',
      render: (s) => <span className="font-bold text-slate-900">{s.label}</span>,
    },
    {
      key: 'desc',
      header: 'Mô tả Tiếng Việt',
      render: (s) => <span className="text-slate-600">{s.descriptionVi}</span>,
    },
    {
      key: 'cefr',
      header: 'Dải CEFR khuyến nghị',
      render: (s) => <Badge variant="emerald">{s.recommendedCefrRange}</Badge>,
    },
    {
      key: 'actions',
      header: 'Thao tác',
      className: 'text-right whitespace-nowrap',
      render: (s) => (
        <div className="inline-flex items-center justify-end gap-1.5">
          <IconButton
            action="edit"
            size="sm"
            label={`Chỉnh sửa tốc độ ${s.label}`}
            onClick={() => {
              setEditingSpeedId(s.id);
              setSpeedCode(s.code);
              setSpeedValue(s.value);
              setSpeedLabel(s.label);
              setSpeedDescVi(s.descriptionVi);
              setSpeedCefrRange(s.recommendedCefrRange);
              setSpeedIsDefault(s.isDefault);
              setIsSpeedDialogOpen(true);
            }}
          />
          <IconButton
            action="delete"
            size="sm"
            label={`Xóa tốc độ ${s.label}`}
            onClick={() => setDeleteSpeedTarget(s)}
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
            <img src="/assets/icons/navigation/nav-prompts.svg" alt="" className="w-6 h-6 object-contain" />
          </div>
          <div>
            <h1 className="text-lg font-extrabold text-slate-900">
              Quản lý CEFR, 19 Modes & Tốc độ nói trong Database
            </h1>
            <p className="text-xs text-slate-500">
              Cấu hình `CefrPromptBuilder` và bảng `SpeechSpeeds` lưu trữ trực tiếp trong SQL Server
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => {
              setIsPreviewDialogOpen(true);
              void handleRunPreview();
            }}
          >
            <Eye className="w-4 h-4 text-emerald-700" />
            Xem thử System Prompt
          </Button>
          <Badge variant="emerald">Permission: prompts.manage</Badge>
        </div>
      </div>

      {feedback && (
        <FeedbackBanner
          type={feedback.type}
          message={feedback.message}
          onClose={() => setFeedback(null)}
        />
      )}

      {/* 2. Toolbar (Tabs + Search + Add New) */}
      <div className="bg-white rounded-2xl border border-slate-200 p-4 flex flex-wrap items-center justify-between gap-3 shadow-2xs">
        <div className="flex flex-wrap items-center gap-2">
          <Button
            variant={activeTab === 'modes' ? 'primary' : 'outline'}
            size="sm"
            onClick={() => {
              setActiveTab('modes');
              setPage(1);
            }}
          >
            <img src="/assets/icons/navigation/nav-prompts.svg" alt="" className="w-4 h-4 object-contain" />
            Chế độ Luyện nói ({displayedModes.length})
          </Button>
          <Button
            variant={activeTab === 'cefr' ? 'primary' : 'outline'}
            size="sm"
            onClick={() => {
              setActiveTab('cefr');
              setPage(1);
            }}
          >
            <img src="/assets/icons/navigation/nav-study.svg" alt="" className="w-4 h-4 object-contain" />
            Trình độ CEFR ({displayedCefr.length})
          </Button>
          <Button
            variant={activeTab === 'speeds' ? 'primary' : 'outline'}
            size="sm"
            onClick={() => {
              setActiveTab('speeds');
              setPage(1);
            }}
          >
            <img src="/assets/icons/navigation/nav-voices.svg" alt="" className="w-4 h-4 object-contain" />
            Tốc độ nói ({displayedSpeeds.length})
          </Button>
        </div>

        <div className="flex items-center gap-2.5 flex-1 justify-end min-w-[240px]">
          <div className="relative max-w-xs w-full">
            <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => {
                setSearchTerm(e.target.value);
                setPage(1);
              }}
              placeholder="Tìm kiếm..."
              className="w-full pl-9 pr-3 py-2 rounded-xl border border-slate-200 text-xs"
            />
          </div>

          <Button variant="primary" size="sm" onClick={openAddByActiveTab}>
            <Plus className="w-4 h-4" />
            {activeTab === 'modes'
              ? 'Add New Mode'
              : activeTab === 'cefr'
              ? 'Update CEFR'
              : 'Add Speech Speed'}
          </Button>
        </div>
      </div>

      {/* 3. DataTable + 4. Pagination per Active Tab */}
      {activeTab === 'modes' && (
        <DataTable
          columns={modeColumns}
          data={filteredModes}
          rowKey={(r) => r.modeCode}
          page={page}
          pageSize={pageSize}
          onPageChange={setPage}
          onPageSizeChange={setPageSize}
        />
      )}

      {activeTab === 'cefr' && (
        <DataTable
          columns={cefrColumns}
          data={filteredCefr}
          rowKey={(r) => r.cefrLevel}
          page={page}
          pageSize={pageSize}
          onPageChange={setPage}
          onPageSizeChange={setPageSize}
        />
      )}

      {activeTab === 'speeds' && (
        <DataTable
          columns={speedColumns}
          data={filteredSpeeds}
          rowKey={(r) => r.id || r.code}
          page={page}
          pageSize={pageSize}
          onPageChange={setPage}
          onPageSizeChange={setPageSize}
        />
      )}

      {/* DIALOG 1: Mode Create / Edit */}
      <Dialog
        isOpen={isModeDialogOpen}
        onClose={() => setIsModeDialogOpen(false)}
        maxWidth="2xl"
        icon={
          <img
            src="/assets/icons/navigation/nav-prompts.svg"
            alt=""
            className="w-5 h-5 object-contain"
          />
        }
        title={editingModeCode ? `Chỉnh sửa Mode: ${editingModeCode}` : 'Thêm mới Chế độ Luyện nói'}
        footer={
          <div className="flex items-center justify-end gap-2 w-full">
            <Button variant="outline" size="sm" onClick={() => setIsModeDialogOpen(false)}>
              Cancel
            </Button>
            <Button variant="primary" size="sm" onClick={(e) => void handleSaveMode(e)}>
              <Save className="w-4 h-4" />
              Save Mode
            </Button>
          </div>
        }
      >
        <form onSubmit={handleSaveMode} className="space-y-4 text-xs">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <FormField label="Mã Mode (modeCode)" required>
              <input
                type="text"
                value={modeCode}
                disabled={Boolean(editingModeCode)}
                onChange={(e) => setModeCode(e.target.value)}
                placeholder="vd: ielts_speaking_part2"
                className="w-full px-3 py-2 rounded-xl border border-slate-200 font-mono disabled:bg-slate-100"
              />
            </FormField>
            <FormField label="Nhóm (Category)">
              <select
                value={modeCategory}
                onChange={(e) => setModeCategory(e.target.value)}
                className="w-full px-3 py-2 rounded-xl border border-slate-200 bg-white"
              >
                <option value="Interview & Coaching">Interview & Coaching</option>
                <option value="Communication & Workplace">Communication & Workplace</option>
              </select>
            </FormField>
          </div>

          <FormField label="Tên hiển thị (Display Name)" required>
            <input
              type="text"
              value={modeDisplayName}
              onChange={(e) => setModeDisplayName(e.target.value)}
              placeholder="vd: IELTS Speaking Part 2 Cue Card Coach"
              className="w-full px-3 py-2 rounded-xl border border-slate-200"
            />
          </FormField>

          <FormField label="Chỉ dẫn Sư phạm cho AI (Directive Template)" required>
            <textarea
              rows={4}
              value={modeDirective}
              onChange={(e) => setModeDirective(e.target.value)}
              className="w-full px-3 py-2 rounded-xl border border-slate-200 font-mono"
            />
          </FormField>
        </form>
      </Dialog>

      {/* DIALOG 2: CEFR Create / Edit */}
      <Dialog
        isOpen={isCefrDialogOpen}
        onClose={() => setIsCefrDialogOpen(false)}
        maxWidth="xl"
        icon={
          <img
            src="/assets/icons/navigation/nav-study.svg"
            alt=""
            className="w-5 h-5 object-contain"
          />
        }
        title={`Cấu hình Hướng dẫn Sư phạm CEFR ${cefrCode}`}
        footer={
          <div className="flex items-center justify-end gap-2 w-full">
            <Button variant="outline" size="sm" onClick={() => setIsCefrDialogOpen(false)}>
              Cancel
            </Button>
            <Button variant="primary" size="sm" onClick={(e) => void handleSaveCefr(e)}>
              <Save className="w-4 h-4" />
              Save CEFR Prompt
            </Button>
          </div>
        }
      >
        <form onSubmit={handleSaveCefr} className="space-y-4 text-xs">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <FormField label="Mức CEFR" required>
              <select
                value={cefrCode}
                onChange={(e) => setCefrCode(e.target.value as CefrLevel)}
                className="w-full px-3 py-2 rounded-xl border border-slate-200 bg-white font-bold"
              >
                {(['A1', 'A2', 'B1', 'B2', 'C1', 'C2'] as CefrLevel[]).map((l) => (
                  <option key={l} value={l}>
                    {l}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField label="Tên hiển thị" required>
              <input
                type="text"
                value={cefrDisplayName}
                onChange={(e) => setCefrDisplayName(e.target.value)}
                className="w-full px-3 py-2 rounded-xl border border-slate-200"
              />
            </FormField>
          </div>

          <FormField label="Hướng dẫn Sư phạm (Level Guidance)" required>
            <textarea
              rows={4}
              value={cefrGuidance}
              onChange={(e) => setCefrGuidance(e.target.value)}
              className="w-full px-3 py-2 rounded-xl border border-slate-200 font-mono"
            />
          </FormField>
        </form>
      </Dialog>

      {/* DIALOG 3: Speech Speed Create / Edit */}
      <Dialog
        isOpen={isSpeedDialogOpen}
        onClose={() => setIsSpeedDialogOpen(false)}
        maxWidth="xl"
        icon={
          <img
            src="/assets/icons/navigation/nav-voices.svg"
            alt=""
            className="w-5 h-5 object-contain"
          />
        }
        title={
          editingSpeedId
            ? `Chỉnh sửa Tốc độ nói: ${speedCode}`
            : 'Thêm mới Tốc độ nói của Teacher (DB)'
        }
        footer={
          <div className="flex items-center justify-end gap-2 w-full">
            <Button variant="outline" size="sm" onClick={() => setIsSpeedDialogOpen(false)}>
              Cancel
            </Button>
            <Button variant="primary" size="sm" onClick={(e) => void handleSaveSpeed(e)}>
              <Save className="w-4 h-4" />
              Save Speech Speed
            </Button>
          </div>
        }
      >
        <form onSubmit={handleSaveSpeed} className="space-y-4 text-xs">
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <FormField label="Mã tốc độ (Code)" required>
              <input
                type="text"
                value={speedCode}
                onChange={(e) => setSpeedCode(e.target.value)}
                placeholder="vd: 0.85x"
                className="w-full px-3 py-2 rounded-xl border border-slate-200 font-mono"
              />
            </FormField>
            <FormField label="Hệ số (0.5 – 2.0)" required>
              <input
                type="number"
                step="0.05"
                min="0.5"
                max="2.0"
                value={speedValue}
                onChange={(e) => setSpeedValue(parseFloat(e.target.value) || 1.0)}
                className="w-full px-3 py-2 rounded-xl border border-slate-200"
              />
            </FormField>
            <FormField label="Dải CEFR khuyến nghị">
              <input
                type="text"
                value={speedCefrRange}
                onChange={(e) => setSpeedCefrRange(e.target.value)}
                placeholder="A2 - B1"
                className="w-full px-3 py-2 rounded-xl border border-slate-200"
              />
            </FormField>
          </div>

          <FormField label="Nhãn hiển thị (Label)" required>
            <input
              type="text"
              value={speedLabel}
              onChange={(e) => setSpeedLabel(e.target.value)}
              placeholder="vd: 0.85x — Chậm vừa phải"
              className="w-full px-3 py-2 rounded-xl border border-slate-200"
            />
          </FormField>

          <FormField label="Mô tả Tiếng Việt">
            <input
              type="text"
              value={speedDescVi}
              onChange={(e) => setSpeedDescVi(e.target.value)}
              className="w-full px-3 py-2 rounded-xl border border-slate-200"
            />
          </FormField>

          <label className="flex items-center gap-2 font-bold text-slate-700 cursor-pointer">
            <input
              type="checkbox"
              checked={speedIsDefault}
              onChange={(e) => setSpeedIsDefault(e.target.checked)}
            />
            Đặt làm tốc độ mặc định
          </label>
        </form>
      </Dialog>

      {/* DIALOG 4: System Prompt Previewer */}
      <Dialog
        isOpen={isPreviewDialogOpen}
        onClose={() => setIsPreviewDialogOpen(false)}
        maxWidth="3xl"
        icon={
          <img
            src="/assets/icons/navigation/nav-prompts.svg"
            alt=""
            className="w-5 h-5 object-contain"
          />
        }
        title="Trình tạo thử System Instruction (POST /api/prompts/preview)"
        description="Kiểm tra kết quả ghép nối CEFR Level Guidance + Conversation Mode + Teacher Voice từ Database"
        footer={
          <div className="flex items-center justify-end gap-2 w-full">
            <Button variant="outline" size="sm" onClick={() => setIsPreviewDialogOpen(false)}>
              Đóng
            </Button>
            <Button variant="primary" size="sm" onClick={() => void handleRunPreview()}>
              <Sparkles className="w-4 h-4" />
              Kết xuất lại Prompt
            </Button>
          </div>
        }
      >
        <div className="space-y-4 text-xs">
          <div className="grid grid-cols-1 sm:grid-cols-4 gap-3">
            <select
              value={previewCefr}
              onChange={(e) => setPreviewCefr(e.target.value)}
              className="px-3 py-2 rounded-xl border border-slate-200 bg-white font-semibold"
            >
              {displayedCefr.map((c) => (
                <option key={c.cefrLevel} value={c.cefrLevel}>
                  {c.displayName}
                </option>
              ))}
            </select>

            <select
              value={previewMode}
              onChange={(e) => setPreviewMode(e.target.value)}
              className="px-3 py-2 rounded-xl border border-slate-200 bg-white font-semibold"
            >
              {displayedModes.map((m) => (
                <option key={m.modeCode} value={m.modeCode}>
                  {m.displayName}
                </option>
              ))}
            </select>

            <select
              value={previewVoice}
              onChange={(e) => setPreviewVoice(e.target.value)}
              className="px-3 py-2 rounded-xl border border-slate-200 bg-white font-semibold"
            >
              {voices.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.label}
                </option>
              ))}
            </select>

            <input
              type="text"
              value={previewTopic}
              onChange={(e) => setPreviewTopic(e.target.value)}
              className="px-3 py-2 rounded-xl border border-slate-200"
              placeholder="Topic..."
            />
          </div>

          <pre className="p-4 rounded-2xl bg-slate-900 text-emerald-300 text-xs font-mono whitespace-pre-wrap max-h-80 overflow-y-auto">
            {previewOutput || 'Nhấn Kết xuất lại Prompt...'}
          </pre>
        </div>
      </Dialog>

      {/* Delete Confirm Dialogs */}
      <ConfirmDialog
        isOpen={Boolean(deleteModeTarget)}
        onClose={() => setDeleteModeTarget(null)}
        onConfirm={() => {
          if (!deleteModeTarget) return;
          void deleteModeMutation.mutateAsync(deleteModeTarget.modeCode).catch(() => {});
          setLocalModes(displayedModes.filter((x) => x.modeCode !== deleteModeTarget.modeCode));
          setDeleteModeTarget(null);
        }}
        title="Xóa Chế độ Luyện nói"
        targetName={deleteModeTarget?.displayName || ''}
      />

      <ConfirmDialog
        isOpen={Boolean(deleteCefrTarget)}
        onClose={() => setDeleteCefrTarget(null)}
        onConfirm={() => {
          if (!deleteCefrTarget) return;
          void deleteCefrMutation.mutateAsync(deleteCefrTarget.cefrLevel).catch(() => {});
          setLocalCefr(displayedCefr.filter((x) => x.cefrLevel !== deleteCefrTarget.cefrLevel));
          setDeleteCefrTarget(null);
        }}
        title="Xóa Cấu hình CEFR"
        targetName={deleteCefrTarget?.displayName || ''}
      />

      <ConfirmDialog
        isOpen={Boolean(deleteSpeedTarget)}
        onClose={() => setDeleteSpeedTarget(null)}
        onConfirm={() => {
          if (!deleteSpeedTarget) return;
          if (deleteSpeedTarget.id) {
            void deleteSpeedMutation.mutateAsync(deleteSpeedTarget.id).catch(() => {});
          }
          setLocalSpeeds(displayedSpeeds.filter((x) => x.code !== deleteSpeedTarget.code));
          setDeleteSpeedTarget(null);
        }}
        title="Xóa Cấu hình Tốc độ nói"
        targetName={deleteSpeedTarget?.label || ''}
      />
    </div>
  );
};
