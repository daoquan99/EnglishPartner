import React, { useState } from 'react';
import { Volume2, RotateCw, ChevronLeft, ChevronRight, Check } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { cn } from '@/shared/lib/cn';
import { LessonAudioButton } from './LessonAudioButton';

interface CardItem {
  id: string;
  term: string;
  ipa?: string;
  meaningVi: string;
  exampleEn?: string;
  exampleVi?: string;
  audioUrl?: string;
}

interface FlashcardPlayerProps {
  content: {
    cards?: CardItem[];
  };
  onSubmit: (payload: { ratings: Record<string, number> }) => void;
  isSubmitting?: boolean;
  isSubmitted?: boolean;
}

export const FlashcardPlayer: React.FC<FlashcardPlayerProps> = ({
  content,
  onSubmit,
  isSubmitting = false,
  isSubmitted = false,
}) => {
  const cards = content?.cards ?? [];
  const [currentIndex, setCurrentIndex] = useState(0);
  const [isFlipped, setIsFlipped] = useState(false);
  const [ratings, setRatings] = useState<Record<string, number>>({});

  if (cards.length === 0) {
    return <div className="text-slate-500 text-sm italic">Không có thẻ flashcard nào trong bài.</div>;
  }

  const currentCard = cards[currentIndex];
  const ratedCount = Object.keys(ratings).length;
  const isAllRated = ratedCount === cards.length;

  const handleRate = (level: number) => {
    setRatings((prev) => ({ ...prev, [currentCard.id]: level }));
    setIsFlipped(false);
    if (currentIndex < cards.length - 1) {
      setCurrentIndex((i) => i + 1);
    }
  };

  const handleNext = () => {
    setIsFlipped(false);
    if (currentIndex < cards.length - 1) setCurrentIndex((i) => i + 1);
  };

  const handlePrev = () => {
    setIsFlipped(false);
    if (currentIndex > 0) setCurrentIndex((i) => i - 1);
  };

  const playAudio = (url?: string) => {
    if (!url) return;
    const audio = new Audio(url);
    void audio.play().catch(() => {});
  };

  return (
    <div className="space-y-6 max-w-xl mx-auto">
      {/* Deck Progress Bar */}
      <div className="flex items-center justify-between text-xs text-slate-600 font-medium">
        <span>Thẻ {currentIndex + 1} / {cards.length}</span>
        <Badge variant={isAllRated ? 'emerald' : 'default'}>
          Đã đánh giá: {ratedCount}/{cards.length}
        </Badge>
      </div>

      {/* Card Body */}
      <LessonAudioButton label="Nghe phát âm thẻ hiện tại" cardId={currentCard.id} />
      <div
        role="button"
        tabIndex={0}
        onClick={() => setIsFlipped(!isFlipped)}
        onKeyDown={(e) => {
          if (e.key === ' ' || e.key === 'Enter') {
            e.preventDefault();
            setIsFlipped(!isFlipped);
          }
        }}
        className={cn(
          'w-full min-h-[260px] p-8 rounded-3xl border text-center transition-all duration-300 cursor-pointer flex flex-col justify-center items-center shadow-sm select-none',
          isFlipped
            ? 'bg-emerald-50/40 border-emerald-200'
            : 'bg-white border-slate-200 hover:border-slate-300'
        )}
      >
        {!isFlipped ? (
          // Front Side
          <div className="space-y-4">
            <span className="text-xs uppercase font-extrabold tracking-wider text-slate-400">
              Mặt trước • Bấm để lật xem nghĩa
            </span>
            <div className="text-3xl sm:text-4xl font-extrabold text-slate-900 tracking-tight">
              {currentCard.term}
            </div>
            {currentCard.ipa && (
              <div className="text-sm font-mono text-emerald-700 bg-emerald-50 px-3 py-1 rounded-full inline-block">
                /{currentCard.ipa}/
              </div>
            )}
            {currentCard.audioUrl && (
              <div>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={(e) => {
                    e.stopPropagation();
                    playAudio(currentCard.audioUrl);
                  }}
                  className="rounded-full gap-1.5 text-xs text-emerald-700"
                >
                  <Volume2 className="w-4 h-4" /> Nghe phát âm
                </Button>
              </div>
            )}
          </div>
        ) : (
          // Back Side
          <div className="space-y-4">
            <span className="text-xs uppercase font-extrabold tracking-wider text-emerald-600">
              Mặt sau • Định nghĩa & Ví dụ
            </span>
            <div className="text-2xl sm:text-3xl font-bold text-slate-900">
              {currentCard.meaningVi}
            </div>
            {currentCard.exampleEn && (
              <div className="p-3 bg-white/90 border border-emerald-100 rounded-xl text-left text-xs space-y-1">
                <p className="font-semibold text-slate-800">&ldquo;{currentCard.exampleEn}&rdquo;</p>
                {currentCard.exampleVi && (
                  <p className="text-slate-500 italic">{currentCard.exampleVi}</p>
                )}
              </div>
            )}
          </div>
        )}
      </div>

      {/* Navigation & Flip Hint */}
      <div className="flex items-center justify-between">
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={handlePrev}
          disabled={currentIndex === 0}
          className="gap-1"
        >
          <ChevronLeft className="w-4 h-4" /> Trước
        </Button>

        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => setIsFlipped(!isFlipped)}
          className="gap-1.5 text-xs"
        >
          <RotateCw className="w-3.5 h-3.5" /> Lật thẻ
        </Button>

        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={handleNext}
          disabled={currentIndex === cards.length - 1}
          className="gap-1"
        >
          Sau <ChevronRight className="w-4 h-4" />
        </Button>
      </div>

      {/* Self-Rating Buttons */}
      <div className="space-y-2 pt-2 border-t border-slate-100">
        <span className="text-xs font-bold text-slate-700 block text-center">
          Tự đánh giá mức độ ghi nhớ cho thẻ này:
        </span>
        <div className="grid grid-cols-4 gap-2">
          {[
            { level: 1, label: 'Chưa nhớ', color: 'hover:bg-rose-50 hover:border-rose-300' },
            { level: 2, label: 'Mơ hồ', color: 'hover:bg-amber-50 hover:border-amber-300' },
            { level: 3, label: 'Gần nhớ', color: 'hover:bg-sky-50 hover:border-sky-300' },
            { level: 4, label: 'Đã nhớ', color: 'hover:bg-emerald-50 hover:border-emerald-400' },
          ].map((item) => {
            const isSelected = ratings[currentCard.id] === item.level;
            return (
              <button
                key={item.level}
                type="button"
                onClick={() => handleRate(item.level)}
                disabled={isSubmitted}
                className={cn(
                  'p-2.5 rounded-xl border text-center transition-all text-xs font-semibold cursor-pointer',
                  isSelected
                    ? 'bg-emerald-600 text-white border-emerald-600 shadow-xs'
                    : `bg-white text-slate-700 border-slate-200 ${item.color}`
                )}
              >
                <div>Mức {item.level}</div>
                <div className="text-[10px] opacity-80">{item.label}</div>
              </button>
            );
          })}
        </div>
      </div>

      {/* Final Submit Deck */}
      {!isSubmitted && (
        <div className="pt-2">
          <Button
            type="button"
            variant="primary"
            onClick={() => onSubmit({ ratings })}
            disabled={!isAllRated || isSubmitting}
            className="w-full gap-2 py-3 rounded-2xl"
          >
            <Check className="w-4 h-4" />
            {isSubmitting ? 'Đang lưu...' : isAllRated ? 'Hoàn thành bài Flashcards' : `Đánh giá đủ ${cards.length} thẻ để nộp`}
          </Button>
        </div>
      )}
    </div>
  );
};
