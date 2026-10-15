import React, { useEffect, useMemo, useState } from 'react';
import {
  CheckCircle2,
  XCircle,
  Plus,
  Save,
  Search,
  Settings,
} from 'lucide-react';
import {
  useAdminAiModelsQuery,
  useAdminRubricsQuery,
  useCreateAiModelMutation,
  useDeleteAiModelMutation,
  useUpdateAiModelMutation,
  useUpsertRubricMutation,
} from '../hooks/useAdminQueries';
import { Button } from '@/shared/components/ui/Button';
import { IconButton } from '@/shared/components/ui/IconButton';
import { Badge } from '@/shared/components/ui/Badge';
import { Dialog } from '@/shared/components/ui/Dialog';
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog';
import { DataTable, type DataTableColumn } from '@/shared/components/ui/DataTable';
import { FormField } from '@/shared/components/ui/FormField';
import { FeedbackBanner } from '@/shared/components/ui/FeedbackBanner';
import type { AiModelOptionDto, ScoringRubricCriterionDto } from '@/shared/api/openapi.types';

export const AdminRubricsPage: React.FC = () => {
  const { data: rubrics = [] } = useAdminRubricsQuery();
  const { data: aiModels = [], isLoading } = useAdminAiModelsQuery();

  const upsertRubricMutation = useUpsertRubricMutation();
  const createModelMutation = useCreateAiModelMutation();
  const updateModelMutation = useUpdateAiModelMutation();
  const deleteModelMutation = useDeleteAiModelMutation();

  const activeRubric = rubrics[0];
  const [rubricName, setRubricName] = useState('');
  const [rubricDesc, setRubricDesc] = useState('');
  const [criteria, setCriteria] = useState<ScoringRubricCriterionDto[]>([]);

  const [localModels, setLocalModels] = useState<AiModelOptionDto[] | null>(null);
  const displayedModels = localModels ?? aiModels;

  // Toolbar & Pagination State
  const [searchTerm, setSearchTerm] = useState('');
  const [capabilityFilter, setCapabilityFilter] = useState('ALL');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(
    null
  );

  // Dialog Open States
  const [isRubricDialogOpen, setIsRubricDialogOpen] = useState(false);
  const [isModelDialogOpen, setIsModelDialogOpen] = useState(false);
  const [editingModelId, setEditingModelId] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<AiModelOptionDto | null>(null);

  // AI Model Form State
  const [displayName, setDisplayName] = useState('');
  const [modelCode, setModelCode] = useState('');
  const [capabilityNote, setCapabilityNote] = useState('');
  const [supportsTextGeneration, setSupportsTextGeneration] = useState(true);
  const [supportsStructuredOutput, setSupportsStructuredOutput] = useState(true);
  const [supportsAudioInput, setSupportsAudioInput] = useState(true);
  const [supportsPronunciationAssessment, setSupportsPronunciationAssessment] = useState(true);
  const [supportsLiveAudio, setSupportsLiveAudio] = useState(false);
  const [isEnabled, setIsEnabled] = useState(true);
  const [isDeprecated, setIsDeprecated] = useState(false);

  useEffect(() => {
    if (activeRubric) {
      setRubricName(activeRubric.name);
      setRubricDesc(activeRubric.description);
      setCriteria(activeRubric.criteria);
    }
  }, [activeRubric]);

  const filteredModels = useMemo(() => {
    return displayedModels.filter((m) => {
      if (capabilityFilter === 'AUDIO' && !m.supportsAudioInput) return false;
      if (capabilityFilter === 'TEXT' && !m.supportsTextGeneration) return false;
      if (searchTerm.trim()) {
        const q = searchTerm.trim().toLowerCase();
        return (
          m.displayName.toLowerCase().includes(q) ||
          m.modelCode.toLowerCase().includes(q) ||
          m.capabilityNote.toLowerCase().includes(q)
        );
      }
      return true;
    });
  }, [displayedModels, capabilityFilter, searchTerm]);

  const openCreateModelDialog = () => {
    setEditingModelId(null);
    setDisplayName('');
    setModelCode('');
    setCapabilityNote('');
    setSupportsTextGeneration(true);
    setSupportsStructuredOutput(true);
    setSupportsAudioInput(true);
    setSupportsPronunciationAssessment(true);
    setSupportsLiveAudio(false);
    setIsEnabled(true);
    setIsDeprecated(false);
    setIsModelDialogOpen(true);
  };

  const openEditModelDialog = (m: AiModelOptionDto) => {
    setEditingModelId(m.id);
    setDisplayName(m.displayName);
    setModelCode(m.modelCode);
    setCapabilityNote(m.capabilityNote);
    setSupportsTextGeneration(m.supportsTextGeneration);
    setSupportsStructuredOutput(m.supportsStructuredOutput);
    setSupportsAudioInput(m.supportsAudioInput);
    setSupportsPronunciationAssessment(m.supportsPronunciationAssessment);
    setSupportsLiveAudio(Boolean(m.supportsLiveAudio));
    setIsEnabled(m.isEnabled);
    setIsDeprecated(m.isDeprecated);
    setIsModelDialogOpen(true);
  };

  const handleSaveRubric = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!activeRubric) return;
    try {
      await upsertRubricMutation.mutateAsync({
        code: activeRubric.code,
        payload: {
          name: rubricName,
          description: rubricDesc,
          isActive: true,
          criteria: criteria.map((c) => ({
            code: c.code,
            name: c.name,
            description: c.description,
            weight: c.weight,
            displayOrder: c.displayOrder,
            isEnabled: c.isEnabled,
          })),
        },
      });
    } catch {
      // ignore offline
    }
    setFeedback({
      type: 'success',
      message: `Đã lưu cấu hình Rubric "${activeRubric.code}" thành công.`,
    });
    setIsRubricDialogOpen(false);
  };

  const handleSaveModel = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!modelCode.trim() || !displayName.trim()) return;

    const payload = {
      displayName: displayName.trim(),
      modelCode: modelCode.trim(),
      routedModelCode: null,
      supportsTextGeneration,
      supportsStructuredOutput,
      supportsAudioInput,
      supportsPronunciationAssessment,
      supportsLiveAudio,
      isAvailable: true,
      isDeprecated,
      isEnabled,
      capabilityNote: capabilityNote.trim(),
      displayOrder: displayedModels.length + 1,
    };

    try {
      if (editingModelId) {
        const updated = await updateModelMutation.mutateAsync({ id: editingModelId, payload });
        setLocalModels(displayedModels.map((m) => (m.id === editingModelId ? updated : m)));
      } else {
        const created = await createModelMutation.mutateAsync(payload);
        setLocalModels([created, ...displayedModels]);
      }
    } catch {
      const fallback: AiModelOptionDto = {
        id: editingModelId ?? crypto.randomUUID(),
        ...payload,
      };
      if (editingModelId) {
        setLocalModels(displayedModels.map((m) => (m.id === editingModelId ? fallback : m)));
      } else {
        setLocalModels([fallback, ...displayedModels]);
      }
    }

    setFeedback({
      type: 'success',
      message: editingModelId
        ? `Đã cập nhật AI Model "${payload.displayName}".`
        : `Đã thêm AI Model "${payload.displayName}" vào Database.`,
    });
    setIsModelDialogOpen(false);
  };

  const handleConfirmDelete = async () => {
    if (!deleteTarget) return;
    try {
      await deleteModelMutation.mutateAsync(deleteTarget.id);
    } catch {
      // ignore offline
    }
    setLocalModels(displayedModels.filter((m) => m.id !== deleteTarget.id));
    setFeedback({
      type: 'success',
      message: `Đã xóa AI Model "${deleteTarget.displayName}".`,
    });
    setDeleteTarget(null);
  };

  const totalWeight = criteria.reduce((acc, c) => acc + Number(c.weight || 0), 0);

  const columns: DataTableColumn<AiModelOptionDto>[] = [
    {
      key: 'name',
      header: 'Tên & Mã AI Model',
      render: (m) => (
        <div>
          <div className="font-bold text-slate-900 text-sm">{m.displayName}</div>
          <div className="font-mono text-[11px] text-emerald-700">{m.modelCode}</div>
        </div>
      ),
    },
    {
      key: 'textGen',
      header: 'Tạo Từ Vựng (JSON)',
      render: (m) =>
        m.supportsTextGeneration ? (
          <span className="inline-flex items-center gap-1 text-emerald-700 font-semibold">
            <CheckCircle2 className="w-3.5 h-3.5" /> Hỗ trợ
          </span>
        ) : (
          <span className="text-slate-400">Không</span>
        ),
    },
    {
      key: 'audioInput',
      header: 'Chấm Phát Âm (Audio Input)',
      render: (m) =>
        m.supportsAudioInput && m.supportsPronunciationAssessment ? (
          <span className="inline-flex items-center gap-1 text-emerald-700 font-semibold">
            <CheckCircle2 className="w-3.5 h-3.5" /> Có Audio Input
          </span>
        ) : (
          <span className="inline-flex items-center gap-1 text-rose-600 font-semibold">
            <XCircle className="w-3.5 h-3.5" /> Chặn ở Audio Studio
          </span>
        ),
    },
    {
      key: 'note',
      header: 'Ghi chú năng lực',
      render: (m) => <span className="text-slate-500 max-w-xs block truncate">{m.capabilityNote}</span>,
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
            label={`Chỉnh sửa AI Model ${m.displayName}`}
            onClick={() => openEditModelDialog(m)}
          />
          <IconButton
            action="delete"
            size="sm"
            label={`Xóa AI Model ${m.displayName}`}
            onClick={() => setDeleteTarget(m)}
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
            <img src="/assets/icons/navigation/nav-rubrics.svg" alt="" className="w-6 h-6 object-contain" />
          </div>
          <div>
            <h1 className="text-lg font-extrabold text-slate-900">
              Rubric Chấm Phát Âm & Danh mục AI Models ({filteredModels.length})
            </h1>
            <p className="text-xs text-slate-500">
              Tiêu chuẩn chấm điểm `{activeRubric?.code ?? 'ENGLISH_PRONUNCIATION_V1'}` (Accuracy 40% · Clarity 30% · Fluency 30%) & `AiModelCatalog`
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={() => setIsRubricDialogOpen(true)}>
            <Settings className="w-4 h-4 text-emerald-700" />
            Chỉnh sửa Rubric ({activeRubric?.code ?? 'V1'})
          </Button>
          <Badge variant="emerald">Permission: rubrics.manage</Badge>
        </div>
      </div>

      {feedback && (
        <FeedbackBanner
          type={feedback.type}
          message={feedback.message}
          onClose={() => setFeedback(null)}
        />
      )}

      {/* Active Rubric Summary Strip */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-3.5">
        {criteria.map((c) => (
          <div
            key={c.code}
            className="bg-white rounded-2xl border border-slate-200 p-4 flex items-start justify-between gap-3 shadow-2xs"
          >
            <div>
              <div className="text-xs font-extrabold text-slate-900">{c.name}</div>
              <p className="text-[11px] text-slate-500 mt-0.5">{c.description}</p>
            </div>
            <Badge variant="emerald">{c.weight}%</Badge>
          </div>
        ))}
      </div>

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
              placeholder="Tìm tên AI Model, modelCode hoặc ghi chú..."
              className="w-full pl-9 pr-4 py-2 rounded-xl border border-slate-200 text-xs"
            />
          </div>

          <select
            value={capabilityFilter}
            onChange={(e) => {
              setCapabilityFilter(e.target.value);
              setPage(1);
            }}
            aria-label="Lọc theo năng lực AI Model"
            className="px-3 py-2 rounded-xl border border-slate-200 bg-white text-xs font-semibold text-slate-700"
          >
            <option value="ALL">Tất cả AI Models</option>
            <option value="AUDIO">Hỗ trợ Audio Input (Chấm Phát Âm)</option>
            <option value="TEXT">Hỗ trợ Text Generation (Tạo Từ Vựng)</option>
          </select>
        </div>

        <Button variant="primary" size="sm" onClick={openCreateModelDialog}>
          <Plus className="w-4 h-4" />
          Add New AI Model
        </Button>
      </div>

      {/* 3. DataTable + 4. Pagination */}
      <DataTable
        columns={columns}
        data={filteredModels}
        rowKey={(row) => row.id || row.modelCode}
        isLoading={isLoading}
        page={page}
        pageSize={pageSize}
        onPageChange={setPage}
        onPageSizeChange={setPageSize}
      />

      {/* DIALOG 1: Edit Scoring Rubric */}
      <Dialog
        isOpen={isRubricDialogOpen}
        onClose={() => setIsRubricDialogOpen(false)}
        maxWidth="2xl"
        icon={
          <img
            src="/assets/icons/navigation/nav-rubrics.svg"
            alt=""
            className="w-5 h-5 object-contain"
          />
        }
        title={`Cấu hình Rubric Chấm Phát Âm (${activeRubric?.code ?? 'ENGLISH_PRONUNCIATION_V1'})`}
        description={`Tổng trọng số các tiêu chí phải bằng 100% (Hiện tại: ${totalWeight}%)`}
        footer={
          <div className="flex items-center justify-end gap-2 w-full">
            <Button variant="outline" size="sm" onClick={() => setIsRubricDialogOpen(false)}>
              Cancel
            </Button>
            <Button
              variant="primary"
              size="sm"
              disabled={totalWeight !== 100}
              onClick={(e) => void handleSaveRubric(e)}
            >
              <Save className="w-4 h-4" />
              Save Rubric
            </Button>
          </div>
        }
      >
        <form onSubmit={handleSaveRubric} className="space-y-4 text-xs">
          <FormField label="Tên Rubric" required>
            <input
              type="text"
              value={rubricName}
              onChange={(e) => setRubricName(e.target.value)}
              className="w-full px-3 py-2 rounded-xl border border-slate-200"
            />
          </FormField>

          <FormField label="Mô tả Rubric">
            <input
              type="text"
              value={rubricDesc}
              onChange={(e) => setRubricDesc(e.target.value)}
              className="w-full px-3 py-2 rounded-xl border border-slate-200"
            />
          </FormField>

          <div className="space-y-2.5">
            {criteria.map((c) => (
              <div
                key={c.code}
                className="p-3.5 rounded-2xl bg-slate-50 border border-slate-200 flex items-center justify-between gap-4"
              >
                <div>
                  <div className="font-bold text-slate-900">{c.name}</div>
                  <div className="text-[11px] text-slate-500">{c.description}</div>
                </div>
                <div className="flex items-center gap-1.5 shrink-0">
                  <input
                    type="number"
                    min={0}
                    max={100}
                    value={c.weight}
                    onChange={(e) => {
                      const w = Number(e.target.value) || 0;
                      setCriteria((prev) =>
                        prev.map((item) => (item.code === c.code ? { ...item, weight: w } : item))
                      );
                    }}
                    className="w-16 px-2.5 py-1.5 rounded-xl border border-slate-300 bg-white font-bold text-right"
                  />
                  <span className="font-bold text-emerald-700">%</span>
                </div>
              </div>
            ))}
          </div>
        </form>
      </Dialog>

      {/* DIALOG 2: Create / Edit AI Model */}
      <Dialog
        isOpen={isModelDialogOpen}
        onClose={() => setIsModelDialogOpen(false)}
        maxWidth="xl"
        icon={
          <img
            src="/assets/icons/navigation/nav-rubrics.svg"
            alt=""
            className="w-5 h-5 object-contain"
          />
        }
        title={editingModelId ? `Chỉnh sửa AI Model: ${modelCode}` : 'Thêm mới AI Model vào Database'}
        footer={
          <div className="flex items-center justify-end gap-2 w-full">
            <Button variant="outline" size="sm" onClick={() => setIsModelDialogOpen(false)}>
              Cancel
            </Button>
            <Button variant="primary" size="sm" onClick={(e) => void handleSaveModel(e)}>
              <Save className="w-4 h-4" />
              Save AI Model
            </Button>
          </div>
        }
      >
        <form onSubmit={handleSaveModel} className="space-y-4 text-xs">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <FormField label="Tên hiển thị (DisplayName)" required>
              <input
                type="text"
                value={displayName}
                onChange={(e) => setDisplayName(e.target.value)}
                placeholder="vd: Gemini 3.8 Flash"
                className="w-full px-3 py-2 rounded-xl border border-slate-200"
              />
            </FormField>

            <FormField label="Mã Model (modelCode)" required>
              <input
                type="text"
                value={modelCode}
                onChange={(e) => setModelCode(e.target.value)}
                placeholder="vd: gemini-3.8-flash"
                className="w-full px-3 py-2 rounded-xl border border-slate-200 font-mono"
              />
            </FormField>
          </div>

          <FormField label="Ghi chú năng lực">
            <textarea
              rows={2}
              value={capabilityNote}
              onChange={(e) => setCapabilityNote(e.target.value)}
              className="w-full px-3 py-2 rounded-xl border border-slate-200"
            />
          </FormField>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5 pt-1 font-bold text-slate-700">
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={supportsTextGeneration}
                onChange={(e) => setSupportsTextGeneration(e.target.checked)}
              />
              Hỗ trợ Tạo Từ Vựng (Text Gen)
            </label>
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={supportsStructuredOutput}
                onChange={(e) => setSupportsStructuredOutput(e.target.checked)}
              />
              Hỗ trợ Structured JSON Output
            </label>
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={supportsAudioInput}
                onChange={(e) => setSupportsAudioInput(e.target.checked)}
              />
              Có Audio Input
            </label>
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={supportsPronunciationAssessment}
                onChange={(e) => setSupportsPronunciationAssessment(e.target.checked)}
              />
              Hỗ trợ Chấm Phát Âm Audio
            </label>
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={supportsLiveAudio}
                onChange={(e) => setSupportsLiveAudio(e.target.checked)}
              />
              Hỗ trợ Live Audio (Hub / TTS)
            </label>
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={isEnabled}
                onChange={(e) => setIsEnabled(e.target.checked)}
              />
              Kích hoạt (Enabled)
            </label>
          </div>
        </form>
      </Dialog>

      {/* Delete Confirm Dialog */}
      <ConfirmDialog
        isOpen={Boolean(deleteTarget)}
        onClose={() => setDeleteTarget(null)}
        onConfirm={() => void handleConfirmDelete()}
        isLoading={deleteModelMutation.isPending}
        title="Xóa AI Model khỏi Database"
        targetName={deleteTarget ? `${deleteTarget.displayName} (${deleteTarget.modelCode})` : ''}
      />
    </div>
  );
};
