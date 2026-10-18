import React from 'react';
import { ComprehensionQuestions } from './ComprehensionQuestions';

interface Props {
  content: { passage?: string; subQuestions?: unknown[] };
  onSubmit: (payload: Record<string, unknown>) => void;
  isSubmitting?: boolean;
  isSubmitted?: boolean;
}

export function ReadingComprehensionPlayer({ content, ...props }: Props) {
  return <div className="space-y-6 max-w-3xl mx-auto select-text">
    <section className="rounded-2xl border border-slate-200 bg-slate-50 p-5 space-y-3">
      <h4 className="font-bold text-emerald-900">Bài đọc hiểu</h4>
      <p className="whitespace-pre-line text-base leading-relaxed text-slate-900">{content.passage || 'Bài tập chưa có đoạn đọc.'}</p>
    </section>
    <ComprehensionQuestions questions={content.subQuestions} {...props} />
  </div>;
}
