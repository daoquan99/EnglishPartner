import React, { useState } from 'react';
import { Button } from '@/shared/components/ui/Button';
import { buildComprehensionSubmission, normalizeQuestions } from './lessonContent';

interface Props {
  questions: unknown;
  onSubmit: (payload: Record<string, unknown>) => void;
  isSubmitting?: boolean;
  isSubmitted?: boolean;
}

export function ComprehensionQuestions({ questions, onSubmit, isSubmitting, isSubmitted }: Props) {
  const items = normalizeQuestions(questions);
  const [answers, setAnswers] = useState<Record<string, unknown>>({});
  const disabled = Boolean(isSubmitted || isSubmitting);
  const allAnswered = items.length > 0 && items.every((q) => {
    const value = answers[q.id];
    return Array.isArray(value) ? value.length > 0 : typeof value === 'boolean' || (typeof value === 'string' && value.trim().length > 0);
  });
  const setAnswer = (id: string, value: unknown) => setAnswers((prev) => ({ ...prev, [id]: value }));

  return (
    <div className="space-y-4">
      {items.length === 0 && <p role="alert">Bài tập chưa có câu hỏi. Vui lòng kiểm tra lại nội dung bài học.</p>}
      {items.map((q, index) => (
        <fieldset key={q.id} disabled={disabled} className="rounded-2xl border border-slate-200 bg-white p-5 space-y-3 min-w-0">
          <legend className="px-1 text-base font-bold text-slate-900">Câu {index + 1}: {q.questionText || 'Câu hỏi chưa có nội dung'}</legend>
          {q.maxPoints != null && <p className="text-xs text-slate-500">{q.maxPoints} điểm</p>}
          {(q.questionKind === 'SingleChoice' || q.questionKind === 'MultipleChoice') && q.options?.map((option) => {
            const multiple = q.questionKind === 'MultipleChoice';
            const value = answers[q.id];
            const checked = multiple ? Array.isArray(value) && value.includes(option.id) : value === option.id;
            return <label key={option.id} className="flex items-start gap-3 rounded-xl border border-slate-200 p-3 cursor-pointer has-checked:bg-emerald-50">
              <input type={multiple ? 'checkbox' : 'radio'} name={`question-${q.id}`} checked={checked}
                onChange={() => setAnswer(q.id, multiple
                  ? (checked ? (value as string[]).filter((id) => id !== option.id) : [...(Array.isArray(value) ? value : []), option.id])
                  : option.id)} className="mt-1 accent-emerald-600" />
              <span>{option.text}</span>
            </label>;
          })}
          {q.questionKind === 'TrueFalse' && <div className="flex gap-4">{[true, false].map((value) => (
            <label key={String(value)} className="flex items-center gap-2 cursor-pointer">
              <input type="radio" name={`question-${q.id}`} checked={answers[q.id] === value} onChange={() => setAnswer(q.id, value)} />
              {value ? 'True (Đúng)' : 'False (Sai)'}
            </label>
          ))}</div>}
          {(q.questionKind === 'ShortAnswer' || q.questionKind === 'FillBlank') && <input
            aria-label={`Câu trả lời ${index + 1}: ${q.questionText}`}
            type="text" value={String(answers[q.id] ?? '')} onChange={(e) => setAnswer(q.id, e.target.value)}
            placeholder="Nhập câu trả lời của bạn..."
            className="w-full select-text text-base rounded-xl border border-slate-300 bg-white p-3 focus:outline-none focus:ring-2 focus:ring-emerald-500" />}
        </fieldset>
      ))}
      {!isSubmitted && <Button type="button" className="w-full" disabled={!allAnswered || isSubmitting}
        onClick={() => onSubmit(buildComprehensionSubmission(items, answers))}>
        {isSubmitting ? 'Đang nộp câu trả lời...' : 'Nộp các câu trả lời'}
      </Button>}
    </div>
  );
}
