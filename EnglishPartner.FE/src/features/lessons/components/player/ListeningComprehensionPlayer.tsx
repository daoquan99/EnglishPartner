import React from 'react';
import { ComprehensionQuestions } from './ComprehensionQuestions';
import { LessonAudioButton } from './LessonAudioButton';

interface Props {
  content: { subQuestions?: unknown[] };
  onSubmit: (payload: Record<string, unknown>) => void;
  isSubmitting?: boolean;
  isSubmitted?: boolean;
  initialAnswers?: Record<string, unknown>;
}

export function ListeningComprehensionPlayer({ content, ...props }: Props) {
  return <div className="max-w-3xl mx-auto space-y-6">
    <section className="rounded-2xl border border-emerald-200 bg-emerald-50 p-5 space-y-3">
      <h4 className="font-bold text-emerald-900">Bài nghe hiểu</h4>
      <LessonAudioButton label="Nghe bài hội thoại" />
    </section>
    <ComprehensionQuestions questions={content.subQuestions} {...props} />
  </div>;
}
