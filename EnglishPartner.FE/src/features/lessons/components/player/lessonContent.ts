export type QuestionKind = 'SingleChoice' | 'MultipleChoice' | 'TrueFalse' | 'FillBlank' | 'ShortAnswer';
export interface ComprehensionQuestion {
  id: string;
  questionKind: QuestionKind;
  questionText: string;
  options?: Array<{ id: string; text: string }>;
  maxPoints?: number;
  order?: number;
}

export function normalizeQuestions(questions: unknown): ComprehensionQuestion[] {
  if (!Array.isArray(questions)) return [];
  return questions.map((q) => ({
    id: String(q.id),
    questionKind: q.questionKind ?? q.kind ?? 'ShortAnswer',
    questionText: q.questionText ?? q.prompt ?? q.question ?? '',
    options: q.options,
    maxPoints: q.maxPoints,
    order: q.order,
  }));
}

export function normalizeBlankText(content: { textWithBlanks?: string; text?: string }): string {
  return (content.textWithBlanks ?? content.text ?? '').replace(/\{\{\s*([\w-]+)\s*\}\}/g, '[$1]');
}

export function buildComprehensionSubmission(questions: ComprehensionQuestion[], answers: Record<string, unknown>) {
  return {
    subQuestionAnswers: questions.map((q) => {
      const answer = answers[q.id];
      if (q.questionKind === 'SingleChoice' || q.questionKind === 'MultipleChoice') {
        return { questionId: q.id, selectedOptionIds: Array.isArray(answer) ? answer : [answer] };
      }
      if (q.questionKind === 'TrueFalse') return { questionId: q.id, answer: answer as boolean };
      return { questionId: q.id, textAnswer: String(answer ?? '').trim() };
    }),
  };
}
