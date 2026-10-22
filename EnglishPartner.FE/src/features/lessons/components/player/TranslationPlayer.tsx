import React, { useState } from 'react';
import { ArrowRightLeft, Check } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';

interface TranslationPlayerProps {
  content: {
    sourceText?: string;
    direction?: 'EnToVi' | 'ViToEn' | string;
    contextHint?: string;
  };
  onSubmit: (payload: { translatedText: string }) => void;
  isSubmitting?: boolean;
  isSubmitted?: boolean;
  initialTranslatedText?: string;
}

export const TranslationPlayer: React.FC<TranslationPlayerProps> = ({
  content,
  onSubmit,
  isSubmitting = false,
  isSubmitted = false,
  initialTranslatedText = '',
}) => {
  const [translated, setTranslated] = useState(initialTranslatedText);
  const isEnToVi = content.direction === 'EnToVi' || !content.direction;

  React.useEffect(() => {
    if (initialTranslatedText) {
      setTranslated(initialTranslatedText);
    }
  }, [initialTranslatedText]);

  return (
    <div className="space-y-6 max-w-xl mx-auto">
      {/* Direction & Source Box */}
      <div className="bg-slate-50 p-5 rounded-2xl border border-slate-100 space-y-3">
        <div className="flex items-center justify-between">
          <Badge variant="outline" className="gap-1.5 font-bold">
            <ArrowRightLeft className="w-3 h-3" />
            {isEnToVi ? 'Tiếng Anh ➔ Tiếng Việt' : 'Tiếng Việt ➔ Tiếng Anh'}
          </Badge>
          {content.contextHint && (
            <span className="text-xs text-slate-500 italic">{content.contextHint}</span>
          )}
        </div>

        <div className="text-base sm:text-lg font-bold text-slate-900 leading-snug">
          &ldquo;{content.sourceText}&rdquo;
        </div>
      </div>

      {/* Translation Textarea */}
      <div className="space-y-2">
        <label className="text-xs font-bold text-slate-700 block">
          Bản dịch của bạn ({isEnToVi ? 'Viết bằng Tiếng Việt' : 'Write in English'}):
        </label>
        <textarea
          rows={3}
          value={translated}
          onChange={(e) => setTranslated(e.target.value)}
          disabled={isSubmitted}
          placeholder="Nhập bản dịch tại đây..."
          className="w-full text-sm font-medium rounded-2xl border border-slate-200 bg-white p-4 focus:outline-none focus:ring-2 focus:ring-emerald-500 shadow-xs"
        />
      </div>

      {/* Submit Action */}
      {!isSubmitted && (
        <div className="pt-2">
          <Button
            type="button"
            variant="primary"
            onClick={() => onSubmit({ translatedText: translated.trim() })}
            disabled={translated.trim().length === 0 || isSubmitting}
            className="w-full gap-2 py-3 rounded-2xl"
          >
            <Check className="w-4 h-4" />
            {isSubmitting ? 'AI đang chấm điểm...' : 'Xác nhận nộp bản dịch'}
          </Button>
        </div>
      )}
    </div>
  );
};
