import React from 'react';
import type { FallingWordSessionDto } from '../types/fallingWords.types';

interface SessionSummaryModalProps {
  session: FallingWordSessionDto;
  onPlayAgain: () => void;
  onGoToHistory: () => void;
  onGoToSetup: () => void;
}

export const SessionSummaryModal: React.FC<SessionSummaryModalProps> = ({
  session,
  onPlayAgain,
  onGoToHistory,
  onGoToSetup,
}) => {
  const accuracyText =
    session.accuracy != null ? `${session.accuracy.toFixed(1)}%` : '—';

  return (
    <div
      className="fixed inset-0 bg-slate-900/70 backdrop-blur-md flex items-center justify-center p-4 z-50 overflow-y-auto animate-in fade-in duration-300"
      role="dialog"
      aria-modal="true"
    >
      <div className="bg-white rounded-3xl max-w-2xl w-full p-6 sm:p-8 shadow-2xl border border-slate-200 flex flex-col gap-6 my-auto">
        {/* Title */}
        <div className="text-center">
          <div className="text-4xl mb-2">🏆</div>
          <h2 className="text-2xl sm:text-3xl font-extrabold text-slate-800">
            {session.status === 'Completed'
              ? 'Hoàn thành ván chơi!'
              : 'Ván chơi đã dừng'}
          </h2>
          <p className="text-slate-500 text-sm mt-1">
            Nguồn: {session.source === 'DailyVocabulary' ? 'Daily Vocabulary hôm nay' : 'Kho từ vựng'}
            {session.cefrLevel ? ` • Trình độ ${session.cefrLevel}` : ''} • {session.actualCount} từ
          </p>
        </div>

        {/* Stats Grid */}
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 text-center">
          <div className="bg-emerald-50 border border-emerald-200/60 rounded-2xl p-4">
            <div className="text-2xl sm:text-3xl font-extrabold text-emerald-700">
              {accuracyText}
            </div>
            <div className="text-xs font-semibold text-emerald-900 mt-1 uppercase tracking-wider">
              Độ chính xác
            </div>
          </div>

          <div className="bg-emerald-50/60 border border-emerald-200/50 rounded-2xl p-4">
            <div className="text-2xl sm:text-3xl font-extrabold text-emerald-600">
              {session.correctCount}
            </div>
            <div className="text-xs font-semibold text-slate-600 mt-1 uppercase tracking-wider">
              Đúng
            </div>
          </div>

          <div className="bg-rose-50/60 border border-rose-200/50 rounded-2xl p-4">
            <div className="text-2xl sm:text-3xl font-extrabold text-rose-600">
              {session.incorrectCount + session.timedOutCount}
            </div>
            <div className="text-xs font-semibold text-slate-600 mt-1 uppercase tracking-wider">
              Sai / Hết giờ
            </div>
          </div>

          <div className="bg-amber-50/60 border border-amber-200/50 rounded-2xl p-4">
            <div className="text-2xl sm:text-3xl font-extrabold text-amber-600">
              {session.bestStreak}
            </div>
            <div className="text-xs font-semibold text-slate-600 mt-1 uppercase tracking-wider">
              Chuỗi đúng dài nhất
            </div>
          </div>
        </div>

        {/* Word Results Review List */}
        <div className="flex flex-col gap-2 max-h-60 overflow-y-auto pr-1">
          <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400 px-1">
            Chi tiết các từ trong ván
          </h3>
          {session.completedRounds.map((round) => {
            const isCorrect = round.status === 'Correct';
            const isTimed = round.status === 'TimedOut';
            return (
              <div
                key={round.roundId}
                className="flex items-center justify-between p-3 rounded-xl bg-slate-50 border border-slate-200/80 text-sm"
              >
                <div className="flex flex-col">
                  <div className="flex items-center gap-2">
                    <span className="font-bold text-slate-800 font-mono text-base">
                      {round.expectedWord}
                    </span>
                    {round.cefrLevel && (
                      <span className="text-[10px] px-1.5 py-0.5 rounded bg-slate-200 font-medium text-slate-700">
                        {round.cefrLevel}
                      </span>
                    )}
                  </div>
                  <span className="text-xs text-slate-500 mt-0.5 line-clamp-1">
                    {round.meaningVi}
                  </span>
                </div>

                <div className="flex items-center gap-2">
                  <span
                    className={`px-2.5 py-1 rounded-lg text-xs font-bold ${
                      isCorrect
                        ? 'bg-emerald-100 text-emerald-800'
                        : isTimed
                        ? 'bg-amber-100 text-amber-800'
                        : 'bg-rose-100 text-rose-800'
                    }`}
                  >
                    {isCorrect ? 'Đúng' : isTimed ? 'Hết giờ' : 'Sai'}
                  </span>
                </div>
              </div>
            );
          })}
        </div>

        {/* Action Buttons */}
        <div className="flex flex-col sm:flex-row items-center gap-3 pt-2">
          <button
            type="button"
            onClick={onPlayAgain}
            className="w-full sm:flex-1 py-3 px-4 rounded-xl bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white font-bold shadow-md transition text-center"
          >
            Chơi ván mới
          </button>
          <button
            type="button"
            onClick={onGoToHistory}
            className="w-full sm:flex-1 py-3 px-4 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-700 font-semibold transition text-center"
          >
            Xem lịch sử
          </button>
          <button
            type="button"
            onClick={onGoToSetup}
            className="w-full sm:w-auto py-3 px-4 rounded-xl text-slate-500 hover:text-slate-800 font-medium transition text-center"
          >
            Về thiết lập
          </button>
        </div>
      </div>
    </div>
  );
};
