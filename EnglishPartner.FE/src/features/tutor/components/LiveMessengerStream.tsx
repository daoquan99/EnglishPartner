import React, { useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { Volume2, Square, User, Bot, Sparkles, CheckCircle2, Wand2 } from 'lucide-react';
import type { BilingualTurnItem, GrammarCorrectionItem } from '../types/tutor.types';
import { cn } from '@/shared/lib/cn';

interface LiveMessengerStreamProps {
  turns: BilingualTurnItem[];
  corrections: GrammarCorrectionItem[];
  replayingTurnId?: string | null;
  onReplayTurnAudio?: (turn: BilingualTurnItem) => void;
  onSelectCorrection?: (correction: GrammarCorrectionItem) => void;
  onAnalyzeTurn?: (turnId: string, englishText: string) => void;
}

export const LiveMessengerStream: React.FC<LiveMessengerStreamProps> = ({
  turns,
  corrections,
  replayingTurnId = null,
  onReplayTurnAudio,
  onSelectCorrection,
  onAnalyzeTurn,
}) => {
  const { t } = useTranslation();
  const bottomRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [turns.length]);

  const handleReplayTurn = (turn: BilingualTurnItem) => {
    if (onReplayTurnAudio) {
      onReplayTurnAudio(turn);
      return;
    }
    if (!('speechSynthesis' in window) || !turn.englishLine1) return;
    window.speechSynthesis.cancel();
    const utterance = new SpeechSynthesisUtterance(turn.englishLine1);
    utterance.lang = 'en-US';
    utterance.rate = 0.95;
    window.speechSynthesis.speak(utterance);
  };

  if (turns.length === 0) {
    return (
      <div className="flex-1 flex flex-col items-center justify-center p-8 text-center">
        <img
          src="/assets/illustrations/illustration-welcome.svg"
          alt=""
          className="w-44 h-44 object-contain mb-3 drop-shadow-xs select-none pointer-events-none"
        />
        <h3 className="text-lg font-extrabold text-slate-800">
          {t('tutor.readyTitle', 'Sẵn sàng bắt đầu phiên hội thoại song ngữ')}
        </h3>
        <p className="text-xs text-slate-500 max-w-md mt-1 leading-relaxed">
          {t(
            'tutor.readyDesc',
            'Nhập Google AI Studio API Key ở cột Thiết lập bên trái rồi nhấn Bắt đầu luyện nói để kết nối đồng thời 3 SignalR Hubs: Hub 1 (Giọng nói AI 24kHz), Hub 2 (Phụ đề Tiếng Anh + Sửa ngữ pháp Realtime), và Hub 3 (Dịch Tiếng Việt tức thì).'
          )}
        </p>
      </div>
    );
  }

  return (
    <div className="flex-1 overflow-y-auto p-4 sm:p-6 space-y-4">
      {turns.map((turn) => {
        const isUser = turn.speaker === 'user';
        const isReplaying = replayingTurnId === turn.turnId;
        const turnCorrection = isUser
          ? corrections.find((c) => c.turnId === turn.turnId)
          : undefined;

        return (
          <div
            key={turn.turnId}
            className={cn('flex items-start gap-3', isUser ? 'flex-row-reverse' : 'flex-row')}
          >
            <div
              className={cn(
                'w-9 h-9 rounded-2xl flex items-center justify-center shrink-0 shadow-xs overflow-hidden',
                isUser
                  ? 'bg-emerald-600 text-white p-0.5'
                  : 'bg-emerald-50 text-emerald-800 border border-emerald-200 p-1.5'
              )}
            >
              {isUser ? (
                <img
                  src="/assets/mascot/cat/cat-avatar.svg"
                  alt="User"
                  className="w-full h-full object-contain"
                />
              ) : (
                <img
                  src="/assets/brand/logo-mark.svg"
                  alt="AI Teacher"
                  className="w-full h-full object-contain"
                />
              )}
            </div>

            <div className={cn('max-w-[82%] flex flex-col gap-1.5', isUser ? 'items-end' : 'items-start')}>
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
                  <div className="flex items-center gap-1 shrink-0">
                    {isUser && turn.englishLine1 && onAnalyzeTurn && (
                      <button
                        type="button"
                        onClick={() => onAnalyzeTurn(turn.turnId, turn.englishLine1)}
                        title={t('tutor.analyzeTurnTooltip', 'Phân tích ngữ pháp & từ vựng câu này')}
                        className="p-1 rounded-lg text-emerald-100 hover:bg-emerald-500 transition-colors cursor-pointer"
                      >
                        <Wand2 className="w-3.5 h-3.5" />
                      </button>
                    )}
                    {turn.englishLine1 && (
                      <button
                        type="button"
                        onClick={() => handleReplayTurn(turn)}
                        title={
                          isReplaying
                            ? t('tutor.stopReplay', 'Dừng phát lại audio')
                            : isUser
                              ? t('tutor.replayUserAudio', 'Nghe lại câu tiếng Anh (Giọng thu âm của bạn)')
                              : t('tutor.replayAiAudio', 'Nghe lại câu tiếng Anh (Giọng AI)')
                        }
                        className={cn(
                          'px-1.5 py-1 rounded-lg transition-colors cursor-pointer inline-flex items-center gap-1',
                          isUser
                            ? isReplaying
                              ? 'bg-white text-emerald-700 font-bold shadow-2xs'
                              : 'text-emerald-100 hover:bg-emerald-500'
                            : isReplaying
                              ? 'bg-amber-100 text-amber-800 font-bold border border-amber-300'
                              : 'text-slate-400 hover:bg-slate-100 hover:text-slate-700'
                        )}
                      >
                        {isReplaying ? (
                          <>
                            <Square className="w-3 h-3 fill-current animate-pulse" />
                            <span className="text-[10px]">{t('tutor.playing', 'Đang phát')}</span>
                          </>
                        ) : (
                          <Volume2 className="w-3.5 h-3.5" />
                        )}
                      </button>
                    )}
                  </div>
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
                  {turn.vietnameseLine2 || t('tutor.translating', 'Đang dịch sang tiếng Việt...')}
                </p>
              </div>

              {/* Compact 1-line Grammar Status Badge (full details sit in the Right Panel) */}
              {turnCorrection && (
                <button
                  type="button"
                  onClick={() => onSelectCorrection?.(turnCorrection)}
                  title={t('tutor.viewCorrectionHint', 'Xem giải thích chi tiết ở cột Sửa lỗi Ngữ pháp & Từ vựng bên phải')}
                  className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-amber-50 hover:bg-amber-100 border border-amber-200/90 text-amber-900 text-[11px] font-semibold transition-colors cursor-pointer max-w-full"
                >
                  <CheckCircle2 className="w-3.5 h-3.5 text-amber-600 shrink-0" />
                  <span className="truncate">
                    {turnCorrection.errorCategory}: &ldquo;{turnCorrection.correctedSentence}&rdquo;
                  </span>
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
