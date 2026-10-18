import React, { useState } from 'react';
import { Save, AlertCircle } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { TopicCreatablePicker } from '@/shared/components/ui/TopicCreatablePicker';
import { useTopicsQuery } from '@/features/catalog/hooks/useCatalogQueries';
import type { CefrLevel } from '../../types/lessons.types';

interface LessonMetadataFormProps {
  initialValues: {
    title: string;
    cefrLevel: string;
    topic: string;
    content?: string;
    estimatedMinutes: number;
    learningObjectives?: string[];
    targetVocabulary?: string[];
    targetGrammar?: string[];
    rowVersion?: string | null;
  };
  onSave: (values: {
    title: string;
    cefrLevel: string;
    topic: string;
    content?: string;
    estimatedMinutes: number;
    learningObjectives?: string[];
    targetVocabulary?: string[];
    targetGrammar?: string[];
    rowVersion?: string | null;
  }) => void;
  isSaving?: boolean;
}

const CEFR_LEVELS: CefrLevel[] = ['A1', 'A2', 'B1', 'B2', 'C1', 'C2'];

export const LessonMetadataForm: React.FC<LessonMetadataFormProps> = ({
  initialValues,
  onSave,
  isSaving = false,
}) => {
  const { data: topics = [] } = useTopicsQuery({ onlyActive: true });

  const [title, setTitle] = useState(initialValues.title);
  const [cefrLevel, setCefrLevel] = useState(initialValues.cefrLevel || 'B1');
  const [topic, setTopic] = useState(initialValues.topic);
  const [estimatedMinutes, setEstimatedMinutes] = useState(initialValues.estimatedMinutes || 20);
  const [content, setContent] = useState(initialValues.content || '');

  const [objectivesText, setObjectivesText] = useState(
    initialValues.learningObjectives?.join('\n') || ''
  );
  const [vocabText, setVocabText] = useState(
    initialValues.targetVocabulary?.join(', ') || ''
  );
  const [grammarText, setGrammarText] = useState(
    initialValues.targetGrammar?.join(', ') || ''
  );

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    const objectives = objectivesText
      .split('\n')
      .map((s) => s.trim())
      .filter(Boolean);

    const vocab = vocabText
      .split(',')
      .map((s) => s.trim())
      .filter(Boolean);

    const grammar = grammarText
      .split(',')
      .map((s) => s.trim())
      .filter(Boolean);

    onSave({
      title: title.trim(),
      cefrLevel,
      topic: topic.trim(),
      estimatedMinutes: Number(estimatedMinutes) || 15,
      content: content.trim(),
      learningObjectives: objectives,
      targetVocabulary: vocab,
      targetGrammar: grammar,
      rowVersion: initialValues.rowVersion,
    });
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-5 bg-white p-6 rounded-3xl border border-slate-200/90 shadow-2xs">
      <div className="flex items-center justify-between border-b border-slate-100 pb-3">
        <h3 className="text-sm font-bold text-slate-800 uppercase tracking-wider">
          Thông tin tổng quan bài học
        </h3>
        <Button type="submit" variant="primary" size="sm" disabled={isSaving || !title.trim()} className="gap-1.5">
          <Save className="w-4 h-4" />
          {isSaving ? 'Đang lưu...' : 'Lưu thông tin'}
        </Button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {/* Title */}
        <div className="md:col-span-2 space-y-1">
          <label className="text-xs font-bold text-slate-700">
            Tiêu đề bài học <span className="text-rose-500">*</span>
          </label>
          <input
            type="text"
            required
            placeholder="vd: Dining Out at a Restaurant"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            className="w-full text-sm rounded-xl border border-slate-200 p-2.5 bg-slate-50/50 focus:bg-white focus:ring-2 focus:ring-emerald-500"
          />
        </div>

        {/* CEFR Level */}
        <div className="space-y-1">
          <label className="text-xs font-bold text-slate-700">Trình độ CEFR</label>
          <select
            value={cefrLevel}
            onChange={(e) => setCefrLevel(e.target.value)}
            className="w-full text-sm rounded-xl border border-slate-200 p-2.5 bg-slate-50/50 focus:bg-white focus:ring-2 focus:ring-emerald-500"
          >
            {CEFR_LEVELS.map((lvl) => (
              <option key={lvl} value={lvl}>
                Level {lvl}
              </option>
            ))}
          </select>
        </div>

        {/* Estimated Minutes */}
        <div className="space-y-1">
          <label className="text-xs font-bold text-slate-700">Thời lượng ước tính (phút)</label>
          <input
            type="number"
            min={1}
            max={180}
            value={estimatedMinutes}
            onChange={(e) => setEstimatedMinutes(Number(e.target.value))}
            className="w-full text-sm rounded-xl border border-slate-200 p-2.5 bg-slate-50/50 focus:bg-white focus:ring-2 focus:ring-emerald-500"
          />
        </div>

        {/* Topic Input with API Suggestions */}
        <div className="md:col-span-2 space-y-1">
          <TopicCreatablePicker
            label="Chủ đề bài học (Topic) *"
            value={topic}
            onChange={setTopic}
            topics={topics}
            placeholder="vd: Restaurant, Travel, Work..."
            helperText="Gợi ý tự động từ danh mục chủ đề (API get topics) hoặc gõ tự do chủ đề mới."
          />
        </div>
      </div>

      {/* Content / Intro */}
      <div className="space-y-1">
        <label className="text-xs font-bold text-slate-700">Tóm tắt / Hướng dẫn mở đầu</label>
        <textarea
          rows={2}
          placeholder="Mô tả bối cảnh và hướng dẫn người học trước khi vào bài..."
          value={content}
          onChange={(e) => setContent(e.target.value)}
          className="w-full text-xs sm:text-sm rounded-xl border border-slate-200 p-2.5 bg-slate-50/50 focus:bg-white focus:ring-2 focus:ring-emerald-500"
        />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {/* Objectives */}
        <div className="space-y-1">
          <label className="text-xs font-bold text-slate-700">Mục tiêu bài học (Mỗi dòng 1 mục tiêu)</label>
          <textarea
            rows={3}
            placeholder="Đặt món tự tin&#10;Hỏi giá và hoá đơn"
            value={objectivesText}
            onChange={(e) => setObjectivesText(e.target.value)}
            className="w-full text-xs rounded-xl border border-slate-200 p-2.5 bg-slate-50/50 focus:bg-white focus:ring-2 focus:ring-emerald-500"
          />
        </div>

        {/* Target Vocab */}
        <div className="space-y-1">
          <label className="text-xs font-bold text-slate-700">Từ vựng trọng tâm (Phân cách bằng dấu phẩy)</label>
          <textarea
            rows={3}
            placeholder="appetizer, entree, dessert, bill"
            value={vocabText}
            onChange={(e) => setVocabText(e.target.value)}
            className="w-full text-xs rounded-xl border border-slate-200 p-2.5 bg-slate-50/50 focus:bg-white focus:ring-2 focus:ring-emerald-500"
          />
        </div>

        {/* Target Grammar */}
        <div className="space-y-1">
          <label className="text-xs font-bold text-slate-700">Ngữ pháp trọng tâm (Phân cách bằng dấu phẩy)</label>
          <textarea
            rows={3}
            placeholder="Would like + noun, Can I have...?"
            value={grammarText}
            onChange={(e) => setGrammarText(e.target.value)}
            className="w-full text-xs rounded-xl border border-slate-200 p-2.5 bg-slate-50/50 focus:bg-white focus:ring-2 focus:ring-emerald-500"
          />
        </div>
      </div>
    </form>
  );
};
