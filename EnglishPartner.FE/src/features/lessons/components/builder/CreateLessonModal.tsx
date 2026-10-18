import React, { useState } from 'react';
import { BookOpen, Sparkles, Plus, Trash2, Clock } from 'lucide-react';
import { Dialog } from '@/shared/components/ui/Dialog';
import { Button } from '@/shared/components/ui/Button';
import { TopicCreatablePicker } from '@/shared/components/ui/TopicCreatablePicker';
import { useTopicsQuery } from '@/features/catalog/hooks/useCatalogQueries';
import type { CreateLessonDraftRequest, CefrLevel } from '../../types/lessons.types';

interface CreateLessonModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (data: CreateLessonDraftRequest) => Promise<void> | void;
  isSubmitting?: boolean;
}

const CEFR_LEVELS: CefrLevel[] = ['A1', 'A2', 'B1', 'B2', 'C1', 'C2'];

export const CreateLessonModal: React.FC<CreateLessonModalProps> = ({
  isOpen,
  onClose,
  onSubmit,
  isSubmitting = false,
}) => {
  const { data: topics = [] } = useTopicsQuery({ onlyActive: true });

  const [title, setTitle] = useState('');
  const [cefrLevel, setCefrLevel] = useState<CefrLevel>('B1');
  const [topic, setTopic] = useState('');
  const [estimatedMinutes, setEstimatedMinutes] = useState(20);
  const [content, setContent] = useState('');

  // Lists
  const [objectives, setObjectives] = useState<string[]>(['']);
  const [vocabInput, setVocabInput] = useState('');
  const [grammarInput, setGrammarInput] = useState('');

  const [errors, setErrors] = useState<Record<string, string>>({});

  const handleAddObjective = () => {
    setObjectives([...objectives, '']);
  };

  const handleRemoveObjective = (index: number) => {
    setObjectives(objectives.filter((_, i) => i !== index));
  };

  const handleObjectiveChange = (index: number, val: string) => {
    const updated = [...objectives];
    updated[index] = val;
    setObjectives(updated);
  };

  const validate = (): boolean => {
    const errs: Record<string, string> = {};
    if (!title.trim()) {
      errs.title = 'Vui lòng nhập tiêu đề bài học.';
    }
    if (!topic.trim()) {
      errs.topic = 'Vui lòng nhập chủ đề bài học.';
    }
    if (estimatedMinutes < 1 || estimatedMinutes > 300) {
      errs.estimatedMinutes = 'Thời lượng phải từ 1 đến 300 phút.';
    }
    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;

    const cleanObjectives = objectives
      .map((o) => o.trim())
      .filter((o) => o.length > 0);

    const cleanVocab = vocabInput
      .split(',')
      .map((v) => v.trim())
      .filter((v) => v.length > 0);

    const cleanGrammar = grammarInput
      .split(',')
      .map((g) => g.trim())
      .filter((g) => g.length > 0);

    const payload: CreateLessonDraftRequest = {
      title: title.trim(),
      cefrLevel,
      topic: topic.trim(),
      estimatedMinutes,
      content: content.trim() || undefined,
      learningObjectives: cleanObjectives.length > 0 ? cleanObjectives : undefined,
      targetVocabulary: cleanVocab.length > 0 ? cleanVocab : undefined,
      targetGrammar: cleanGrammar.length > 0 ? cleanGrammar : undefined,
    };

    await onSubmit(payload);
  };

  const resetForm = () => {
    setTitle('');
    setCefrLevel('B1');
    setTopic('');
    setEstimatedMinutes(20);
    setContent('');
    setObjectives(['']);
    setVocabInput('');
    setGrammarInput('');
    setErrors({});
  };

  return (
    <Dialog
      isOpen={isOpen}
      onClose={() => {
        resetForm();
        onClose();
      }}
      maxWidth="2xl"
      title={
        <div className="flex items-center gap-2 text-slate-900 font-bold">
          <BookOpen className="w-5 h-5 text-emerald-600" />
          <span>Tạo bài học mới (Bản thảo)</span>
        </div>
      }
      description="Khởi tạo thông tin cơ bản cho giáo án trước khi thêm và cấu hình các bài tập tương tác."
      footer={
        <div className="flex items-center justify-end gap-2.5 w-full">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => {
              resetForm();
              onClose();
            }}
            disabled={isSubmitting}
          >
            Hủy bỏ
          </Button>
          <Button
            type="submit"
            form="create-lesson-form"
            variant="primary"
            size="sm"
            disabled={isSubmitting}
            className="gap-1.5"
          >
            <Sparkles className="w-4 h-4" />
            {isSubmitting ? 'Đang tạo...' : 'Tạo bài học & Mở trình biên soạn'}
          </Button>
        </div>
      }
    >
      <form id="create-lesson-form" onSubmit={handleSubmit} className="space-y-4 py-1">
        {/* Title */}
        <div>
          <label className="block text-xs font-bold text-slate-700 mb-1">
            Tiêu đề bài học <span className="text-rose-500">*</span>
          </label>
          <input
            type="text"
            placeholder="Ví dụ: Ordering Food at a Restaurant, Business Negotiations..."
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            className="w-full text-xs rounded-xl border border-slate-200 px-3 py-2.5 bg-slate-50 focus:bg-white focus:outline-none focus:ring-2 focus:ring-emerald-500"
          />
          {errors.title && <p className="text-xs text-rose-500 mt-1">{errors.title}</p>}
        </div>

        {/* Level & Duration */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <label className="block text-xs font-bold text-slate-700 mb-1">
              Khung năng lực CEFR <span className="text-rose-500">*</span>
            </label>
            <select
              value={cefrLevel}
              onChange={(e) => setCefrLevel(e.target.value as CefrLevel)}
              className="w-full text-xs rounded-xl border border-slate-200 px-3 py-2 bg-slate-50 focus:bg-white focus:outline-none focus:ring-2 focus:ring-emerald-500"
            >
              {CEFR_LEVELS.map((lvl) => (
                <option key={lvl} value={lvl}>
                  Level {lvl}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 mb-1 flex items-center gap-1">
              <Clock className="w-3.5 h-3.5 text-slate-400" />
              Thời lượng dự kiến (phút) <span className="text-rose-500">*</span>
            </label>
            <input
              type="number"
              min={1}
              max={300}
              value={estimatedMinutes}
              onChange={(e) => setEstimatedMinutes(Number(e.target.value) || 20)}
              className="w-full text-xs rounded-xl border border-slate-200 px-3 py-2 bg-slate-50 focus:bg-white focus:outline-none focus:ring-2 focus:ring-emerald-500"
            />
            {errors.estimatedMinutes && (
              <p className="text-xs text-rose-500 mt-1">{errors.estimatedMinutes}</p>
            )}
          </div>
        </div>

        {/* Topic Input with API Suggestions */}
        <div className="space-y-1">
          <TopicCreatablePicker
            label="Chủ đề bài học (Topic) *"
            value={topic}
            onChange={(val) => {
              setTopic(val);
              if (errors.topic) {
                setErrors((prev) => ({ ...prev, topic: '' }));
              }
            }}
            topics={topics}
            placeholder="Chọn chủ đề gợi ý hoặc gõ chủ đề mới..."
            helperText="Gợi ý tự động từ danh mục chủ đề (API get topics) hoặc gõ tự do chủ đề mới."
          />
          {errors.topic && <p className="text-xs text-rose-500 mt-1">{errors.topic}</p>}
        </div>

        {/* Content / Reading Passage */}
        <div>
          <label className="block text-xs font-bold text-slate-700 mb-1">
            Nội dung / Ngữ cảnh bài học (Tùy chọn)
          </label>
          <textarea
            rows={3}
            placeholder="Mô tả nội dung bài học, bối cảnh giao tiếp hoặc đoạn văn bản tham chiếu..."
            value={content}
            onChange={(e) => setContent(e.target.value)}
            className="w-full text-xs rounded-xl border border-slate-200 px-3 py-2 bg-slate-50 focus:bg-white focus:outline-none focus:ring-2 focus:ring-emerald-500"
          />
        </div>

        {/* Learning Objectives */}
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <label className="block text-xs font-bold text-slate-700">
              Mục tiêu học tập (Learning Objectives)
            </label>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={handleAddObjective}
              className="gap-1 text-[11px] py-1 px-2 text-emerald-700 border-emerald-200 hover:bg-emerald-50"
            >
              <Plus className="w-3 h-3" /> Thêm mục tiêu
            </Button>
          </div>

          <div className="space-y-2">
            {objectives.map((obj, idx) => (
              <div key={idx} className="flex items-center gap-2">
                <input
                  type="text"
                  placeholder={`Mục tiêu #${idx + 1}: e.g. Tự tin gọi món và hỏi giá`}
                  value={obj}
                  onChange={(e) => handleObjectiveChange(idx, e.target.value)}
                  className="flex-1 text-xs rounded-xl border border-slate-200 px-3 py-1.5 bg-slate-50 focus:bg-white focus:outline-none focus:ring-2 focus:ring-emerald-500"
                />
                {objectives.length > 1 && (
                  <button
                    type="button"
                    onClick={() => handleRemoveObjective(idx)}
                    className="p-1.5 text-slate-400 hover:text-rose-500 rounded-lg hover:bg-rose-50"
                  >
                    <Trash2 className="w-3.5 h-3.5" />
                  </button>
                )}
              </div>
            ))}
          </div>
        </div>

        {/* Target Vocabulary & Grammar */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-1">
          <div>
            <label className="block text-xs font-bold text-slate-700 mb-1">
              Từ vựng trọng tâm (phân tách bởi dấu phẩy)
            </label>
            <input
              type="text"
              placeholder="e.g. appetizer, beverage, bill, order"
              value={vocabInput}
              onChange={(e) => setVocabInput(e.target.value)}
              className="w-full text-xs rounded-xl border border-slate-200 px-3 py-2 bg-slate-50 focus:bg-white focus:outline-none focus:ring-2 focus:ring-emerald-500"
            />
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 mb-1">
              Ngữ pháp trọng tâm (phân tách bởi dấu phẩy)
            </label>
            <input
              type="text"
              placeholder="e.g. Would like, Can I have, Modal verbs"
              value={grammarInput}
              onChange={(e) => setGrammarInput(e.target.value)}
              className="w-full text-xs rounded-xl border border-slate-200 px-3 py-2 bg-slate-50 focus:bg-white focus:outline-none focus:ring-2 focus:ring-emerald-500"
            />
          </div>
        </div>
      </form>
    </Dialog>
  );
};
