import React, { useState } from 'react';
import { X, Plus, Trash2, Check, AlertCircle } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import {
  ExerciseType,
  EXERCISE_TYPE_CATALOG,
  type CreateLessonRubricRequest,
  type CreateRubricCriterionRequest,
} from '../../types/lessons.types';

interface CreateLessonRubricModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (data: CreateLessonRubricRequest) => void;
  isSubmitting?: boolean;
}

export const CreateLessonRubricModal: React.FC<CreateLessonRubricModalProps> = ({
  isOpen,
  onClose,
  onSubmit,
  isSubmitting = false,
}) => {
  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [exerciseType, setExerciseType] = useState<string>(ExerciseType.WritingPractice);
  const [cefrLevel, setCefrLevel] = useState<string>('All');

  const [criteria, setCriteria] = useState<CreateRubricCriterionRequest[]>([
    {
      code: 'TASK_ACHIEVEMENT',
      name: 'Task Achievement',
      description: 'Mức độ đáp ứng đầy đủ yêu cầu đề bài.',
      weight: 50,
      displayOrder: 1,
      descriptorLevel0: 'Không đáp ứng yêu cầu tối thiểu.',
      descriptorLevel1: 'Đáp ứng một phần nhỏ yêu cầu.',
      descriptorLevel2: 'Đáp ứng mức cơ bản.',
      descriptorLevel3: 'Đáp ứng tốt tất cả yêu cầu.',
      descriptorLevel4: 'Hoàn thành xuất sắc, vượt mong đợi.',
    },
    {
      code: 'LANGUAGE_ACCURACY',
      name: 'Language Accuracy',
      description: 'Độ chính xác về ngữ pháp và từ vựng.',
      weight: 50,
      displayOrder: 2,
      descriptorLevel0: 'Lỗi ngữ pháp trầm trọng.',
      descriptorLevel1: 'Nhiều lỗi ngữ pháp gây khó hiểu.',
      descriptorLevel2: 'Đôi chỗ còn lỗi nhưng diễn đạt được ý.',
      descriptorLevel3: 'Ngữ pháp và từ vựng phong phú, ít lỗi.',
      descriptorLevel4: 'Độ chính xác hoàn hảo, tự nhiên.',
    },
  ]);

  if (!isOpen) return null;

  const totalWeight = criteria.reduce((sum, c) => sum + (Number(c.weight) || 0), 0);
  const isWeightValid = totalWeight === 100;

  const handleAddCriterion = () => {
    setCriteria((prev) => [
      ...prev,
      {
        code: `CRITERION_${prev.length + 1}`,
        name: `Tiêu chí ${prev.length + 1}`,
        description: '',
        weight: 0,
        displayOrder: prev.length + 1,
        descriptorLevel0: 'Chưa đạt yêu cầu.',
        descriptorLevel1: 'Dưới trung bình.',
        descriptorLevel2: 'Trung bình.',
        descriptorLevel3: 'Tốt.',
        descriptorLevel4: 'Xuất sắc.',
      },
    ]);
  };

  const handleRemoveCriterion = (index: number) => {
    setCriteria((prev) => prev.filter((_, idx) => idx !== index));
  };

  const handleCriterionChange = (index: number, field: keyof CreateRubricCriterionRequest, val: any) => {
    setCriteria((prev) =>
      prev.map((item, idx) => (idx === index ? { ...item, [field]: val } : item))
    );
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!isWeightValid) return;

    onSubmit({
      code: code.trim(),
      name: name.trim(),
      description: description.trim(),
      exerciseType: exerciseType || null,
      cefrLevel,
      criteria,
    });
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 p-4 overflow-y-auto">
      <div className="bg-white rounded-3xl border border-slate-200 shadow-2xl w-full max-w-3xl max-h-[90vh] flex flex-col my-8">
        {/* Header */}
        <div className="flex items-center justify-between p-5 border-b border-slate-100">
          <div>
            <h3 className="text-base font-bold text-slate-900">Tạo mới Rubric chấm điểm bài tập</h3>
            <p className="text-xs text-slate-500">
              Thiết lập các tiêu chuẩn CEFR và ma trận thang điểm 0-4 cho AI grader.
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="p-1.5 text-slate-400 hover:text-slate-700 rounded-xl hover:bg-slate-100 cursor-pointer"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Form Body */}
        <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto p-5 space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-1">
              <label className="text-xs font-bold text-slate-700">Mã Rubric (Code duy nhất)</label>
              <input
                type="text"
                required
                placeholder="vd: LESSON_WRITING_B1"
                value={code}
                onChange={(e) => setCode(e.target.value.toUpperCase())}
                className="w-full text-xs font-mono font-bold rounded-xl border border-slate-200 p-2.5 bg-slate-50 focus:bg-white focus:ring-2 focus:ring-emerald-500"
              />
            </div>

            <div className="space-y-1">
              <label className="text-xs font-bold text-slate-700">Tên Rubric</label>
              <input
                type="text"
                required
                placeholder="vd: Writing Rubric Level B1"
                value={name}
                onChange={(e) => setName(e.target.value)}
                className="w-full text-xs rounded-xl border border-slate-200 p-2.5 bg-slate-50 focus:bg-white focus:ring-2 focus:ring-emerald-500"
              />
            </div>

            <div className="space-y-1">
              <label className="text-xs font-bold text-slate-700">Áp dụng cho loại bài</label>
              <select
                value={exerciseType}
                onChange={(e) => setExerciseType(e.target.value)}
                className="w-full text-xs rounded-xl border border-slate-200 p-2.5 bg-slate-50 focus:bg-white focus:ring-2 focus:ring-emerald-500"
              >
                {Object.values(ExerciseType).map((t) => (
                  <option key={t} value={t}>
                    {EXERCISE_TYPE_CATALOG[t].labelVi}
                  </option>
                ))}
              </select>
            </div>

            <div className="space-y-1">
              <label className="text-xs font-bold text-slate-700">Cấp độ CEFR</label>
              <select
                value={cefrLevel}
                onChange={(e) => setCefrLevel(e.target.value)}
                className="w-full text-xs rounded-xl border border-slate-200 p-2.5 bg-slate-50 focus:bg-white focus:ring-2 focus:ring-emerald-500"
              >
                <option value="All">Tất cả (All Levels)</option>
                <option value="A1">A1</option>
                <option value="A2">A2</option>
                <option value="B1">B1</option>
                <option value="B2">B2</option>
                <option value="C1">C1</option>
                <option value="C2">C2</option>
              </select>
            </div>
          </div>

          <div className="space-y-1">
            <label className="text-xs font-bold text-slate-700">Mô tả tổng quát</label>
            <textarea
              rows={2}
              placeholder="Hướng dẫn chung về mục tiêu đánh giá..."
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="w-full text-xs rounded-xl border border-slate-200 p-2.5 bg-slate-50 focus:bg-white focus:ring-2 focus:ring-emerald-500"
            />
          </div>

          {/* Criteria Management */}
          <div className="space-y-3 pt-2 border-t border-slate-100">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <span className="text-xs font-bold text-slate-800 uppercase tracking-wider">
                  Các tiêu chí đánh giá ({criteria.length})
                </span>
                <Badge variant={isWeightValid ? 'emerald' : 'rose'}>
                  Tổng trọng số: {totalWeight}% / 100%
                </Badge>
              </div>

              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={handleAddCriterion}
                className="gap-1 text-xs text-slate-700 border-slate-200 hover:border-emerald-500 hover:text-emerald-700 hover:bg-emerald-50"
              >
                <Plus className="w-3.5 h-3.5" /> Thêm tiêu chí
              </Button>
            </div>

            {!isWeightValid && (
              <p className="text-xs text-rose-600 font-semibold flex items-center gap-1">
                <AlertCircle className="w-3.5 h-3.5" />
                Tổng trọng số của các tiêu chí phải đạt chính xác 100% để có thể lưu.
              </p>
            )}

            <div className="space-y-3">
              {criteria.map((c, idx) => (
                <div
                  key={idx}
                  className="p-4 rounded-2xl border border-slate-200 bg-slate-50/50 space-y-3 shadow-2xs"
                >
                  <div className="flex items-center justify-between gap-2">
                    <span className="text-xs font-bold text-slate-800">
                      Tiêu chí #{idx + 1}
                    </span>
                    {criteria.length > 1 && (
                      <button
                        type="button"
                        onClick={() => handleRemoveCriterion(idx)}
                        className="text-slate-400 hover:text-rose-600 cursor-pointer"
                        title="Xóa tiêu chí"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    )}
                  </div>

                  <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                    <input
                      type="text"
                      placeholder="Mã tiêu chí (CODE)"
                      value={c.code}
                      onChange={(e) => handleCriterionChange(idx, 'code', e.target.value.toUpperCase())}
                      className="text-xs font-mono font-bold rounded-xl border border-slate-200 p-2 bg-white"
                    />
                    <input
                      type="text"
                      placeholder="Tên tiêu chí"
                      value={c.name}
                      onChange={(e) => handleCriterionChange(idx, 'name', e.target.value)}
                      className="text-xs rounded-xl border border-slate-200 p-2 bg-white"
                    />
                    <div className="flex items-center gap-1">
                      <input
                        type="number"
                        min={1}
                        max={100}
                        placeholder="Trọng số %"
                        value={c.weight}
                        onChange={(e) => handleCriterionChange(idx, 'weight', Number(e.target.value))}
                        className="text-xs rounded-xl border border-slate-200 p-2 bg-white w-full"
                      />
                      <span className="text-xs font-bold text-slate-600">%</span>
                    </div>
                  </div>

                  <input
                    type="text"
                    placeholder="Mô tả tiêu chí..."
                    value={c.description}
                    onChange={(e) => handleCriterionChange(idx, 'description', e.target.value)}
                    className="w-full text-xs rounded-xl border border-slate-200 p-2 bg-white"
                  />
                </div>
              ))}
            </div>
          </div>

          {/* Footer */}
          <div className="flex items-center justify-end gap-2 pt-3 border-t border-slate-100">
            <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>
              Hủy
            </Button>
            <Button
              type="submit"
              variant="primary"
              disabled={!isWeightValid || !code.trim() || !name.trim() || isSubmitting}
              className="gap-1.5"
            >
              <Check className="w-4 h-4" />
              {isSubmitting ? 'Đang tạo Rubric...' : 'Lưu Rubric'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
