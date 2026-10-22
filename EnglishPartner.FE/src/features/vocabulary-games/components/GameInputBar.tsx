import React, { useEffect, useRef } from 'react';

interface GameInputBarProps {
  value: string;
  onChange: (val: string) => void;
  onSubmit: () => void;
  disabled: boolean;
  isSubmitting: boolean;
}

export const GameInputBar: React.FC<GameInputBarProps> = ({
  value,
  onChange,
  onSubmit,
  disabled,
  isSubmitting,
}) => {
  const inputRef = useRef<HTMLInputElement>(null);
  const isComposingRef = useRef(false);

  useEffect(() => {
    if (!disabled && inputRef.current) {
      inputRef.current.focus();
    }
  }, [disabled]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (isComposingRef.current) return;
    if (!disabled && !isSubmitting && value.trim()) {
      onSubmit();
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      if (isComposingRef.current || e.nativeEvent.isComposing) {
        return; // Ignore Enter during IME composition (Vietnamese Telex, etc.)
      }
      e.preventDefault();
      if (!disabled && !isSubmitting && value.trim()) {
        onSubmit();
      }
    }
  };

  return (
    <form
      role="search"
      autoComplete="off"
      onSubmit={handleSubmit}
      className="w-full bg-white/95 backdrop-blur-md border border-slate-200 rounded-2xl p-3 sm:p-4 shadow-lg flex items-center gap-3 z-30"
    >
      <div className="relative flex-1">
        <input
          ref={inputRef}
          type="text"
          role="searchbox"
          id="vocabulary-game-word-query"
          name="vocabularyGameWordQuery"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          onKeyDown={handleKeyDown}
          onCompositionStart={() => {
            isComposingRef.current = true;
          }}
          onCompositionEnd={() => {
            isComposingRef.current = false;
          }}
          disabled={disabled || isSubmitting}
          placeholder={disabled ? 'Đang tải...' : 'Nhập từ tiếng Anh chính xác rồi nhấn Enter...'}
          autoComplete="off"
          autoCorrect="off"
          autoCapitalize="none"
          spellCheck="false"
          data-form-type="other"
          data-lpignore="true"
          data-1p-ignore="true"
          aria-autocomplete="none"
          className="w-full px-4 py-3 bg-slate-50 border border-slate-300 rounded-xl text-base sm:text-lg font-medium text-slate-800 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500 disabled:opacity-60 transition"
        />
      </div>

      <button
        type="submit"
        disabled={disabled || isSubmitting || !value.trim()}
        className="px-5 py-3 rounded-xl bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white font-semibold shadow-md disabled:opacity-50 disabled:cursor-not-allowed transition flex items-center gap-2 whitespace-nowrap"
      >
        {isSubmitting ? (
          <>
            <svg
              className="animate-spin h-5 w-5 text-white"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
            >
              <circle
                className="opacity-25"
                cx="12"
                cy="12"
                r="10"
                stroke="currentColor"
                strokeWidth="4"
              />
              <path
                className="opacity-75"
                fill="currentColor"
                d="M4 12a8 8 0 018-8v8H4z"
              />
            </svg>
            <span>Đang nộp...</span>
          </>
        ) : (
          <>
            <span>Nộp</span>
            <kbd className="hidden sm:inline-block px-1.5 py-0.5 text-xs bg-emerald-700 rounded border border-emerald-500/50">
              Enter
            </kbd>
          </>
        )}
      </button>
    </form>
  );
};
