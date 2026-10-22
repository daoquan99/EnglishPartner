import React, { useState } from 'react';
import { Check } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { normalizeBlankText } from './lessonContent';

interface BlankItem {
  id: string;
  hint?: string;
  options?: string[];
}

interface FillInTheBlanksPlayerProps {
  content: {
    textWithBlanks?: string;
    text?: string;
    blanks?: BlankItem[];
  };
  onSubmit: (payload: { answers: Record<string, string> }) => void;
  isSubmitting?: boolean;
  isSubmitted?: boolean;
  initialAnswers?: Record<string, string>;
}

export const FillInTheBlanksPlayer: React.FC<FillInTheBlanksPlayerProps> = ({
  content,
  onSubmit,
  isSubmitting = false,
  isSubmitted = false,
  initialAnswers = {},
}) => {
  const blanks = content?.blanks ?? [];
  const text = normalizeBlankText(content ?? {});
  const [answers, setAnswers] = useState<Record<string, string>>(initialAnswers);

  React.useEffect(() => {
    if (initialAnswers && Object.keys(initialAnswers).length > 0) {
      setAnswers((prev) => ({ ...prev, ...initialAnswers }));
    }
  }, [initialAnswers]);

  const handleInputChange = (blankId: string, val: string) => {
    if (isSubmitted) return;
    setAnswers((prev) => ({ ...prev, [blankId]: val }));
  };

  // Split text by tokens like [blank_1] or [b1]
  const renderTextWithInputs = () => {
    if (!text) {
      return (
        <div className="space-y-3">
          <p role="alert" className="text-sm text-rose-600">Đề bài chưa có câu hoặc đoạn văn chứa chỗ trống. Vui lòng kiểm tra nội dung bài học.</p>
          {blanks.map((b, idx) => (
            <div key={b.id} className="flex items-center gap-3">
              <span className="text-xs font-bold text-slate-500 w-16">Ô trống {idx + 1}:</span>
              {b.options && b.options.length > 0 ? (
                <select
                  value={answers[b.id] ?? ''}
                  onChange={(e) => handleInputChange(b.id, e.target.value)}
                  disabled={isSubmitted}
                  className="rounded-xl border border-slate-200 px-3 py-1.5 text-sm bg-white"
                >
                  <option value="">-- Chọn đáp án --</option>
                  {b.options.map((opt) => (
                    <option key={opt} value={opt}>
                      {opt}
                    </option>
                  ))}
                </select>
              ) : (
                <input
                  type="text"
                  placeholder={b.hint ?? 'Nhập từ...'}
                  value={answers[b.id] ?? ''}
                  onChange={(e) => handleInputChange(b.id, e.target.value)}
                  disabled={isSubmitted}
                  className="rounded-xl border border-slate-200 px-3 py-1.5 text-sm bg-white max-w-xs focus:ring-2 focus:ring-emerald-500"
                />
              )}
            </div>
          ))}
        </div>
      );
    }

    const parts = text.split(/(\[[a-zA-Z0-9_\-]+\])/g);
    return (
      <div className="select-text leading-loose text-slate-800 text-base font-normal">
        {parts.map((part, index) => {
          const match = part.match(/^\[([a-zA-Z0-9_\-]+)\]$/);
          if (match) {
            const blankId = match[1];
            const blankDef = blanks.find((b) => b.id === blankId);

            if (blankDef?.options && blankDef.options.length > 0) {
              return (
                <select
                  key={index}
                  value={answers[blankId] ?? ''}
                  onChange={(e) => handleInputChange(blankId, e.target.value)}
                  disabled={isSubmitted}
                  className="mx-1.5 inline-block rounded-xl border border-emerald-400 bg-white px-2.5 py-1 text-sm font-semibold text-emerald-900 focus:outline-none focus:ring-2 focus:ring-emerald-500 shadow-2xs"
                >
                  <option value="">(Chọn)</option>
                  {blankDef.options.map((opt) => (
                    <option key={opt} value={opt}>
                      {opt}
                    </option>
                  ))}
                </select>
              );
            }

            return (
              <input
                key={index}
                type="text"
                aria-label={`Ô trống ${blanks.findIndex((b) => b.id === blankId) + 1}`}
                placeholder={blankDef?.hint ?? '...'}
                value={answers[blankId] ?? ''}
                onChange={(e) => handleInputChange(blankId, e.target.value)}
                disabled={isSubmitted}
                className="mx-1.5 inline-block w-32 rounded-xl border border-emerald-400 bg-white px-3 py-1 text-sm font-medium text-slate-900 text-center focus:outline-none focus:ring-2 focus:ring-emerald-500 shadow-2xs"
              />
            );
          }
          return <span key={index}>{part}</span>;
        })}
      </div>
    );
  };

  const hasAnyAnswer = Object.values(answers).some((val) => val.trim().length > 0);

  return (
    <div className="space-y-6 max-w-2xl mx-auto">
      <div className="bg-slate-50/90 p-5 sm:p-6 rounded-2xl border border-slate-100 shadow-xs">
        {renderTextWithInputs()}
      </div>

      {!isSubmitted && (
        <div className="pt-2">
          <Button
            type="button"
            variant="primary"
            onClick={() => onSubmit({ answers })}
            disabled={!hasAnyAnswer || isSubmitting}
            className="w-full gap-2 py-3 rounded-2xl"
          >
            <Check className="w-4 h-4" />
            {isSubmitting ? 'Đang chấm điểm...' : 'Xác nhận nộp bài'}
          </Button>
        </div>
      )}
    </div>
  );
};
