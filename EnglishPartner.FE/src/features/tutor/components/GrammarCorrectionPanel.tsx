import React from 'react';
import {
  Sparkles,
  CheckCircle2,
  ArrowRight,
  Volume2,
  Square,
  Lightbulb,
  Loader2,
  Wand2,
} from 'lucide-react';
import type { GrammarCorrectionItem } from '../types/tutor.types';
import { Badge } from '@/shared/components/ui/Badge';
import { Button } from '@/shared/components/ui/Button';

interface GrammarCorrectionPanelProps {
  corrections: GrammarCorrectionItem[];
  isAnalyzing?: boolean;
  canAnalyzeLatestTurn?: boolean;
  replayingTurnId?: string | null;
  onAnalyzeLatestTurn?: () => void;
  onReplayUserTurnAudio?: (turnId: string, fallbackText: string) => void;
}

export const GrammarCorrectionPanel: React.FC<GrammarCorrectionPanelProps> = ({
  corrections,
  isAnalyzing = false,
  canAnalyzeLatestTurn = false,
  replayingTurnId = null,
  onAnalyzeLatestTurn,
  onReplayUserTurnAudio,
}) => {
  const speakEnglish = (text: string) => {
    if (!('speechSynthesis' in window) || !text) return;
    window.speechSynthesis.cancel();
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.lang = 'en-US';
    utterance.rate = 0.95;
    window.speechSynthesis.speak(utterance);
  };

  if (corrections.length === 0) {
    return (
      <div className="bg-white rounded-3xl border border-slate-200/90 p-5 text-center space-y-3 shadow-xs">
        <div className="w-11 h-11 rounded-2xl bg-emerald-50 text-emerald-600 flex items-center justify-center mx-auto">
          {isAnalyzing ? (
            <Loader2 className="w-5 h-5 animate-spin text-amber-600" />
          ) : (
            <CheckCircle2 className="w-5 h-5" />
          )}
        </div>
        <div className="space-y-1">
          <h3 className="text-sm font-extrabold text-slate-800">
            Sửa lỗi Ngữ pháp & Từ vựng Realtime
          </h3>
          <p className="text-xs text-slate-500 leading-relaxed">
            {isAnalyzing
              ? 'AI đang phân tích câu nói vừa rồi của bạn để gợi ý ngữ pháp & từ vựng...'
              : 'Khi bạn nói tiếng Anh, AI sẽ tự động phân tích ngữ pháp, cấu trúc câu và gợi ý cách diễn đạt tự nhiên hơn tại đây.'}
          </p>
        </div>

        {canAnalyzeLatestTurn && onAnalyzeLatestTurn && (
          <div className="pt-1">
            <Button
              variant="outline"
              size="sm"
              disabled={isAnalyzing}
              onClick={onAnalyzeLatestTurn}
              className="mx-auto border-amber-300 bg-amber-50/70 hover:bg-amber-100 text-amber-900 font-bold"
            >
              <Wand2 className="w-3.5 h-3.5 text-amber-700" />
              Phân tích lượt nói gần nhất
            </Button>
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="bg-white rounded-3xl border border-slate-200/90 p-4 sm:p-5 flex flex-col h-[calc(100vh-9.5rem)] min-h-[540px] shadow-xs">
      <div className="flex items-center justify-between gap-2 border-b border-slate-100 pb-3 shrink-0">
        <div className="flex items-center gap-2">
          <Sparkles className="w-4 h-4 text-amber-500 shrink-0" />
          <h3 className="text-sm font-extrabold text-slate-900">
            Sửa lỗi Ngữ pháp & Từ vựng ({corrections.length})
          </h3>
          {isAnalyzing && (
            <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-amber-700 bg-amber-50 px-2 py-0.5 rounded-full border border-amber-200">
              <Loader2 className="w-3 h-3 animate-spin" />
              Đang chấm...
            </span>
          )}
        </div>

        {canAnalyzeLatestTurn && onAnalyzeLatestTurn && (
          <button
            type="button"
            disabled={isAnalyzing}
            onClick={onAnalyzeLatestTurn}
            className="text-[11px] font-bold text-emerald-700 hover:underline inline-flex items-center gap-1 cursor-pointer disabled:opacity-50"
          >
            <Wand2 className="w-3 h-3" />
            Chấm lại
          </button>
        )}
      </div>

      <div className="flex-1 overflow-y-auto space-y-3 pt-3 pr-1">
        {corrections.map((item) => {
          const isSameText =
            item.originalSentence.trim().toLowerCase() ===
            item.correctedSentence.trim().toLowerCase();
          const isReplayingOriginal = replayingTurnId === item.turnId;

          return (
            <div
              key={`${item.turnId}_${item.id || item.timestamp}`}
              className="p-3.5 rounded-2xl bg-amber-50/45 border border-amber-200/80 space-y-2.5 text-xs shadow-2xs"
            >
              <div className="flex items-center justify-between gap-2">
                <Badge variant="amber">{item.errorCategory || 'Grammar & Vocabulary'}</Badge>
                <span className="text-[10px] text-slate-400 font-medium">
                  {new Date(item.timestamp).toLocaleTimeString('vi-VN')}
                </span>
              </div>

              {/* Original Utterance */}
              <div className="flex items-start justify-between gap-2 bg-rose-50/70 border border-rose-200/70 rounded-xl px-3 py-2">
                <div className="space-y-0.5">
                  <span className="text-[10px] font-bold uppercase tracking-wider text-rose-600 block">
                    Câu gốc của bạn
                  </span>
                  <p
                    className={
                      isSameText
                        ? 'text-slate-700 font-medium leading-relaxed'
                        : 'text-rose-700 line-through font-medium leading-relaxed'
                    }
                  >
                    {item.originalSentence}
                  </p>
                </div>
                <button
                  type="button"
                  onClick={() => {
                    if (onReplayUserTurnAudio) {
                      onReplayUserTurnAudio(item.turnId, item.originalSentence);
                    } else {
                      speakEnglish(item.originalSentence);
                    }
                  }}
                  title={
                    isReplayingOriginal
                      ? 'Dừng phát lại'
                      : 'Nghe lại câu gốc (Giọng thu âm của bạn)'
                  }
                  className="p-1 rounded-lg text-rose-600 hover:bg-rose-100 shrink-0 cursor-pointer"
                >
                  {isReplayingOriginal ? (
                    <Square className="w-3.5 h-3.5 fill-current animate-pulse" />
                  ) : (
                    <Volume2 className="w-3.5 h-3.5" />
                  )}
                </button>
              </div>

              {/* Corrected / Upgraded Sentence */}
              <div className="flex items-start justify-between gap-2 bg-emerald-50/80 border border-emerald-200 rounded-xl px-3 py-2">
                <div className="space-y-0.5">
                  <span className="text-[10px] font-bold uppercase tracking-wider text-emerald-700 flex items-center gap-1">
                    <ArrowRight className="w-3 h-3" />
                    Câu chuẩn / Nâng cấp diễn đạt
                  </span>
                  <p className="text-emerald-900 font-bold leading-relaxed">
                    {item.correctedSentence}
                  </p>
                </div>
                <button
                  type="button"
                  onClick={() => speakEnglish(item.correctedSentence)}
                  title="Nghe phát âm mẫu câu đã sửa"
                  className="p-1 rounded-lg text-emerald-700 hover:bg-emerald-100 shrink-0 cursor-pointer"
                >
                  <Volume2 className="w-3.5 h-3.5" />
                </button>
              </div>

              {item.naturalAlternative && (
                <div className="text-slate-700 bg-white/90 rounded-xl p-2.5 border border-amber-100 leading-relaxed">
                  <strong className="text-slate-900">Cách nói tự nhiên (Native):</strong>{' '}
                  {item.naturalAlternative}
                </div>
              )}

              {(item.explanationVi || item.overallTipVi) && (
                <p className="text-slate-700 leading-relaxed bg-white/75 rounded-xl px-3 py-2 border border-slate-200/70">
                  <strong className="text-slate-900">Giải thích:</strong>{' '}
                  {item.explanationVi || item.overallTipVi}
                </p>
              )}

              {item.cefrTip && (
                <div className="flex items-start gap-1.5 text-[11px] text-amber-900 bg-amber-100/60 border border-amber-200/70 rounded-xl px-2.5 py-2">
                  <Lightbulb className="w-3.5 h-3.5 text-amber-600 shrink-0 mt-0.5" />
                  <span className="font-medium leading-relaxed">{item.cefrTip}</span>
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
};
