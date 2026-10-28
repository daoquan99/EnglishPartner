import React from 'react';
import { useTranslation } from 'react-i18next';
import {
  Check,
  ChevronLeft,
  ChevronRight,
  Loader2,
  RotateCw,
  Volume2,
} from 'lucide-react';
import { Badge } from '@/shared/components/ui/Badge';
import { Button } from '@/shared/components/ui/Button';
import { cn } from '@/shared/lib/cn';
import type { ReviewRating, VocabularyFlashcardDto } from '../types/vocabulary.types';

export interface VocabularyFlashcardProps {
  currentCard: VocabularyFlashcardDto;
  currentIndex: number;
  totalCards: number;
  isFlipped: boolean;
  onToggleFlip: () => void;
  onFlipToBack: () => void;
  onPrevCard: () => void;
  onNextCard: () => void;
  onSynthesizeTeacherSpeech: (
    textToSpeak: string,
    kind: 'word' | 'sentence',
    overrideAccent?: 'US' | 'UK',
    targetId?: string,
    vocabularyId?: string,
    senseId?: string
  ) => Promise<void>;
  isSynthesizing: boolean;
  ttsVoiceName: string;
  ttsModelCode: string;
  ttsAccent: 'US' | 'UK';
  onReview: (rating: ReviewRating) => Promise<void>;
}

export const VocabularyFlashcard: React.FC<VocabularyFlashcardProps> = React.memo(
  ({
    currentCard,
    currentIndex,
    totalCards,
    isFlipped,
    onToggleFlip,
    onFlipToBack,
    onPrevCard,
    onNextCard,
    onSynthesizeTeacherSpeech,
    isSynthesizing,
    ttsVoiceName,
    ttsModelCode,
    ttsAccent,
    onReview,
  }) => {
    const { t } = useTranslation();

    return (
      <div className="bg-white rounded-3xl border border-slate-200/90 shadow-xs p-6 space-y-5">
        {/* Card Navigation & Flip Bar */}
        <div className="flex items-center justify-between border-b border-slate-100 pb-3">
          <div className="flex items-center gap-2 flex-wrap">
            <Badge variant="emerald">{currentCard.cefrLevel}</Badge>
            {currentCard.itemType === 'CarryOver' ? (
              <Badge variant="amber">{t('vocab.carryOverBadge')}</Badge>
            ) : currentCard.itemType === 'Review' ? (
              <Badge variant="sky">{t('vocab.dueReviewBadge')}</Badge>
            ) : (
              <Badge variant="emerald">{t('vocab.newBadge')}</Badge>
            )}
            <Badge variant="amber">{currentCard.partOfSpeech}</Badge>
            {currentCard.isCompletedToday && (
              <Badge variant="emerald">
                <Check className="w-3 h-3 mr-1 inline" />
                {t('vocab.completedTodayBadge')}
              </Badge>
            )}
            {currentCard.topicCategory && (
              <span className="text-xs text-slate-500 truncate max-w-xs">
                {currentCard.topicCategory}
              </span>
            )}
          </div>

          <div className="flex items-center gap-1.5">
            <Button
              variant="outline"
              size="sm"
              disabled={currentIndex === 0}
              onClick={onPrevCard}
            >
              <ChevronLeft className="w-4 h-4" />
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={onToggleFlip}
            >
              <RotateCw className="w-3.5 h-3.5" />
              {isFlipped ? t('vocab.cardFront') : t('vocab.cardBack')}
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={currentIndex >= totalCards - 1}
              onClick={onNextCard}
            >
              <ChevronRight className="w-4 h-4" />
            </Button>
          </div>
        </div>

        {/* Flashcard Body */}
        {!isFlipped ? (
          <div className="py-6 text-center space-y-4">
            <h2 className="text-4xl font-extrabold text-slate-900 tracking-tight">
              {currentCard.word}
            </h2>

            {/* UK / US IPA & Gemini 3.8 Live Teacher Voice TTS Buttons */}
            <div className="flex items-center justify-center gap-3 flex-wrap">
              {currentCard.pronunciations.map((p) => {
                const acc = p.accent === 'UK' ? 'UK' : 'US';
                return (
                  <button
                    key={p.id}
                    type="button"
                    disabled={isSynthesizing}
                    onClick={() =>
                      void onSynthesizeTeacherSpeech(
                        currentCard.word,
                        'word',
                        acc,
                        currentCard.vocabularyId,
                        currentCard.vocabularyId
                      )
                    }
                    title={`Phát âm từ "${currentCard.word}" theo ngữ điệu ${acc} bằng giọng Teacher ${ttsVoiceName} (${ttsModelCode})`}
                    className="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl bg-slate-50 hover:bg-emerald-50 border border-slate-200 hover:border-emerald-300 text-xs font-semibold text-slate-700 transition-all cursor-pointer disabled:opacity-60"
                  >
                    <span className="font-bold text-emerald-700">{acc}</span>
                    <span className="font-mono">{p.ipa}</span>
                    {isSynthesizing && ttsAccent === acc ? (
                      <Loader2 className="w-3.5 h-3.5 text-emerald-600 animate-spin" />
                    ) : (
                      <Volume2 className="w-3.5 h-3.5 text-emerald-600" />
                    )}
                    <span className="text-[10px] text-slate-400">({ttsVoiceName})</span>
                  </button>
                );
              })}
            </div>

            <p className="text-base font-medium text-slate-700 max-w-xl mx-auto">
              {currentCard.definitionEn}
            </p>

            {currentCard.examples[0] && (
              <div className="max-w-xl mx-auto p-4 rounded-2xl bg-emerald-50/60 border border-emerald-200/80 text-left space-y-2">
                <div className="flex items-start justify-between gap-3">
                  <p className="text-sm font-semibold text-emerald-950">
                    &ldquo;{currentCard.examples[0].sentenceEn}&rdquo;
                  </p>
                  <div className="flex items-center gap-1 shrink-0">
                    <button
                      type="button"
                      disabled={isSynthesizing}
                      onClick={() =>
                        void onSynthesizeTeacherSpeech(
                          currentCard.examples[0].sentenceEn,
                          'sentence',
                          'US',
                          currentCard.examples[0].id,
                          currentCard.vocabularyId,
                          currentCard.senseId
                        )
                      }
                      title={`Nghe câu mẫu với giọng Teacher ${ttsVoiceName} (US Accent)`}
                      className="px-2 py-1 text-[11px] font-bold text-emerald-800 bg-white hover:bg-emerald-100 border border-emerald-200 rounded-lg inline-flex items-center gap-1 cursor-pointer"
                    >
                      <Volume2 className="w-3.5 h-3.5" /> US
                    </button>
                    <button
                      type="button"
                      disabled={isSynthesizing}
                      onClick={() =>
                        void onSynthesizeTeacherSpeech(
                          currentCard.examples[0].sentenceEn,
                          'sentence',
                          'UK',
                          currentCard.examples[0].id,
                          currentCard.vocabularyId,
                          currentCard.senseId
                        )
                      }
                      title={`Nghe câu mẫu với giọng Teacher ${ttsVoiceName} (UK Accent)`}
                      className="px-2 py-1 text-[11px] font-bold text-sky-800 bg-white hover:bg-sky-100 border border-sky-200 rounded-lg inline-flex items-center gap-1 cursor-pointer"
                    >
                      <Volume2 className="w-3.5 h-3.5" /> UK
                    </button>
                  </div>
                </div>
              </div>
            )}

            <button
              type="button"
              onClick={onFlipToBack}
              className="text-xs font-bold text-emerald-700 hover:underline cursor-pointer"
            >
              {t('vocab.clickToSeeBack')}
            </button>
          </div>
        ) : (
          <div className="space-y-4">
            <div className="p-4 rounded-2xl bg-amber-50/70 border border-amber-200">
              <div className="text-xs font-bold uppercase text-amber-800">
                {t('vocab.vietnameseMeaning')}
              </div>
              <div className="text-lg font-extrabold text-slate-900 mt-0.5">
                {currentCard.meaningVi}
              </div>
              <p className="text-xs text-slate-600 mt-1">{currentCard.descriptionEn}</p>
            </div>

            {/* Synonyms & Antonyms */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div className="p-3 rounded-2xl bg-slate-50 border border-slate-200/80">
                <div className="text-xs font-bold text-slate-700 mb-1">
                  {t('vocab.synonyms')}
                </div>
                <div className="flex flex-wrap gap-1.5">
                  {currentCard.synonyms.length > 0 ? (
                    currentCard.synonyms.map((s) => (
                      <Badge key={s} variant="emerald">
                        {s}
                      </Badge>
                    ))
                  ) : (
                    <span className="text-xs text-slate-400">{t('vocab.none')}</span>
                  )}
                </div>
              </div>
              <div className="p-3 rounded-2xl bg-slate-50 border border-slate-200/80">
                <div className="text-xs font-bold text-slate-700 mb-1">
                  {t('vocab.antonyms')}
                </div>
                <div className="flex flex-wrap gap-1.5">
                  {currentCard.antonyms.length > 0 ? (
                    currentCard.antonyms.map((a) => (
                      <Badge key={a} variant="rose">
                        {a}
                      </Badge>
                    ))
                  ) : (
                    <span className="text-xs text-slate-400">{t('vocab.none')}</span>
                  )}
                </div>
              </div>
            </div>

            {/* Collocations */}
            {currentCard.collocations.length > 0 && (
              <div className="p-3.5 rounded-2xl bg-slate-50 border border-slate-200/80 space-y-1.5">
                <div className="text-xs font-bold text-slate-700">
                  {t('vocab.collocations')}
                </div>
                {currentCard.collocations.map((col) => (
                  <div key={col.id} className="text-xs text-slate-700">
                    <strong className="text-emerald-800">{col.phrase}</strong> —{' '}
                    {col.meaningVi}
                  </div>
                ))}
              </div>
            )}

            {/* Bilingual Examples with US/UK Teacher Voice Play Buttons */}
            {currentCard.examples.map((ex) => (
              <div
                key={ex.id}
                className="p-3.5 rounded-2xl bg-emerald-50/50 border border-emerald-200 text-xs space-y-1.5"
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="font-bold text-slate-800">{ex.sentenceEn}</div>
                  <div className="flex items-center gap-1 shrink-0">
                    <button
                      type="button"
                      disabled={isSynthesizing}
                      onClick={() =>
                        void onSynthesizeTeacherSpeech(
                          ex.sentenceEn,
                          'sentence',
                          'US',
                          ex.id,
                          currentCard.vocabularyId,
                          currentCard.senseId
                        )
                      }
                      className="px-2 py-0.5 rounded bg-white border border-emerald-200 font-bold text-emerald-700 hover:bg-emerald-100 cursor-pointer"
                    >
                      🔊 US
                    </button>
                    <button
                      type="button"
                      disabled={isSynthesizing}
                      onClick={() =>
                        void onSynthesizeTeacherSpeech(
                          ex.sentenceEn,
                          'sentence',
                          'UK',
                          ex.id,
                          currentCard.vocabularyId,
                          currentCard.senseId
                        )
                      }
                      className="px-2 py-0.5 rounded bg-white border border-sky-200 font-bold text-sky-700 hover:bg-sky-100 cursor-pointer"
                    >
                      🔊 UK
                    </button>
                  </div>
                </div>
                <div className="text-slate-600">{ex.translationVi}</div>
              </div>
            ))}
          </div>
        )}

        {/* SM-2 Spaced Repetition Rating Buttons */}
        <div className="pt-4 border-t border-slate-100">
          <div className="text-xs font-bold text-slate-500 mb-2 text-center">
            {t('vocab.sm2RatingTitle')}
          </div>
          <div className="grid grid-cols-4 gap-2">
            {(['Again', 'Hard', 'Good', 'Easy'] as ReviewRating[]).map((rating) => (
              <button
                key={rating}
                type="button"
                onClick={() => void onReview(rating)}
                className={cn(
                  'py-2.5 px-3 rounded-xl font-bold text-xs border transition-all cursor-pointer',
                  rating === 'Again' &&
                    'bg-rose-50 hover:bg-rose-100 text-rose-700 border-rose-200',
                  rating === 'Hard' &&
                    'bg-amber-50 hover:bg-amber-100 text-amber-800 border-amber-200',
                  rating === 'Good' &&
                    'bg-emerald-50 hover:bg-emerald-100 text-emerald-700 border-emerald-200',
                  rating === 'Easy' &&
                    'bg-sky-50 hover:bg-sky-100 text-sky-700 border-sky-200'
                )}
              >
                {rating === 'Again'
                  ? t('vocab.ratingAgain')
                  : rating === 'Hard'
                  ? t('vocab.ratingHard')
                  : rating === 'Good'
                  ? t('vocab.ratingGood')
                  : t('vocab.ratingEasy')}
              </button>
            ))}
          </div>
        </div>
      </div>
    );
  }
);

VocabularyFlashcard.displayName = 'VocabularyFlashcard';
