import React from 'react';
import { Sparkles, CheckCircle2, ArrowRight } from 'lucide-react';
import type { GrammarCorrectionItem } from '../types/tutor.types';

interface GrammarCorrectionPanelProps {
  corrections: GrammarCorrectionItem[];
}

export const GrammarCorrectionPanel: React.FC<GrammarCorrectionPanelProps> = ({ corrections }) => {
  if (corrections.length === 0) {
    return (
      <div className="bg-white rounded-3xl border border-slate-200/90 p-5 text-center space-y-2">
        <div className="w-10 h-10 rounded-2xl bg-emerald-50 text-emerald-600 flex items-center justify-center mx-auto">
          <CheckCircle2 className="w-5 h-5" />
        </div>
        <h3 className="text-sm font-bold text-slate-800">Sửa lỗi Ngữ pháp & Từ vựng Realtime</h3>
        <p className="text-xs text-slate-500 leading-relaxed">
          Khi bạn nói tiếng Anh, AI sẽ tự động phân tích ngữ pháp, cấu trúc câu và gợi ý cách diễn đạt tự nhiên hơn tại đây.
        </p>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-3xl border border-slate-200/90 p-5 space-y-4 max-h-[80vh] overflow-y-auto">
      <div className="flex items-center justify-between border-b border-slate-100 pb-3">
        <h3 className="text-sm font-extrabold text-slate-900 flex items-center gap-1.5">
          <Sparkles className="w-4 h-4 text-amber-500" />
          Gợi ý Sửa lỗi ({corrections.length})
        </h3>
      </div>

      <div className="space-y-3">
        {corrections.map((item) => (
          <div
            key={item.turnId}
            className="p-3.5 rounded-2xl bg-amber-50/50 border border-amber-200/80 space-y-2 text-xs"
          >
            <div className="text-rose-700 line-through font-medium">{item.originalSentence}</div>
            <div className="flex items-start gap-1.5 text-emerald-800 font-bold">
              <ArrowRight className="w-3.5 h-3.5 shrink-0 mt-0.5 text-emerald-600" />
              <span>{item.correctedSentence}</span>
            </div>
            {item.naturalAlternative && (
              <div className="text-slate-600 bg-white/80 rounded-xl p-2 border border-amber-100">
                <strong className="text-slate-800">Cách nói tự nhiên (Native):</strong>{' '}
                {item.naturalAlternative}
              </div>
            )}
            {item.overallTipVi && <p className="text-slate-600 italic">{item.overallTipVi}</p>}
          </div>
        ))}
      </div>
    </div>
  );
};
