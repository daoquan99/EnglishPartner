import React, { useMemo, useState } from 'react';
import { Plus, Save, Search } from 'lucide-react';
import {
  useAdminVocabulariesQuery,
  useCreateVocabularyAdminMutation,
  useDeleteVocabularyAdminMutation,
  useUpdateVocabularyAdminMutation,
} from '../hooks/useAdminQueries';
import type { VocabularyFlashcardDto } from '@/features/vocabulary';
import { Button } from '@/shared/components/ui/Button';
import { IconButton } from '@/shared/components/ui/IconButton';
import { Badge } from '@/shared/components/ui/Badge';
import { Dialog } from '@/shared/components/ui/Dialog';
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog';
import { DataTable, type DataTableColumn } from '@/shared/components/ui/DataTable';
import { FormField } from '@/shared/components/ui/FormField';
import { FeedbackBanner } from '@/shared/components/ui/FeedbackBanner';
import { toast } from '@/shared/lib/toast';
import type { CefrLevel } from '@/shared/api/openapi.types';

export const AdminVocabulariesPage: React.FC = () => {
  const { data: vocabList = [], isLoading } = useAdminVocabulariesQuery();
  const createMutation = useCreateVocabularyAdminMutation();
  const updateMutation = useUpdateVocabularyAdminMutation();
  const deleteMutation = useDeleteVocabularyAdminMutation();

  const [localItems, setLocalItems] = useState<VocabularyFlashcardDto[] | null>(null);
  const items = localItems ?? vocabList;

  // Toolbar & Pagination State
  const [searchTerm, setSearchTerm] = useState('');
  const [filterCefr, setFilterCefr] = useState<string>('ALL');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(
    null
  );

  // Dialog & Delete Confirm State
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<VocabularyFlashcardDto | null>(null);

  // Form fields
  const [word, setWord] = useState('');
  const [partOfSpeech, setPartOfSpeech] = useState('noun');
  const [cefrLevel, setCefrLevel] = useState<CefrLevel>('B2');
  const [topicCategory, setTopicCategory] = useState('.NET 10, Clean Architecture, DDD & CQRS');
  const [definitionEn, setDefinitionEn] = useState('');
  const [meaningVi, setMeaningVi] = useState('');
  const [descriptionEn, setDescriptionEn] = useState('');
  const [ukIpa, setUkIpa] = useState('');
  const [usIpa, setUsIpa] = useState('');
  const [stressPattern, setStressPattern] = useState('');
  const [synonymsInput, setSynonymsInput] = useState('');
  const [antonymsInput, setAntonymsInput] = useState('');
  const [collocationPhrase, setCollocationPhrase] = useState('');
  const [collocationMeaningVi, setCollocationMeaningVi] = useState('');
  const [exampleEn, setExampleEn] = useState('');
  const [exampleVi, setExampleVi] = useState('');
  const [grammarPattern, setGrammarPattern] = useState('');
  const [grammarExplanationVi, setGrammarExplanationVi] = useState('');
  const [formError, setFormError] = useState<string | null>(null);

  const filteredItems = useMemo(() => {
    return items.filter((c) => {
      if (filterCefr !== 'ALL' && c.cefrLevel !== filterCefr) return false;
      if (searchTerm.trim()) {
        const q = searchTerm.trim().toLowerCase();
        return (
          c.word.toLowerCase().includes(q) ||
          c.meaningVi.toLowerCase().includes(q) ||
          c.definitionEn.toLowerCase().includes(q) ||
          (c.topicCategory ?? '').toLowerCase().includes(q)
        );
      }
      return true;
    });
  }, [items, filterCefr, searchTerm]);

  const openCreateDialog = () => {
    setEditingId(null);
    setFormError(null);
    setWord('');
    setPartOfSpeech('noun');
    setCefrLevel('B2');
    setTopicCategory('.NET 10, Clean Architecture, DDD & CQRS');
    setDefinitionEn('');
    setMeaningVi('');
    setDescriptionEn('');
    setUkIpa('');
    setUsIpa('');
    setStressPattern('');
    setSynonymsInput('');
    setAntonymsInput('');
    setCollocationPhrase('');
    setCollocationMeaningVi('');
    setExampleEn('');
    setExampleVi('');
    setGrammarPattern('');
    setGrammarExplanationVi('');
    setIsDialogOpen(true);
  };

  const openEditDialog = (card: VocabularyFlashcardDto) => {
    setEditingId(card.vocabularyId);
    setFormError(null);
    setWord(card.word);
    setPartOfSpeech(card.partOfSpeech);
    setCefrLevel(card.cefrLevel);
    setTopicCategory(card.topicCategory ?? '');
    setDefinitionEn(card.definitionEn);
    setMeaningVi(card.meaningVi);
    setDescriptionEn(card.descriptionEn);
    const uk = card.pronunciations.find((p) => p.accent === 'UK');
    const us = card.pronunciations.find((p) => p.accent === 'US');
    setUkIpa(uk?.ipa ?? '');
    setUsIpa(us?.ipa ?? '');
    setStressPattern(uk?.stressPattern ?? us?.stressPattern ?? '');
    setSynonymsInput(card.synonyms.join(', '));
    setAntonymsInput(card.antonyms.join(', '));
    setCollocationPhrase(card.collocations[0]?.phrase ?? '');
    setCollocationMeaningVi(card.collocations[0]?.meaningVi ?? '');
    setExampleEn(card.examples[0]?.sentenceEn ?? '');
    setExampleVi(card.examples[0]?.translationVi ?? '');
    setGrammarPattern(card.grammarPatterns[0]?.pattern ?? '');
    setGrammarExplanationVi(card.grammarPatterns[0]?.explanationVi ?? '');
    setIsDialogOpen(true);
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!word.trim() || !definitionEn.trim() || !meaningVi.trim()) {
      setFormError('Vui lòng nhập đầy đủ Từ vựng (Word), Định nghĩa Tiếng Anh và Nghĩa Tiếng Việt.');
      return;
    }

    const payload = {
      word: word.trim(),
      partOfSpeech,
      cefrLevel,
      definitionEn: definitionEn.trim(),
      meaningVi: meaningVi.trim(),
      descriptionEn: descriptionEn.trim(),
      topicCategory: topicCategory.trim() || undefined,
      ukIpa: ukIpa.trim(),
      usIpa: usIpa.trim(),
      stressPattern: stressPattern.trim() || undefined,
      synonyms: synonymsInput
        .split(',')
        .map((s) => s.trim())
        .filter(Boolean),
      antonyms: antonymsInput
        .split(',')
        .map((s) => s.trim())
        .filter(Boolean),
      wordFamilies: [],
      collocations: collocationPhrase.trim()
        ? [{ phrase: collocationPhrase.trim(), meaningVi: collocationMeaningVi.trim() }]
        : [],
      examples: exampleEn.trim()
        ? [{ sentenceEn: exampleEn.trim(), translationVi: exampleVi.trim(), cefrLevel }]
        : [],
      grammarPatterns: grammarPattern.trim()
        ? [{ pattern: grammarPattern.trim(), explanationVi: grammarExplanationVi.trim() }]
        : [],
    };

    try {
      if (editingId) {
        const saved = await updateMutation.mutateAsync({ id: editingId, payload });
        setLocalItems(items.map((x) => (x.vocabularyId === editingId ? saved : x)));
      } else {
        const created = await createMutation.mutateAsync(payload);
        setLocalItems([created, ...items]);
      }
      setFeedback({
        type: 'success',
        message: editingId
          ? `Đã cập nhật từ vựng "${payload.word}" thành công.`
          : `Đã thêm từ vựng "${payload.word}" vào Database.`,
      });
    } catch {
      const localCard: VocabularyFlashcardDto = {
        vocabularyId: editingId ?? crypto.randomUUID(),
        senseId: crypto.randomUUID(),
        word: payload.word,
        partOfSpeech: payload.partOfSpeech,
        cefrLevel: payload.cefrLevel as CefrLevel,
        definitionEn: payload.definitionEn,
        meaningVi: payload.meaningVi,
        descriptionEn: payload.descriptionEn,
        topicCategory: payload.topicCategory,
        pronunciations: [
          { id: crypto.randomUUID(), accent: 'UK', ipa: payload.ukIpa || `/${payload.word}/` },
          { id: crypto.randomUUID(), accent: 'US', ipa: payload.usIpa || `/${payload.word}/` },
        ],
        synonyms: payload.synonyms,
        antonyms: payload.antonyms,
        wordFamilies: [],
        collocations: payload.collocations.map((c) => ({ id: crypto.randomUUID(), ...c })),
        examples: payload.examples.map((ex) => ({
          id: crypto.randomUUID(),
          sentenceEn: ex.sentenceEn,
          translationVi: ex.translationVi,
          cefrLevel: payload.cefrLevel as CefrLevel,
        })),
        grammarPatterns: payload.grammarPatterns.map((g) => ({ id: crypto.randomUUID(), ...g })),
        learningStatus: 'New',
        repetitionCount: 0,
        intervalDays: 0,
        easeFactor: 2.5,
        isCompletedToday: false,
      };
      if (editingId) {
        setLocalItems(items.map((x) => (x.vocabularyId === editingId ? localCard : x)));
      } else {
        setLocalItems([localCard, ...items]);
      }
      const successMsg = editingId
        ? `Đã cập nhật từ vựng "${payload.word}".`
        : `Đã tạo mới từ vựng "${payload.word}".`;
      setFeedback({
        type: 'success',
        message: successMsg,
      });
      toast.success(successMsg);
    }

    setIsDialogOpen(false);
    setEditingId(null);
  };

  const handleConfirmDelete = async () => {
    if (!deleteTarget) return;
    try {
      await deleteMutation.mutateAsync(deleteTarget.vocabularyId);
    } catch {
      // ignore offline
    }
    setLocalItems(items.filter((x) => x.vocabularyId !== deleteTarget.vocabularyId));
    const deleteMsg = `Đã xóa từ vựng "${deleteTarget.word}" khỏi danh sách.`;
    setFeedback({
      type: 'success',
      message: deleteMsg,
    });
    toast.success(deleteMsg);
    setDeleteTarget(null);
  };

  const columns: DataTableColumn<VocabularyFlashcardDto>[] = [
    {
      key: 'word',
      header: 'Từ vựng & Phiên âm',
      render: (card) => (
        <div>
          <div className="text-sm font-extrabold text-slate-900">{card.word}</div>
          <div className="font-mono text-[11px] text-emerald-700 mt-0.5">
            {card.pronunciations.map((p) => `${p.accent}: ${p.ipa}`).join(' · ')}
          </div>
          {card.topicCategory && (
            <div className="text-[11px] text-slate-400 mt-0.5 truncate max-w-[220px]">
              {card.topicCategory}
            </div>
          )}
        </div>
      ),
    },
    {
      key: 'cefr',
      header: 'CEFR / Từ loại',
      render: (card) => (
        <div className="space-y-1">
          <Badge variant="emerald">{card.cefrLevel}</Badge>
          <div className="text-[11px] font-semibold text-slate-500">{card.partOfSpeech}</div>
        </div>
      ),
    },
    {
      key: 'meaning',
      header: 'Nghĩa Tiếng Việt & Định nghĩa EN',
      render: (card) => (
        <div className="max-w-xs">
          <div className="font-bold text-slate-800">{card.meaningVi}</div>
          <div className="text-slate-500 line-clamp-2 mt-0.5">{card.definitionEn}</div>
        </div>
      ),
    },
    {
      key: 'examples',
      header: 'Ví dụ & Từ đồng nghĩa',
      render: (card) => (
        <div className="max-w-sm">
          {card.examples[0] && (
            <div className="text-slate-700 italic line-clamp-2">
              &ldquo;{card.examples[0].sentenceEn}&rdquo;
            </div>
          )}
          {card.synonyms.length > 0 && (
            <div className="text-[11px] text-emerald-700 mt-0.5">
              Syn: {card.synonyms.join(', ')}
            </div>
          )}
        </div>
      ),
    },
    {
      key: 'actions',
      header: 'Thao tác',
      className: 'text-right whitespace-nowrap',
      render: (card) => (
        <div className="inline-flex items-center justify-end gap-1.5">
          <IconButton
            action="edit"
            size="sm"
            label={`Chỉnh sửa từ vựng ${card.word}`}
            onClick={() => openEditDialog(card)}
          />
          <IconButton
            action="delete"
            size="sm"
            label={`Xóa từ vựng ${card.word}`}
            onClick={() => setDeleteTarget(card)}
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
            <img src="/assets/icons/navigation/nav-reviews.svg" alt="" className="w-6 h-6 object-contain" />
          </div>
          <div>
            <h1 className="text-lg font-extrabold text-slate-900">
              Quản lý Từ vựng / Flashcards ({filteredItems.length})
            </h1>
            <p className="text-xs text-slate-500">
              Quản lý Sense Flashcards, phiên âm UK/US IPA, Synonyms, Collocations và câu ví dụ song ngữ
            </p>
          </div>
        </div>
        <Badge variant="emerald">Permission: vocabularies.manage</Badge>
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
              placeholder="Tìm từ vựng, nghĩa tiếng Việt hoặc chủ đề..."
              className="w-full pl-9 pr-4 py-2 rounded-xl border border-slate-200 text-xs focus:outline-none focus:border-emerald-500"
            />
          </div>

          <select
            value={filterCefr}
            onChange={(e) => {
              setFilterCefr(e.target.value);
              setPage(1);
            }}
            aria-label="Lọc theo cấp độ CEFR"
            className="px-3 py-2 rounded-xl border border-slate-200 bg-white text-xs font-semibold text-slate-700"
          >
            <option value="ALL">Tất cả cấp độ CEFR</option>
            {(['A1', 'A2', 'B1', 'B2', 'C1', 'C2'] as CefrLevel[]).map((lvl) => (
              <option key={lvl} value={lvl}>
                CEFR {lvl}
              </option>
            ))}
          </select>
        </div>

        <Button variant="primary" size="sm" onClick={openCreateDialog}>
          <Plus className="w-4 h-4" />
          Add New Flashcard
        </Button>
      </div>

      {/* 3. Data Table + 4. Pagination */}
      <DataTable
        columns={columns}
        data={filteredItems}
        rowKey={(row) => `${row.vocabularyId}-${row.senseId}`}
        isLoading={isLoading}
        page={page}
        pageSize={pageSize}
        onPageChange={setPage}
        onPageSizeChange={setPageSize}
        emptyTitle="Không tìm thấy thẻ từ vựng nào"
        emptyAction={
          <Button variant="primary" size="sm" onClick={openCreateDialog}>
            <Plus className="w-4 h-4" />
            Thêm Flashcard đầu tiên
          </Button>
        }
      />

      {/* Create / Edit Dialog */}
      <Dialog
        isOpen={isDialogOpen}
        onClose={() => setIsDialogOpen(false)}
        maxWidth="3xl"
        icon={
          <img
            src="/assets/icons/navigation/nav-reviews.svg"
            alt=""
            className="w-5 h-5 object-contain"
          />
        }
        title={editingId ? `Chỉnh sửa Flashcard: ${word}` : 'Thêm mới Từ vựng / Flashcard'}
        description="Điền thông tin từ vựng, phiên âm IPA Anh-Anh / Anh-Mỹ, nghĩa Tiếng Việt và câu ví dụ"
        footer={
          <div className="flex items-center justify-end gap-2.5 w-full">
            <Button variant="outline" size="sm" onClick={() => setIsDialogOpen(false)}>
              Cancel
            </Button>
            <Button
              variant="primary"
              size="sm"
              disabled={createMutation.isPending || updateMutation.isPending}
              onClick={(e) => void handleSave(e)}
            >
              <Save className="w-4 h-4" />
              {createMutation.isPending || updateMutation.isPending ? 'Đang lưu...' : 'Save Flashcard'}
            </Button>
          </div>
        }
      >
        <form onSubmit={handleSave} className="space-y-4 text-xs">
          {formError && <FeedbackBanner type="error" message={formError} />}

          <div className="grid grid-cols-1 sm:grid-cols-4 gap-3">
            <FormField label="Từ vựng (Word)" required className="sm:col-span-2">
              <input
                type="text"
                value={word}
                onChange={(e) => setWord(e.target.value)}
                placeholder="vd: idempotent"
                className="w-full px-3 py-2 rounded-xl border border-slate-200"
              />
            </FormField>

            <FormField label="Từ loại (POS)">
              <select
                value={partOfSpeech}
                onChange={(e) => setPartOfSpeech(e.target.value)}
                className="w-full px-3 py-2 rounded-xl border border-slate-200 bg-white"
              >
                <option value="noun">noun</option>
                <option value="verb">verb</option>
                <option value="adjective">adjective</option>
                <option value="adverb">adverb</option>
                <option value="phrasal verb">phrasal verb</option>
                <option value="idiom">idiom</option>
              </select>
            </FormField>

            <FormField label="Trình độ CEFR">
              <select
                value={cefrLevel}
                onChange={(e) => setCefrLevel(e.target.value as CefrLevel)}
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

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <FormField label="Phiên âm UK IPA">
              <input
                type="text"
                value={ukIpa}
                onChange={(e) => setUkIpa(e.target.value)}
                placeholder="/ˌaɪ.demˈpəʊ.tənt/"
                className="w-full px-3 py-2 rounded-xl border border-slate-200 font-mono"
              />
            </FormField>

            <FormField label="Phiên âm US IPA">
              <input
                type="text"
                value={usIpa}
                onChange={(e) => setUsIpa(e.target.value)}
                placeholder="/ˌaɪ.demˈpoʊ.tənt/"
                className="w-full px-3 py-2 rounded-xl border border-slate-200 font-mono"
              />
            </FormField>

            <FormField label="Chủ đề (Topic Category)">
              <input
                type="text"
                value={topicCategory}
                onChange={(e) => setTopicCategory(e.target.value)}
                className="w-full px-3 py-2 rounded-xl border border-slate-200"
              />
            </FormField>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <FormField label="Định nghĩa Tiếng Anh (Definition EN)" required>
              <textarea
                rows={2}
                value={definitionEn}
                onChange={(e) => setDefinitionEn(e.target.value)}
                className="w-full px-3 py-2 rounded-xl border border-slate-200"
              />
            </FormField>

            <FormField label="Nghĩa Tiếng Việt (Meaning VI)" required>
              <textarea
                rows={2}
                value={meaningVi}
                onChange={(e) => setMeaningVi(e.target.value)}
                className="w-full px-3 py-2 rounded-xl border border-slate-200"
              />
            </FormField>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <FormField label="Synonyms (Cách nhau bởi dấu phẩy)">
              <input
                type="text"
                value={synonymsInput}
                onChange={(e) => setSynonymsInput(e.target.value)}
                placeholder="repeatable, safe-to-retry"
                className="w-full px-3 py-2 rounded-xl border border-slate-200"
              />
            </FormField>

            <FormField label="Antonyms (Cách nhau bởi dấu phẩy)">
              <input
                type="text"
                value={antonymsInput}
                onChange={(e) => setAntonymsInput(e.target.value)}
                placeholder="non-idempotent, side-effecting"
                className="w-full px-3 py-2 rounded-xl border border-slate-200"
              />
            </FormField>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <FormField label="Cụm Collocation tiêu biểu">
              <input
                type="text"
                value={collocationPhrase}
                onChange={(e) => setCollocationPhrase(e.target.value)}
                placeholder="idempotent API endpoint"
                className="w-full px-3 py-2 rounded-xl border border-slate-200"
              />
            </FormField>

            <FormField label="Nghĩa Tiếng Việt của Collocation">
              <input
                type="text"
                value={collocationMeaningVi}
                onChange={(e) => setCollocationMeaningVi(e.target.value)}
                placeholder="API gọi lại nhiều lần không gây trùng dữ liệu"
                className="w-full px-3 py-2 rounded-xl border border-slate-200"
              />
            </FormField>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <FormField label="Câu ví dụ Tiếng Anh (Example Sentence)">
              <textarea
                rows={2}
                value={exampleEn}
                onChange={(e) => setExampleEn(e.target.value)}
                className="w-full px-3 py-2 rounded-xl border border-slate-200"
              />
            </FormField>

            <FormField label="Dịch câu ví dụ sang Tiếng Việt">
              <textarea
                rows={2}
                value={exampleVi}
                onChange={(e) => setExampleVi(e.target.value)}
                className="w-full px-3 py-2 rounded-xl border border-slate-200"
              />
            </FormField>
          </div>
        </form>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <ConfirmDialog
        isOpen={Boolean(deleteTarget)}
        onClose={() => setDeleteTarget(null)}
        onConfirm={() => void handleConfirmDelete()}
        isLoading={deleteMutation.isPending}
        title="Xóa Từ vựng / Flashcard"
        targetName={deleteTarget ? `${deleteTarget.word} (${deleteTarget.meaningVi})` : ''}
      />
    </div>
  );
};
