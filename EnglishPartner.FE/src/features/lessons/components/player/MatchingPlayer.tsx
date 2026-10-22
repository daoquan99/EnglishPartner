import React, { useState } from 'react';
import { Check, Link2, X } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { cn } from '@/shared/lib/cn';

interface MatchingItem {
  id: string;
  text: string;
  imageUrl?: string;
}

interface MatchingPlayerProps {
  content: {
    leftItems?: MatchingItem[];
    rightItems?: MatchingItem[];
    // Or single pairs array for initial configuration
    pairs?: Array<{ leftId: string; leftText: string; rightId: string; rightText: string }>;
  };
  onSubmit: (payload: { pairs: Array<{ leftId: string; rightId: string }> }) => void;
  isSubmitting?: boolean;
  isSubmitted?: boolean;
  initialPairs?: Array<{ leftId: string; rightId: string }>;
}

export const MatchingPlayer: React.FC<MatchingPlayerProps> = ({
  content,
  onSubmit,
  isSubmitting = false,
  isSubmitted = false,
  initialPairs = [],
}) => {
  // Extract left and right items
  const leftList: MatchingItem[] =
    content.leftItems ??
    (content.pairs?.map((p) => ({ id: p.leftId, text: p.leftText })) ?? []);

  const rightList: MatchingItem[] =
    content.rightItems ??
    (content.pairs?.map((p) => ({ id: p.rightId, text: p.rightText })) ?? []);

  const [pairs, setPairs] = useState<Array<{ leftId: string; rightId: string }>>(initialPairs);
  const [selectedLeftId, setSelectedLeftId] = useState<string | null>(null);
  const [selectedRightId, setSelectedRightId] = useState<string | null>(null);

  React.useEffect(() => {
    if (initialPairs && initialPairs.length > 0) {
      setPairs(initialPairs);
    }
  }, [initialPairs]);

  const isLeftMatched = (id: string) => pairs.some((p) => p.leftId === id);
  const isRightMatched = (id: string) => pairs.some((p) => p.rightId === id);

  const handleSelectLeft = (id: string) => {
    if (isSubmitted) return;
    if (isLeftMatched(id)) {
      // Remove pair
      setPairs((prev) => prev.filter((p) => p.leftId !== id));
      return;
    }

    if (selectedRightId) {
      // Form pair
      setPairs((prev) => [...prev, { leftId: id, rightId: selectedRightId }]);
      setSelectedLeftId(null);
      setSelectedRightId(null);
    } else {
      setSelectedLeftId(selectedLeftId === id ? null : id);
    }
  };

  const handleSelectRight = (id: string) => {
    if (isSubmitted) return;
    if (isRightMatched(id)) {
      // Remove pair
      setPairs((prev) => prev.filter((p) => p.rightId !== id));
      return;
    }

    if (selectedLeftId) {
      // Form pair
      setPairs((prev) => [...prev, { leftId: selectedLeftId, rightId: id }]);
      setSelectedLeftId(null);
      setSelectedRightId(null);
    } else {
      setSelectedRightId(selectedRightId === id ? null : id);
    }
  };

  const removePair = (leftId: string) => {
    if (isSubmitted) return;
    setPairs((prev) => prev.filter((p) => p.leftId !== leftId));
  };

  const getMatchedRightText = (leftId: string) => {
    const pair = pairs.find((p) => p.leftId === leftId);
    if (!pair) return null;
    return rightList.find((r) => r.id === pair.rightId)?.text;
  };

  return (
    <div className="space-y-6 max-w-2xl mx-auto">
      <div className="text-xs text-slate-500 font-medium text-center">
        Bấm chọn một mục bên trái và một mục bên phải tương ứng để nối cặp. Bấm vào cặp đã nối để gỡ bỏ.
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
        {/* Left Column */}
        <div className="space-y-3">
          <span className="text-xs font-bold text-slate-700 uppercase tracking-wider block">
            Cột A (Thuật ngữ / Từ vựng)
          </span>
          {leftList.map((item) => {
            const matched = isLeftMatched(item.id);
            const isSelected = selectedLeftId === item.id;
            return (
              <div
                key={item.id}
                role="button"
                tabIndex={0}
                onClick={() => handleSelectLeft(item.id)}
                className={cn(
                  'p-3.5 rounded-2xl border text-sm font-medium transition-all select-none cursor-pointer flex items-center justify-between shadow-2xs',
                  matched
                    ? 'bg-emerald-50 border-emerald-500/40 text-emerald-900 ring-1 ring-emerald-500/20'
                    : isSelected
                    ? 'bg-sky-50 border-sky-400 ring-2 ring-sky-300 text-sky-900'
                    : 'bg-white border-slate-200 hover:border-slate-300 text-slate-800'
                )}
              >
                <span>{item.text}</span>
                {matched && <Link2 className="w-4 h-4 text-emerald-600 shrink-0" />}
              </div>
            );
          })}
        </div>

        {/* Right Column */}
        <div className="space-y-3">
          <span className="text-xs font-bold text-slate-700 uppercase tracking-wider block">
            Cột B (Định nghĩa / Ghép cặp)
          </span>
          {rightList.map((item) => {
            const matched = isRightMatched(item.id);
            const isSelected = selectedRightId === item.id;
            return (
              <div
                key={item.id}
                role="button"
                tabIndex={0}
                onClick={() => handleSelectRight(item.id)}
                className={cn(
                  'p-3.5 rounded-2xl border text-sm font-medium transition-all select-none cursor-pointer flex items-center justify-between shadow-2xs',
                  matched
                    ? 'bg-emerald-50 border-emerald-500/40 text-emerald-900 ring-1 ring-emerald-500/20'
                    : isSelected
                    ? 'bg-sky-50 border-sky-400 ring-2 ring-sky-300 text-sky-900'
                    : 'bg-white border-slate-200 hover:border-slate-300 text-slate-800'
                )}
              >
                <span>{item.text}</span>
                {matched && <Link2 className="w-4 h-4 text-emerald-600 shrink-0" />}
              </div>
            );
          })}
        </div>
      </div>

      {/* Formed Pairs Summary */}
      {pairs.length > 0 && (
        <div className="pt-2 border-t border-slate-100 space-y-2">
          <span className="text-xs font-bold text-slate-700 block">
            Các cặp đã nối ({pairs.length}/{Math.max(leftList.length, rightList.length)}):
          </span>
          <div className="flex flex-wrap gap-2">
            {pairs.map((p) => {
              const leftText = leftList.find((l) => l.id === p.leftId)?.text ?? p.leftId;
              const rightText = rightList.find((r) => r.id === p.rightId)?.text ?? p.rightId;
              return (
                <div
                  key={p.leftId}
                  className="inline-flex items-center gap-2 bg-emerald-50 text-emerald-900 border border-emerald-200 px-3 py-1.5 rounded-xl text-xs font-medium"
                >
                  <span>{leftText} ➔ {rightText}</span>
                  {!isSubmitted && (
                    <button
                      type="button"
                      onClick={() => removePair(p.leftId)}
                      className="hover:text-rose-600 cursor-pointer"
                      title="Gỡ cặp"
                    >
                      <X className="w-3.5 h-3.5" />
                    </button>
                  )}
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* Submit Action */}
      {!isSubmitted && (
        <div className="pt-2">
          <Button
            type="button"
            variant="primary"
            onClick={() => onSubmit({ pairs })}
            disabled={pairs.length === 0 || isSubmitting}
            className="w-full gap-2 py-3 rounded-2xl"
          >
            <Check className="w-4 h-4" />
            {isSubmitting ? 'Đang chấm điểm...' : 'Xác nhận nộp các cặp đã nối'}
          </Button>
        </div>
      )}
    </div>
  );
};
