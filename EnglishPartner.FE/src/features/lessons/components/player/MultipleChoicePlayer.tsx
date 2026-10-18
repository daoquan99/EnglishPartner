import React, { useState } from 'react';
import { Check, CheckCircle2, Circle } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { cn } from '@/shared/lib/cn';

interface OptionItem {
  id: string;
  text: string;
  explanation?: string;
}

interface MultipleChoicePlayerProps {
  content: {
    question?: string;
    options?: OptionItem[];
    singleSelect?: boolean;
    selectionMode?: 'Single' | 'Multiple';
  };
  onSubmit: (payload: { selectedOptionIds: string[] }) => void;
  isSubmitting?: boolean;
  isSubmitted?: boolean;
  initialSelectedIds?: string[];
}

export const MultipleChoicePlayer: React.FC<MultipleChoicePlayerProps> = ({
  content,
  onSubmit,
  isSubmitting = false,
  isSubmitted = false,
  initialSelectedIds = [],
}) => {
  const options = content?.options ?? [];
  const isSingle = content.singleSelect !== false && content.selectionMode !== 'Multiple';
  const [selectedIds, setSelectedIds] = useState<string[]>(initialSelectedIds);

  const toggleOption = (id: string) => {
    if (isSubmitted) return;
    if (isSingle) {
      setSelectedIds([id]);
    } else {
      setSelectedIds((prev) =>
        prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]
      );
    }
  };

  return (
    <div className="space-y-6 max-w-xl mx-auto">
      {/* Question Prompt */}
      {content.question && (
        <div className="text-base sm:text-lg font-bold text-slate-900 bg-slate-50/80 p-4 sm:p-5 rounded-2xl border border-slate-100">
          {content.question}
        </div>
      )}

      {/* Options List */}
      <div className="space-y-3">
        {options.map((opt) => {
          const isSelected = selectedIds.includes(opt.id);
          return (
            <div
              key={opt.id}
              role="button"
              tabIndex={0}
              onClick={() => toggleOption(opt.id)}
              onKeyDown={(e) => {
                if (e.key === ' ' || e.key === 'Enter') {
                  e.preventDefault();
                  toggleOption(opt.id);
                }
              }}
              className={cn(
                'w-full p-4 rounded-2xl border text-left transition-all duration-200 cursor-pointer flex items-center justify-between gap-3 shadow-xs select-none',
                isSelected
                  ? 'bg-emerald-50/60 border-emerald-500 text-slate-900 ring-1 ring-emerald-500'
                  : 'bg-white border-slate-200 hover:border-slate-300 text-slate-800'
              )}
            >
              <div className="flex items-center gap-3">
                <div className="shrink-0 text-emerald-600">
                  {isSelected ? (
                    <CheckCircle2 className="w-5 h-5 fill-emerald-600 text-white" />
                  ) : (
                    <Circle className="w-5 h-5 text-slate-300" />
                  )}
                </div>
                <span className="text-sm font-medium">{opt.text}</span>
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
            onClick={() => onSubmit({ selectedOptionIds: selectedIds })}
            disabled={selectedIds.length === 0 || isSubmitting}
            className="w-full gap-2 py-3 rounded-2xl"
          >
            <Check className="w-4 h-4" />
            {isSubmitting ? 'Đang chấm điểm...' : 'Xác nhận nộp đáp án'}
          </Button>
        </div>
      )}
    </div>
  );
};
