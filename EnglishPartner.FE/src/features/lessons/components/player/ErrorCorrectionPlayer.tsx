import React, { useState } from 'react';
import { AlertCircle, Check } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';

interface ErrorCorrectionPlayerProps {
  content: {
    sentenceWithErrors?: string;
    incorrectSentence?: string;
    errorFocus?: string;
  };
  onSubmit: (payload: { correctedSentence: string }) => void;
  isSubmitting?: boolean;
  isSubmitted?: boolean;
  initialCorrectedSentence?: string;
}

export const ErrorCorrectionPlayer: React.FC<ErrorCorrectionPlayerProps> = ({
  content,
  onSubmit,
  isSubmitting = false,
  isSubmitted = false,
  initialCorrectedSentence = '',
}) => {
  const sentence = content.incorrectSentence ?? content.sentenceWithErrors ?? '';
  const [corrected, setCorrected] = useState(initialCorrectedSentence);

  return (
    <div className="space-y-6 max-w-xl mx-auto">
      {/* Incorrect Sentence Display */}
      <div className="bg-rose-50/60 border border-rose-200/80 p-5 rounded-2xl space-y-2">
        <div className="flex items-center gap-2 text-rose-800 text-xs font-bold uppercase tracking-wider">
          <AlertCircle className="w-4 h-4 shrink-0" />
          Câu chứa lỗi sai:
        </div>
        <p className="text-base font-semibold text-rose-950">&ldquo;{sentence}&rdquo;</p>
        {content.errorFocus && (
          <span className="text-xs text-rose-700 block italic">
            Trọng tâm lỗi: {content.errorFocus}
          </span>
        )}
      </div>

      {/* Correction Input */}
      <div className="space-y-2">
        <label className="text-xs font-bold text-slate-700 block">
          Nhập lại câu hoàn chỉnh sau khi đã sửa đúng:
        </label>
        <textarea
          rows={3}
          value={corrected}
          onChange={(e) => setCorrected(e.target.value)}
          disabled={isSubmitted}
          placeholder="Gõ câu chuẩn tại đây..."
          className="w-full text-sm font-medium rounded-2xl border border-slate-200 bg-white p-4 focus:outline-none focus:ring-2 focus:ring-emerald-500 shadow-xs"
        />
      </div>

      {/* Submit Action */}
      {!isSubmitted && (
        <div className="pt-2">
          <Button
            type="button"
            variant="primary"
            onClick={() => onSubmit({ correctedSentence: corrected.trim() })}
            disabled={corrected.trim().length === 0 || isSubmitting}
            className="w-full gap-2 py-3 rounded-2xl"
          >
            <Check className="w-4 h-4" />
            {isSubmitting ? 'Đang chấm điểm...' : 'Xác nhận nộp câu sửa'}
          </Button>
        </div>
      )}
    </div>
  );
};
