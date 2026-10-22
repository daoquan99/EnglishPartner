import React, { useEffect, useRef, useState } from 'react';
import type { ActiveFallingWordRoundDto } from '../types/fallingWords.types';
import { FallingWordCard } from './FallingWordCard';

interface FallingGameCanvasProps {
  activeRound: ActiveFallingWordRoundDto | null;
  currentInput?: string;
  onDeadlineReached: () => void;
  streak: number;
}

export const FallingGameCanvas: React.FC<FallingGameCanvasProps> = ({
  activeRound,
  currentInput,
  onDeadlineReached,
  streak,
}) => {
  const [ratio, setRatio] = useState(0);
  const [remainingSec, setRemainingSec] = useState<number>(activeRound?.timeLimitSeconds ?? 5);
  const [isReducedMotion, setIsReducedMotion] = useState(false);
  const deadlineCalledRef = useRef(false);
  const onDeadlineReachedRef = useRef(onDeadlineReached);

  useEffect(() => {
    onDeadlineReachedRef.current = onDeadlineReached;
  }, [onDeadlineReached]);

  useEffect(() => {
    const mediaQuery = window.matchMedia('(prefers-reduced-motion: reduce)');
    setIsReducedMotion(mediaQuery.matches);
    const handler = (e: MediaQueryListEvent) => setIsReducedMotion(e.matches);
    mediaQuery.addEventListener('change', handler);
    return () => mediaQuery.removeEventListener('change', handler);
  }, []);

  useEffect(() => {
    deadlineCalledRef.current = false;
    if (!activeRound || !activeRound.expiresAtUtc || !activeRound.startedAtUtc) {
      setRatio(0);
      setRemainingSec(0);
      return;
    }

    const startMs = new Date(activeRound.startedAtUtc).getTime();
    const expiryMs = new Date(activeRound.expiresAtUtc).getTime();
    const totalMs = Math.max(500, expiryMs - startMs);

    // Calculate approximate server offset once per active round:
    const serverNowMs = activeRound.serverNowUtc
      ? new Date(activeRound.serverNowUtc).getTime()
      : Date.now();
    const clientOffset = serverNowMs - Date.now();

    let animationFrameId: number;

    const tick = () => {
      const nowEstimated = Date.now() + clientOffset;
      const elapsed = Math.max(0, nowEstimated - startMs);
      const currentRatio = Math.min(1, elapsed / totalMs);
      const remainingMs = Math.max(0, expiryMs - nowEstimated);

      setRatio(currentRatio);
      setRemainingSec(Math.max(0, remainingMs / 1000));

      if (currentRatio >= 1 && !deadlineCalledRef.current) {
        deadlineCalledRef.current = true;
        onDeadlineReachedRef.current();
        return;
      }

      animationFrameId = requestAnimationFrame(tick);
    };

    animationFrameId = requestAnimationFrame(tick);

    return () => {
      cancelAnimationFrame(animationFrameId);
    };
  }, [activeRound?.roundId, activeRound?.expiresAtUtc, activeRound?.startedAtUtc]);

  const progressBarColor =
    ratio > 0.8
      ? 'bg-rose-500'
      : ratio > 0.5
      ? 'bg-amber-500'
      : 'bg-emerald-500';

  return (
    <div className="relative w-full flex-1 min-h-[380px] max-h-[580px] bg-gradient-to-b from-emerald-50/60 via-slate-50/80 to-emerald-100/50 rounded-2xl border border-emerald-200/60 shadow-inner overflow-hidden flex flex-col">
      {/* Top Header: Progress & Timer */}
      <div className="p-3 bg-white/80 backdrop-blur-sm border-b border-slate-200/60 flex items-center justify-between text-sm z-20">
        <div className="flex items-center gap-3">
          <span className="font-semibold text-slate-700">
            Lượt {activeRound?.roundOrder ?? 1} / {activeRound?.totalRounds ?? 10}
          </span>
          {streak > 1 && (
            <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-bold bg-amber-100 text-amber-800 border border-amber-300">
              🔥 Chuỗi {streak}
            </span>
          )}
        </div>

        <div className="flex items-center gap-2">
          <span className="text-xs text-slate-500 font-medium">Thời gian:</span>
          <span
            className={`font-mono font-bold text-base ${
              remainingSec <= 2 ? 'text-rose-600 animate-pulse' : 'text-slate-800'
            }`}
          >
            {remainingSec.toFixed(1)}s
          </span>
        </div>
      </div>

      {/* Top Countdown Bar (Shared Authoritative Ratio) */}
      <div className="w-full h-1.5 bg-slate-200 overflow-hidden z-20">
        <div
          className={`h-full transition-all duration-75 ease-linear ${progressBarColor}`}
          style={{ width: `${Math.max(0, 100 - ratio * 100)}%` }}
        />
      </div>

      {/* Falling Area */}
      <div className="relative flex-1 w-full overflow-hidden">
        {activeRound && (
          <FallingWordCard
            round={activeRound}
            currentInput={currentInput}
            progressRatio={ratio}
            isReducedMotion={isReducedMotion}
          />
        )}
      </div>
    </div>
  );
};
