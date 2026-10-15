import React, { useEffect, useRef } from 'react';
import { Volume2, User, Bot, Sparkles, CheckCircle2 } from 'lucide-react';
import type { BilingualTurnItem, GrammarCorrectionItem } from '../types/tutor.types';
import { cn } from '@/shared/lib/cn';

interface LiveMessengerStreamProps {
  turns: BilingualTurnItem[];
  corrections: GrammarCorrectionItem[];
  onSelectCorrection?: (correction: GrammarCorrectionItem) => void;
}

export const LiveMessengerStream: React.FC<LiveMessengerStreamProps> = ({
  turns,
  corrections,
  onSelectCorrection,
}) => {
  const bottomRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [turns.length]);

  const speakEnglishText = (text: string) => {
    if (!('speechSynthesis' in window) || !text) return;
    window.speechSynthesis.cancel();
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.lang = 'en-US';
    utterance.rate = 0.95;
    window.speechSynthesis.speak(utterance);
  };

  if (turns.length === 0) {
    return (
      <div className="flex-1 flex flex-col items-center justify-center p-8 text-center">
        <div className="w-16 h-16 rounded-3xl bg-emerald-50 border border-emerald-200 flex items-center justify-center text-emerald-600 mb-4 shadow-xs">
          <Sparkles className="w-8 h-8" />
        </div>
        <h3 className="text-lg font-extrabold text-slate-800">
          Sẵn sàng bắt đầu phiên hội thoại song ngữ
        </h3>
        <p className="text-xs text-slate-500 max-w-md mt-1 leading-relaxed">
          Nhấn nút <strong>Bắt đầu Luyện Nói Live</strong> bên dưới để kết nối đồng thời 3 SignalR Hubs:
          Hub 1 (Giọng nói AI 24kHz), Hub 2 (Phụ đề Tiếng Anh + Sửa ngữ pháp), và Hub 3 (Dịch Tiếng Việt tức thì).
        </p>
      </div>
    );
  }

  return (
    <div className="flex-1 overflow-y-auto p-4 sm:p-6 space-y-4">
      {turns.map((turn) => {
        const isUser = turn.speaker === 'user';
        const turnCorrection = isUser
          ? corrections.find((c) => c.turnId === turn.turnId && c.hasErrors)
          : undefined;

        return (
          <div
            key={turn.turnId}
            className={cn('flex items-start gap-3', isUser ? 'flex-row-reverse' : 'flex-row')}
          >
            <div
              className={cn(
                'w-9 h-9 rounded-2xl flex items-center justify-center shrink-0 shadow-xs',
                isUser
                  ? 'bg-emerald-600 text-white'
                  : 'bg-amber-100 text-amber-800 border border-amber-200'
              )}
            >
              {isUser ? <User className="w-4 h-4" /> : <Bot className="w-4 h-4" />}
            </div>

            <div className={cn('max-w-[82%] space-y-1.5', isUser ? 'items-end' : 'items-start')}>
              <div
                className={cn(
                  'rounded-2xl px-4 py-3 shadow-xs border transition-all',
                  isUser
                    ? 'bg-emerald-600 text-white border-emerald-700 rounded-tr-xs'
                    : 'bg-white text-slate-800 border-slate-200 rounded-tl-xs'
                )}
              >
                {/* Line 1: English Transcript */}
                <div className="flex items-start justify-between gap-2">
                  <p className="text-sm font-semibold leading-relaxed">
                    {turn.englishLine1 || '...'}
                  </p>
                  {turn.englishLine1 && (
                    <button
                      type="button"
                      onClick={() => speakEnglishText(turn.englishLine1)}
                      title="Nghe lại câu tiếng Anh"
                      className={cn(
                        'p-1 rounded-lg shrink-0 transition-colors cursor-pointer',
                        isUser
                          ? 'text-emerald-100 hover:bg-emerald-500'
                          : 'text-slate-400 hover:bg-slate-100 hover:text-slate-700'
                      )}
                    >
                      <Volume2 className="w-3.5 h-3.5" />
                    </button>
                  )}
                </div>

                {/* Line 2: Vietnamese Translation */}
                <p
                  className={cn(
                    'text-xs mt-1.5 pt-1.5 border-t leading-relaxed',
                    isUser
                      ? 'text-emerald-100 border-emerald-500/60'
                      : 'text-slate-500 border-slate-100'
                  )}
                >
                  {turn.vietnameseLine2 || 'Đang dịch sang tiếng Việt...'}
                </p>
              </div>

              {/* Inline Grammar Suggestion Pill if errors were detected */}
              {turnCorrection && (
                <button
                  type="button"
                  onClick={() => onSelectCorrection?.(turnCorrection)}
                  className="inline-flex items-center gap-1.5 px-3 py-1 rounded-xl bg-amber-50 hover:bg-amber-100 border border-amber-200 text-amber-900 text-xs font-semibold transition-colors cursor-pointer"
                >
                  <CheckCircle2 className="w-3.5 h-3.5 text-amber-600" />
                  <span>Gợi ý nâng cấp câu: &ldquo;{turnCorrection.correctedSentence}&rdquo;</span>
                </button>
              )}
            </div>
          </div>
        );
      })}
      <div ref={bottomRef} />
    </div>
  );
};
