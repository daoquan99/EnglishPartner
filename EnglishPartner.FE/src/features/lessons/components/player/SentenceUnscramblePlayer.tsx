import React, { useState } from 'react';
import { Check, RotateCcw } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';

interface TokenItem {
  id: string;
  text: string;
}

interface SentenceUnscramblePlayerProps {
  content: {
    tokens?: TokenItem[];
    translationHint?: string;
  };
  onSubmit: (payload: { orderedTokenIds: string[] }) => void;
  isSubmitting?: boolean;
  isSubmitted?: boolean;
  initialOrderedTokenIds?: string[];
}

export const SentenceUnscramblePlayer: React.FC<SentenceUnscramblePlayerProps> = ({
  content,
  onSubmit,
  isSubmitting = false,
  isSubmitted = false,
  initialOrderedTokenIds = [],
}) => {
  const allTokens = content?.tokens ?? [];

  // Available token IDs initially: all tokens not in initialOrderedTokenIds
  const [selectedTokenIds, setSelectedTokenIds] = useState<string[]>(initialOrderedTokenIds);

  const availableTokenIds = allTokens
    .map((t) => t.id)
    .filter((id) => !selectedTokenIds.includes(id));

  const addToken = (id: string) => {
    if (isSubmitted) return;
    setSelectedTokenIds((prev) => [...prev, id]);
  };

  const removeToken = (id: string) => {
    if (isSubmitted) return;
    setSelectedTokenIds((prev) => prev.filter((x) => x !== id));
  };

  const resetTokens = () => {
    if (isSubmitted) return;
    setSelectedTokenIds([]);
  };

  const getTokenText = (id: string) => allTokens.find((t) => t.id === id)?.text ?? id;

  return (
    <div className="space-y-6 max-w-2xl mx-auto">
      {/* Translation / Meaning Hint */}
      {content.translationHint && (
        <div className="text-sm font-semibold text-slate-700 bg-slate-50 p-4 rounded-2xl border border-slate-100 text-center">
          Gợi ý nghĩa: &ldquo;{content.translationHint}&rdquo;
        </div>
      )}

      {/* Selected sentence staging area */}
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <span className="text-xs font-bold text-slate-700 uppercase tracking-wider">
            Câu của bạn (Bấm từ để bỏ chọn):
          </span>
          {selectedTokenIds.length > 0 && !isSubmitted && (
            <button
              type="button"
              onClick={resetTokens}
              className="text-xs text-rose-600 hover:underline flex items-center gap-1 cursor-pointer"
            >
              <RotateCcw className="w-3.5 h-3.5" /> Đặt lại
            </button>
          )}
        </div>

        <div className="min-h-[72px] p-4 rounded-2xl border-2 border-dashed border-emerald-500/30 bg-emerald-50/20 flex flex-wrap items-center gap-2">
          {selectedTokenIds.length === 0 ? (
            <span className="text-xs text-slate-400 italic">
              Bấm vào các từ bên dưới để xếp thành câu hoàn chỉnh...
            </span>
          ) : (
            selectedTokenIds.map((id) => (
              <button
                key={id}
                type="button"
                onClick={() => removeToken(id)}
                disabled={isSubmitted}
                className="px-3.5 py-2 rounded-xl bg-emerald-600 text-white font-medium text-sm shadow-xs hover:bg-emerald-700 transition-all cursor-pointer"
              >
                {getTokenText(id)}
              </button>
            ))
          )}
        </div>
      </div>

      {/* Available tokens pool */}
      <div className="space-y-2 pt-2">
        <span className="text-xs font-bold text-slate-700 uppercase tracking-wider block">
          Kho từ vựng (Bấm từ để đưa vào câu):
        </span>
        <div className="flex flex-wrap gap-2.5 p-4 bg-slate-50 rounded-2xl border border-slate-100 min-h-[64px]">
          {availableTokenIds.map((id) => (
            <button
              key={id}
              type="button"
              onClick={() => addToken(id)}
              disabled={isSubmitted}
              className="px-3.5 py-2 rounded-xl bg-white border border-slate-200 text-slate-800 font-medium text-sm shadow-2xs hover:border-emerald-500 hover:text-emerald-700 hover:bg-emerald-50/50 transition-all cursor-pointer"
            >
              {getTokenText(id)}
            </button>
          ))}
          {availableTokenIds.length === 0 && (
            <span className="text-xs text-slate-400 italic">Đã dùng hết tất cả các từ trong kho.</span>
          )}
        </div>
      </div>

      {/* Submit Action */}
      {!isSubmitted && (
        <div className="pt-2">
          <Button
            type="button"
            variant="primary"
            onClick={() => onSubmit({ orderedTokenIds: selectedTokenIds })}
            disabled={selectedTokenIds.length === 0 || isSubmitting}
            className="w-full gap-2 py-3 rounded-2xl"
          >
            <Check className="w-4 h-4" />
            {isSubmitting ? 'Đang chấm điểm...' : 'Xác nhận nộp câu'}
          </Button>
        </div>
      )}
    </div>
  );
};
