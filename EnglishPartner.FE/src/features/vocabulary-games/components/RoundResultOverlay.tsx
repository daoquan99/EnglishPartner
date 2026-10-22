import React, { useEffect } from 'react';
import type { TerminalFallingWordRoundDto } from '../types/fallingWords.types';

interface RoundResultOverlayProps {
  result: TerminalFallingWordRoundDto;
  onNext: () => void;
  isLastRound: boolean;
  isLoadingNext: boolean;
  onPlayAudio?: () => void;
  isAudioPlaying?: boolean;
}

export const RoundResultOverlay: React.FC<RoundResultOverlayProps> = ({
  result,
  onNext,
  isLastRound,
  isLoadingNext,
  onPlayAudio,
  isAudioPlaying = false,
}) => {
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Enter' || e.key === ' ') {
        e.preventDefault();
        if (!isLoadingNext) {
          onNext();
        }
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [onNext, isLoadingNext]);

  const isCorrect = result.status === 'Correct';
  const isTimedOut = result.status === 'TimedOut';
  const isSkipped =
    result.failureReason === 'SkippedByLearner' || result.learnerAnswer === '[Bỏ qua]';

  const badgeConfig = isCorrect
    ? {
        bg: 'bg-emerald-50 border-emerald-500/40 text-emerald-800',
        title: 'Chính xác! 🎉',
        icon: '✓',
      }
    : isSkipped
    ? {
        bg: 'bg-amber-50 border-amber-500/40 text-amber-800',
        title: 'Đã bỏ qua! ⏭️',
        icon: '↷',
      }
    : isTimedOut
    ? {
        bg: 'bg-amber-50 border-amber-500/40 text-amber-800',
        title: 'Hết giờ! ⏰',
        icon: '⌛',
      }
    : {
        bg: 'bg-rose-50 border-rose-500/40 text-rose-800',
        title: 'Chưa chính xác! ❌',
        icon: '✕',
      };

  return (
    <div
      className="fixed inset-0 bg-slate-900/60 backdrop-blur-sm flex items-center justify-center p-4 z-50 animate-in fade-in duration-200"
      role="dialog"
      aria-modal="true"
    >
      <div className="bg-white rounded-3xl max-w-md w-full p-6 shadow-2xl border border-slate-200 text-center flex flex-col gap-5">
        {/* Outcome Header Badge */}
        <div
          className={`inline-flex items-center justify-center gap-2 px-4 py-2 rounded-2xl border font-bold text-lg mx-auto ${badgeConfig.bg}`}
        >
          <span className="text-xl">{badgeConfig.icon}</span>
          <span>{badgeConfig.title}</span>
        </div>

        {/* Expected Word Details */}
        <div className="py-2">
          <div className="text-xs uppercase tracking-wider text-slate-400 font-semibold mb-1">
            Đáp án đúng
          </div>
          <div className="flex items-center justify-center gap-2">
            <span className="text-3xl font-extrabold text-slate-800 tracking-wide font-mono">
              {result.expectedWord}
            </span>
            {onPlayAudio && (
              <button
                type="button"
                onClick={onPlayAudio}
                className="p-2 rounded-xl text-emerald-700 hover:bg-emerald-50 active:scale-95 border border-emerald-200/60 shadow-sm transition"
                title="Nghe phát âm từ này"
              >
                {isAudioPlaying ? '🔊...' : '🔊'}
              </button>
            )}
          </div>
          {result.cefrLevel && (
            <span className="inline-block mt-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-100 text-emerald-800">
              {result.cefrLevel}
            </span>
          )}
          <p className="text-base text-emerald-950 font-medium mt-3 bg-emerald-50/60 p-3 rounded-xl border border-emerald-200/50">
            {result.meaningVi}
          </p>
        </div>

        {/* Learner Submission Info */}
        {!isCorrect && (
          <div className="bg-slate-50 p-3 rounded-xl border border-slate-200 text-sm text-left">
            <div className="flex justify-between items-center text-slate-600">
              <span className="text-slate-400">Bạn đã nhập:</span>
              <span
                className={`font-semibold font-mono ${
                  isSkipped ? 'text-amber-700' : 'text-rose-700'
                }`}
              >
                {isSkipped ? '(Đã bỏ qua từ này)' : result.learnerAnswer || '(Không có)'}
              </span>
            </div>
            {result.elapsedMs != null && (
              <div className="flex justify-between items-center text-slate-500 text-xs mt-1">
                <span>Thời gian phản hồi:</span>
                <span>{(result.elapsedMs / 1000).toFixed(2)}s</span>
              </div>
            )}
          </div>
        )}

        {/* Action Button */}
        <button
          type="button"
          onClick={onNext}
          disabled={isLoadingNext}
          autoFocus
          className="w-full py-3.5 px-6 rounded-2xl bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white font-bold text-base shadow-lg shadow-emerald-600/25 transition flex items-center justify-center gap-2"
        >
          {isLoadingNext ? (
            <span>Đang chuẩn bị lượt tiếp theo...</span>
          ) : isLastRound ? (
            <>
              <span>Xem tổng kết ván chơi</span>
              <kbd className="px-2 py-0.5 text-xs bg-emerald-700 rounded border border-emerald-500/50">
                Enter
              </kbd>
            </>
          ) : (
            <>
              <span>Tiếp theo</span>
              <kbd className="px-2 py-0.5 text-xs bg-emerald-700 rounded border border-emerald-500/50">
                Enter
              </kbd>
            </>
          )}
        </button>
      </div>
    </div>
  );
};
