import React, { useState } from 'react';
import { Volume2, Check, HelpCircle } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { LessonAudioButton } from './LessonAudioButton';

interface SpellingPracticePlayerProps {
  content: {
    prompt?: string;
    definition?: string;
    hint?: string;
    audioUrl?: string;
  };
  onSubmit: (payload: { spelledWord: string }) => void;
  isSubmitting?: boolean;
  isSubmitted?: boolean;
  initialSpelledWord?: string;
}

export const SpellingPracticePlayer: React.FC<SpellingPracticePlayerProps> = ({
  content,
  onSubmit,
  isSubmitting = false,
  isSubmitted = false,
  initialSpelledWord = '',
}) => {
  const [word, setWord] = useState(initialSpelledWord);
  const [showHint, setShowHint] = useState(false);

  const playAudio = () => {
    if (!content.audioUrl) return;
    const audio = new Audio(content.audioUrl);
    void audio.play().catch(() => {});
  };

  return (
    <div className="space-y-6 max-w-xl mx-auto">
      {/* Prompt / Cue */}
      <div className="bg-slate-50 p-6 rounded-3xl border border-slate-100 text-center space-y-4 shadow-xs">
        <span className="text-xs uppercase font-extrabold tracking-wider text-slate-400">
          Luyện nghe và gõ chính tả từ vựng
        </span>

        {content.prompt && (
          <div className="text-lg font-bold text-slate-900">{content.prompt}</div>
        )}

        {content.definition && (
          <p className="text-sm text-slate-600 italic">&ldquo;{content.definition}&rdquo;</p>
        )}

        {content.audioUrl && (
          <div>
            <Button
              type="button"
              variant="outline"
              size="md"
              onClick={playAudio}
              className="rounded-full gap-2 text-emerald-700 hover:bg-emerald-50 border-emerald-200"
            >
              <Volume2 className="w-5 h-5" /> Nghe phát âm
            </Button>
          </div>
        )}
        {!content.audioUrl && <LessonAudioButton label="Nghe phát âm từ cần viết" />}

        {content.hint && (
          <div className="pt-2">
            {!showHint ? (
              <button
                type="button"
                onClick={() => setShowHint(true)}
                className="text-xs text-sky-600 hover:underline inline-flex items-center gap-1 cursor-pointer"
              >
                <HelpCircle className="w-3.5 h-3.5" /> Xem gợi ý
              </button>
            ) : (
              <div className="text-xs text-sky-800 bg-sky-50 px-3 py-1.5 rounded-xl inline-block border border-sky-100">
                Gợi ý: {content.hint}
              </div>
            )}
          </div>
        )}
      </div>

      {/* Input box */}
      <div className="space-y-2">
        <label className="text-xs font-bold text-slate-700 block">
          Nhập từ vựng tiếng Anh chính xác:
        </label>
        <input
          type="text"
          placeholder="Nhập từ chính tả..."
          value={word}
          onChange={(e) => setWord(e.target.value)}
          disabled={isSubmitted}
          autoComplete="off"
          autoCorrect="off"
          spellCheck={false}
          className="w-full text-center text-xl font-bold tracking-wide rounded-2xl border border-slate-200 bg-white p-4 focus:outline-none focus:ring-2 focus:ring-emerald-500 shadow-xs"
        />
      </div>

      {/* Submit Action */}
      {!isSubmitted && (
        <div className="pt-2">
          <Button
            type="button"
            variant="primary"
            onClick={() => onSubmit({ spelledWord: word.trim() })}
            disabled={word.trim().length === 0 || isSubmitting}
            className="w-full gap-2 py-3 rounded-2xl"
          >
            <Check className="w-4 h-4" />
            {isSubmitting ? 'Đang chấm điểm...' : 'Xác nhận nộp từ'}
          </Button>
        </div>
      )}
    </div>
  );
};
