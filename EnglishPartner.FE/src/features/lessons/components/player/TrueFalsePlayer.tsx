import React, { useState } from 'react';
import { Check } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { cn } from '@/shared/lib/cn';

interface StatementItem {
  id: string;
  text: string;
}

interface TrueFalsePlayerProps {
  content: {
    passage?: string;
    statements?: StatementItem[];
  };
  onSubmit: (payload: { answers: Record<string, boolean> }) => void;
  isSubmitting?: boolean;
  isSubmitted?: boolean;
  initialAnswers?: Record<string, boolean>;
}

export const TrueFalsePlayer: React.FC<TrueFalsePlayerProps> = ({
  content,
  onSubmit,
  isSubmitting = false,
  isSubmitted = false,
  initialAnswers = {},
}) => {
  const statements = content?.statements ?? [];
  const [answers, setAnswers] = useState<Record<string, boolean>>(initialAnswers);

  React.useEffect(() => {
    if (initialAnswers && Object.keys(initialAnswers).length > 0) {
      setAnswers((prev) => ({ ...prev, ...initialAnswers }));
    }
  }, [initialAnswers]);

  const handleSelect = (id: string, value: boolean) => {
    if (isSubmitted) return;
    setAnswers((prev) => ({ ...prev, [id]: value }));
  };

  const isAllAnswered = statements.length > 0 && statements.every((s) => answers[s.id] !== undefined);

  return (
    <div className="space-y-6 max-w-2xl mx-auto">
      {/* Context Passage */}
      {content.passage && (
        <div className="bg-slate-50/90 p-5 rounded-2xl border border-slate-100 text-sm leading-relaxed text-slate-800">
          <span className="text-xs font-bold text-slate-500 uppercase tracking-wider block mb-2">
            Đoạn văn ngữ cảnh:
          </span>
          {content.passage}
        </div>
      )}

      {/* Statements List */}
      <div className="space-y-3">
        <span className="text-xs font-bold text-slate-700 uppercase tracking-wider block">
          Chọn Đúng (True) hoặc Sai (False) cho từng nhận định:
        </span>

        {statements.map((stmt, idx) => {
          const currentVal = answers[stmt.id];
          return (
            <div
              key={stmt.id}
              className="p-4 rounded-2xl border border-slate-200 bg-white flex flex-col sm:flex-row sm:items-center justify-between gap-3 shadow-2xs"
            >
              <div className="text-sm font-medium text-slate-900">
                <span className="font-bold text-slate-400 mr-2">{idx + 1}.</span>
                {stmt.text}
              </div>

              <div className="flex items-center gap-2 shrink-0">
                <button
                  type="button"
                  onClick={() => handleSelect(stmt.id, true)}
                  disabled={isSubmitted}
                  className={cn(
                    'px-4 py-1.5 rounded-xl text-xs font-bold transition-all cursor-pointer',
                    currentVal === true
                      ? 'bg-emerald-600 text-white shadow-2xs'
                      : 'bg-slate-100 text-slate-700 hover:bg-slate-200'
                  )}
                >
                  True (Đúng)
                </button>
                <button
                  type="button"
                  onClick={() => handleSelect(stmt.id, false)}
                  disabled={isSubmitted}
                  className={cn(
                    'px-4 py-1.5 rounded-xl text-xs font-bold transition-all cursor-pointer',
                    currentVal === false
                      ? 'bg-rose-600 text-white shadow-2xs'
                      : 'bg-slate-100 text-slate-700 hover:bg-slate-200'
                  )}
                >
                  False (Sai)
                </button>
              </div>
            </div>
          );
        })}
      </div>

      {/* Submit Action */}
      {!isSubmitted && (
        <div className="pt-2">
          <Button
            type="button"
            variant="primary"
            onClick={() => onSubmit({ answers })}
            disabled={!isAllAnswered || isSubmitting}
            className="w-full gap-2 py-3 rounded-2xl"
          >
            <Check className="w-4 h-4" />
            {isSubmitting ? 'Đang chấm điểm...' : 'Xác nhận nộp bài Đúng / Sai'}
          </Button>
        </div>
      )}
    </div>
  );
};
