import React from 'react';
import type { ActiveFallingWordRoundDto } from '../types/fallingWords.types';

export interface LiveSlot {
  char: string;
  isFilled: boolean;
  isSeparator: boolean;
}

export function computeLiveSlots(maskedPattern: string, input: string): LiveSlot[] {
  if (!maskedPattern) return [];

  // In backend: foreach char in word.Trim(): token = IsLetterOrDigit ? "_" : c.ToString()
  // string.Join(" ", tokens) -> tokens are at even indices (0, 2, 4, 6...)
  const tokens: string[] = [];
  for (let i = 0; i < maskedPattern.length; i += 2) {
    tokens.push(maskedPattern[i]);
  }

  const cleanInput = input || '';
  let inputPtr = 0;

  return tokens.map((token) => {
    if (token !== '_') {
      // Fixed character such as space, hyphen, apostrophe
      if (inputPtr < cleanInput.length && cleanInput[inputPtr] === token) {
        inputPtr++;
      }
      return {
        char: token,
        isFilled: false,
        isSeparator: true,
      };
    }

    // Underscore letter blank slot
    if (inputPtr < cleanInput.length) {
      const typedChar = cleanInput[inputPtr++];
      return {
        char: typedChar,
        isFilled: true,
        isSeparator: false,
      };
    }

    return {
      char: '_',
      isFilled: false,
      isSeparator: false,
    };
  });
}

export function getPatternTypographyClass(letterCount: number): string {
  if (letterCount <= 6) {
    return 'text-2xl sm:text-3xl tracking-widest';
  }
  if (letterCount <= 10) {
    return 'text-xl sm:text-2xl tracking-wider';
  }
  if (letterCount <= 14) {
    return 'text-base sm:text-xl tracking-wide';
  }
  return 'text-sm sm:text-base tracking-normal';
}

interface FallingWordCardProps {
  round: ActiveFallingWordRoundDto;
  progressRatio: number; // 0.0 to 1.0
  isReducedMotion: boolean;
  currentInput?: string;
}

export const FallingWordCard: React.FC<FallingWordCardProps> = ({
  round,
  progressRatio,
  isReducedMotion,
  currentInput,
}) => {
  // If reduced motion, card stays at top-center and relies on the timer bar
  // If animated, translates translateY from 0% to ~85% of play area
  const verticalPercentage = isReducedMotion ? 10 : Math.min(progressRatio * 82, 82);

  const hasInput = Boolean(currentInput && currentInput.length > 0);
  const liveSlots = computeLiveSlots(round.maskedPattern, currentInput || '');

  const effectiveLength = Math.max(
    round.letterCount || 0,
    Math.round((round.maskedPattern?.length || 0) / 2)
  );
  const typographyClass = getPatternTypographyClass(effectiveLength);

  return (
    <div
      className="absolute left-1/2 -translate-x-1/2 w-11/12 max-w-lg sm:max-w-xl transition-transform duration-75 ease-linear pointer-events-none select-none z-10"
      style={{
        top: `${verticalPercentage}%`,
      }}
      role="region"
      aria-label="Thẻ từ đang rơi"
    >
      <div className="bg-white/95 backdrop-blur-md rounded-2xl shadow-xl border-2 border-emerald-500/30 p-5 text-center transition-all">
        {/* CEFR Level & Letter Count Badges */}
        <div className="flex items-center justify-between gap-2 mb-3">
          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-100 text-emerald-800">
            {round.cefrLevel || 'General'}
          </span>
          <span className="text-xs font-medium text-slate-500">
            {round.letterCount} chữ cái
          </span>
        </div>

        {/* Masked Pattern / Live Filled Slots */}
        <div
          data-testid="falling-word-masked-pattern"
          className={`font-mono font-bold text-slate-800 my-2 select-all py-1.5 px-3 bg-slate-50 rounded-xl border border-slate-200/60 inline-flex flex-wrap items-center justify-center max-w-full break-words whitespace-pre-wrap text-center ${typographyClass}`}
        >
          {!hasInput ? (
            round.maskedPattern
          ) : (
            liveSlots.map((slot, idx) => (
              <span
                key={idx}
                className={
                  slot.isFilled
                    ? 'text-emerald-600 font-extrabold underline decoration-emerald-500 decoration-2 underline-offset-4'
                    : slot.isSeparator
                    ? 'text-slate-500 font-bold'
                    : 'text-slate-400'
                }
              >
                {slot.char}
                {idx < liveSlots.length - 1 ? ' ' : ''}
              </span>
            ))
          )}
        </div>

        {/* Vietnamese Meaning (wraps properly for long definitions) */}
        <p className="text-base sm:text-lg font-medium text-emerald-950 mt-2 break-words leading-relaxed">
          {round.meaningVi}
        </p>

        {isReducedMotion && (
          <div className="mt-3 text-xs text-slate-400">
            Chế độ giảm chuyển động: thẻ cố định vị trí, theo dõi thanh thời gian bên trên.
          </div>
        )}
      </div>
    </div>
  );
};
