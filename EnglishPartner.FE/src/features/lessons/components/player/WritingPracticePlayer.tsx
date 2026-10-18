import React, { useState } from 'react';
import { Edit3, Check, FileText } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';

interface WritingPracticePlayerProps {
  content: {
    prompt?: string;
    taskInstructions?: string;
    minWords?: number;
    maxWords?: number;
  };
  onSubmit: (payload: { writtenText: string }) => void;
  isSubmitting?: boolean;
  isSubmitted?: boolean;
  initialWrittenText?: string;
}

export const WritingPracticePlayer: React.FC<WritingPracticePlayerProps> = ({
  content,
  onSubmit,
  isSubmitting = false,
  isSubmitted = false,
  initialWrittenText = '',
}) => {
  const [text, setText] = useState(initialWrittenText);

  const wordCount = text.trim() ? text.trim().split(/\s+/).length : 0;
  const minWords = content.minWords ?? 30;
  const maxWords = content.maxWords ?? 150;
  const isCountValid = wordCount >= minWords && wordCount <= maxWords * 1.5;

  return (
    <div className="space-y-6 max-w-2xl mx-auto">
      {/* Writing Prompt */}
      <div className="bg-slate-50 p-6 rounded-3xl border border-slate-100 space-y-3 shadow-xs">
        <div className="flex items-center justify-between">
          <span className="text-xs uppercase font-extrabold tracking-wider text-emerald-600 flex items-center gap-1.5">
            <Edit3 className="w-4 h-4" /> Đề bài luyện viết (Writing Practice)
          </span>

          <Badge variant="outline" className="font-semibold text-slate-600">
            Mục tiêu: {minWords} - {maxWords} từ
          </Badge>
        </div>

        <div className="text-base sm:text-lg font-bold text-slate-900 leading-snug">
          {content.prompt}
        </div>

        {content.taskInstructions && (
          <p className="text-xs text-slate-600 leading-relaxed">{content.taskInstructions}</p>
        )}
      </div>

      {/* Writing Textarea */}
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <label className="text-xs font-bold text-slate-700 block">
            Bài viết của bạn:
          </label>
          <div className="flex items-center gap-1.5 text-xs">
            <FileText className="w-3.5 h-3.5 text-slate-400" />
            <span
              className={
                wordCount < minWords
                  ? 'text-amber-600 font-semibold'
                  : wordCount > maxWords
                  ? 'text-sky-600 font-semibold'
                  : 'text-emerald-700 font-bold'
              }
            >
              {wordCount} từ
            </span>
          </div>
        </div>

        <textarea
          rows={8}
          value={text}
          onChange={(e) => setText(e.target.value)}
          disabled={isSubmitted}
          placeholder="Bắt đầu viết bài luận hoặc đoạn văn tại đây..."
          className="w-full text-sm font-medium rounded-2xl border border-slate-200 bg-white p-4 focus:outline-none focus:ring-2 focus:ring-emerald-500 shadow-xs leading-relaxed"
        />
      </div>

      {/* Submit Action */}
      {!isSubmitted && (
        <div className="pt-2">
          <Button
            type="button"
            variant="primary"
            onClick={() => onSubmit({ writtenText: text.trim() })}
            disabled={wordCount === 0 || isSubmitting}
            className="w-full gap-2 py-3 rounded-2xl"
          >
            <Check className="w-4 h-4" />
            {isSubmitting ? 'AI đang chấm rubric bài viết...' : 'Xác nhận nộp bài viết'}
          </Button>
        </div>
      )}
    </div>
  );
};
