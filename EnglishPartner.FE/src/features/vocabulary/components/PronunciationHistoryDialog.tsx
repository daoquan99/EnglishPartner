import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  History,
  Mic,
  BookOpen,
  Clock,
  Award,
  AlertCircle,
  ShieldCheck,
  Calendar,
  CheckCircle2,
} from 'lucide-react';
import { Dialog } from '@/shared/components/ui/Dialog';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { cn } from '@/shared/lib/cn';
import type {
  PronunciationAssessmentResultDto,
  VocabularyExampleDto,
  VocabularyFlashcardDto,
} from '../types/vocabulary.types';

export interface PronunciationHistoryDialogProps {
  isOpen: boolean;
  onClose: () => void;
  card: VocabularyFlashcardDto | null;
  targetType: 'word' | 'example';
  selectedExample: VocabularyExampleDto | null;
  selectedExampleIndex: number;
  recentAssessmentResult: PronunciationAssessmentResultDto | null;
}

export const PronunciationHistoryDialog: React.FC<PronunciationHistoryDialogProps> = ({
  isOpen,
  onClose,
  card,
  targetType,
  selectedExample,
  selectedExampleIndex,
  recentAssessmentResult,
}) => {
  const { t, i18n } = useTranslation();
  const [activeTab, setActiveTab] = useState<'pronunciation' | 'flashcard'>('pronunciation');

  if (!card) return null;

  const isWordTarget = targetType === 'word';
  const hasExample = !isWordTarget && Boolean(selectedExample);

  const title = isWordTarget
    ? `${t('vocab.historyModalTitle')}: ${t('vocab.historyTargetWord')} "${card.word}"`
    : selectedExample
      ? `${t('vocab.historyModalTitle')}: ${t('vocab.historyTargetExample')} #${selectedExampleIndex + 1}`
      : `${t('vocab.historyModalTitle')}: ${t('vocab.historyTargetExample')}`;

  const description = isWordTarget
    ? `${t('vocab.vietnameseMeaning')}: ${card.meaningVi || card.definitionEn} · ${t('common.cefrLevel')} ${card.cefrLevel}`
    : selectedExample
      ? `"${selectedExample.sentenceEn}"`
      : t('vocab.noExamples');

  // Format date helper with user's local timezone
  const formatLocalDate = (isoString?: string | null): string => {
    if (!isoString) return t('vocab.noSchedule');
    try {
      const date = new Date(isoString);
      return new Intl.DateTimeFormat(i18n.language === 'vi' ? 'vi-VN' : 'en-US', {
        dateStyle: 'medium',
        timeStyle: 'short',
      }).format(date);
    } catch {
      return isoString;
    }
  };

  // Determine if recentAssessmentResult matches the current target
  const isRecentResultForTarget =
    recentAssessmentResult &&
    (isWordTarget
      ? !recentAssessmentResult.targetId || recentAssessmentResult.targetType === 'Word'
      : recentAssessmentResult.targetId === selectedExample?.id ||
        recentAssessmentResult.targetType === 'Sentence');

  return (
    <div className="learnerTheme">
      <Dialog
        isOpen={isOpen}
        onClose={onClose}
        maxWidth="3xl"
        icon={
          <img
            src="/assets/icons/navigation/nav-history.svg"
            alt=""
            className="w-5 h-5 object-contain"
          />
        }
        title={title}
        description={description}
        footer={
          <div className="flex items-center justify-between gap-3 w-full">
            <span className="text-[11px] text-slate-500 font-medium">
              ID Thẻ: <span className="font-mono text-slate-700">{card.vocabularyId.slice(0, 8)}...</span> · Nghĩa:{' '}
              <span className="font-mono text-slate-700">{card.senseId.slice(0, 8)}...</span>
            </span>
            <Button variant="outline" size="sm" onClick={onClose}>
              {t('common.closeDialog')}
            </Button>
          </div>
        }
      >
        <div className="space-y-4 text-xs">
          {/* Target Tabs (Word: Pronunciation + Flashcard Review; Example: Pronunciation Only) */}
          {isWordTarget ? (
            <div className="flex border-b border-slate-200 gap-2">
              <button
                type="button"
                onClick={() => setActiveTab('pronunciation')}
                className={cn(
                  'flex items-center gap-1.5 pb-2.5 px-3 font-bold border-b-2 transition-all cursor-pointer text-xs',
                  activeTab === 'pronunciation'
                    ? 'border-emerald-600 text-emerald-700'
                    : 'border-transparent text-slate-500 hover:text-slate-800'
                )}
              >
                <img src="/assets/icons/navigation/nav-voices.svg" alt="" className="w-3.5 h-3.5 object-contain" />
                {t('vocab.pronunciationTab')} ({card.word})
              </button>
              <button
                type="button"
                onClick={() => setActiveTab('flashcard')}
                className={cn(
                  'flex items-center gap-1.5 pb-2.5 px-3 font-bold border-b-2 transition-all cursor-pointer text-xs',
                  activeTab === 'flashcard'
                    ? 'border-emerald-600 text-emerald-700'
                    : 'border-transparent text-slate-500 hover:text-slate-800'
                )}
              >
                <img src="/assets/icons/navigation/nav-study.svg" alt="" className="w-3.5 h-3.5 object-contain" />
                {t('vocab.flashcardTab')}
              </button>
            </div>
          ) : (
            <div className="flex border-b border-slate-200 gap-2">
              <div className="flex items-center gap-1.5 pb-2.5 px-3 font-bold border-b-2 border-emerald-600 text-emerald-700 text-xs">
                <img src="/assets/icons/navigation/nav-sentences.svg" alt="" className="w-3.5 h-3.5 object-contain" />
                {t('vocab.historyTargetExample')}
              </div>
            </div>
          )}

          {/* If target is example and there are no examples, show empty message */}
          {!isWordTarget && !hasExample ? (
            <div className="p-6 text-center rounded-2xl bg-slate-50 border border-slate-200 space-y-2">
              <AlertCircle className="w-8 h-8 text-amber-500 mx-auto" />
              <div className="text-sm font-bold text-slate-800">
                {t('vocab.noExamples')}
              </div>
              <p className="text-xs text-slate-500 max-w-md mx-auto">
                {t('vocab.noExampleHistory')}
              </p>
            </div>
          ) : (
            <>
              {/* Backend Blocker / Pending Integration Notice */}
              <div className="p-4 rounded-2xl bg-amber-50/80 border border-amber-300 text-amber-900 space-y-2">
                <div className="flex items-center gap-2">
                  <div className="p-1 rounded-lg bg-amber-100 text-amber-700">
                    <Clock className="w-4 h-4" />
                  </div>
                  <div className="font-extrabold text-xs sm:text-sm text-amber-950">
                    {t('vocab.historyPending')}
                  </div>
                  <Badge variant="amber" className="ml-auto text-[10px]">
                    Contract Pending
                  </Badge>
                </div>
                <p className="text-xs text-amber-900 leading-relaxed">
                  {activeTab === 'pronunciation'
                    ? t('vocab.historyPendingDescPronunciation')
                    : t('vocab.historyPendingDescFlashcard')}
                </p>
                <div className="pt-1.5 border-t border-amber-200/80 text-[11px] text-amber-800 flex flex-wrap gap-x-4 gap-y-1">
                  <span>
                    🎯 {t('vocab.targetLabel')}{' '}
                    <strong>
                      {isWordTarget
                        ? `${t('vocab.historyTargetWord')} "${card.word}" (Word)`
                        : `${t('vocab.historyTargetExample')} #${selectedExampleIndex + 1} (Sentence)`}
                    </strong>
                  </span>
                  <span>
                    📋 {t('vocab.standardDoc')}{' '}
                    <code className="px-1 py-0.5 rounded bg-amber-100 text-[10px]">
                      pronunciation-learning-history-be.md
                    </code>
                  </span>
                </div>
              </div>

              {/* In-Session Recent Result (if available for this target) */}
              {activeTab === 'pronunciation' && isRecentResultForTarget && recentAssessmentResult && (
                <div className="p-4 rounded-2xl bg-emerald-50/70 border border-emerald-200 space-y-3">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <CheckCircle2 className="w-4 h-4 text-emerald-600" />
                      <span className="font-extrabold text-xs text-emerald-950">
                        {t('vocab.recentSessionResult')}
                      </span>
                    </div>
                    <span className="text-[10px] text-slate-500">
                      {formatLocalDate(recentAssessmentResult.assessedAtUtc)}
                    </span>
                  </div>

                  <div className="p-3 rounded-xl bg-white border border-emerald-200 flex flex-wrap items-center justify-between gap-3">
                    <div className="flex items-center gap-3">
                      <div className="w-9 h-9 rounded-xl bg-emerald-600 text-white flex items-center justify-center font-extrabold text-sm shadow-xs">
                        {recentAssessmentResult.overallScore === null
                          ? '—'
                          : recentAssessmentResult.overallScore}
                      </div>
                      <div>
                        <div className="font-extrabold text-slate-900 text-xs">
                          {recentAssessmentResult.overallScore === null
                            ? t('vocab.cannotScore')
                            : `${t('vocab.totalScore')} ${recentAssessmentResult.overallScore}/100`}
                        </div>
                        <div className="text-[10px] text-slate-500">
                          Model: {recentAssessmentResult.modelCodeUsed} · {t('common.status')}: {recentAssessmentResult.status}
                        </div>
                      </div>
                    </div>
                    <Badge variant="emerald">{t('vocab.currentSession')}</Badge>
                  </div>

                  {/* Transcript */}
                  {recentAssessmentResult.recognizedTranscript && (
                    <div className="p-2.5 rounded-xl bg-white border border-slate-200 text-xs">
                      <span className="font-bold text-slate-700">{t('vocab.aiTranscript')}</span>{' '}
                      <span className="italic text-slate-600">
                        &ldquo;{recentAssessmentResult.recognizedTranscript}&rdquo;
                      </span>
                    </div>
                  )}

                  {/* Criteria Scores */}
                  {recentAssessmentResult.criterionScores &&
                    recentAssessmentResult.criterionScores.length > 0 && (
                      <div className="grid grid-cols-1 sm:grid-cols-3 gap-2">
                        {recentAssessmentResult.criterionScores.map((c) => (
                          <div
                            key={c.criterionCode}
                            className="p-2.5 rounded-xl bg-white border border-emerald-200 text-xs"
                          >
                            <div className="font-extrabold text-slate-900">
                              {c.criterionName || c.criterionCode}: {c.score}/100
                            </div>
                            <div className="text-[10px] text-emerald-700 font-semibold">
                              Weight: {c.weight}%
                            </div>
                            {c.feedbackVi && (
                              <div className="text-[11px] text-slate-600 mt-0.5">
                                {c.feedbackVi}
                              </div>
                            )}
                          </div>
                        ))}
                      </div>
                    )}

                  {/* Feedback */}
                  {(recentAssessmentResult.phonemeAndStressFeedbackVi ||
                    recentAssessmentResult.coachingSuggestionVi) && (
                    <div className="p-3 rounded-xl bg-white border border-slate-200 text-xs space-y-1">
                      {recentAssessmentResult.phonemeAndStressFeedbackVi && (
                        <p>
                          <strong className="text-slate-800">{t('vocab.phonemeFeedback')}</strong>{' '}
                          <span className="text-slate-600">
                            {recentAssessmentResult.phonemeAndStressFeedbackVi}
                          </span>
                        </p>
                      )}
                      {recentAssessmentResult.coachingSuggestionVi && (
                        <p>
                          <strong className="text-emerald-800">{t('vocab.coachingSuggestion')}</strong>{' '}
                          <span className="text-slate-600">
                            {recentAssessmentResult.coachingSuggestionVi}
                          </span>
                        </p>
                      )}
                    </div>
                  )}
                </div>
              )}

              {/* Flashcard Review Overview (Tab Flashcard) */}
              {activeTab === 'flashcard' && (
                <div className="space-y-3">
                  <div className="p-4 rounded-2xl bg-slate-50 border border-slate-200 space-y-3">
                    <div className="font-bold text-slate-800 flex items-center gap-1.5">
                      <Calendar className="w-4 h-4 text-emerald-600" />
                      {t('vocab.sm2RatingTitle')}
                    </div>

                    <div className="grid grid-cols-2 sm:grid-cols-4 gap-2.5 text-center">
                      <div className="p-2.5 rounded-xl bg-white border border-slate-200">
                        <div className="text-[10px] text-slate-500 font-bold uppercase">
                          {t('common.status')}
                        </div>
                        <div className="text-sm font-extrabold text-slate-900 mt-0.5">
                          {card.learningStatus || 'Learning'}
                        </div>
                      </div>
                      <div className="p-2.5 rounded-xl bg-white border border-slate-200">
                        <div className="text-[10px] text-slate-500 font-bold uppercase">
                          {t('vocab.repetitionCount')}
                        </div>
                        <div className="text-sm font-extrabold text-slate-900 mt-0.5">
                          {card.repetitionCount}
                        </div>
                      </div>
                      <div className="p-2.5 rounded-xl bg-white border border-slate-200">
                        <div className="text-[10px] text-slate-500 font-bold uppercase">
                          {t('vocab.intervalDays')}
                        </div>
                        <div className="text-sm font-extrabold text-slate-900 mt-0.5">
                          {card.intervalDays} {t('vocab.days')}
                        </div>
                      </div>
                      <div className="p-2.5 rounded-xl bg-white border border-slate-200">
                        <div className="text-[10px] text-slate-500 font-bold uppercase">
                          {t('vocab.easeFactor')}
                        </div>
                        <div className="text-sm font-extrabold text-emerald-700 mt-0.5">
                          {card.easeFactor}
                        </div>
                      </div>
                    </div>

                    <div className="p-3 rounded-xl bg-white border border-slate-200 flex items-center justify-between text-xs">
                      <span className="text-slate-600">{t('vocab.nextReviewTime')}</span>
                      <strong className="text-slate-900">
                        {formatLocalDate(card.nextReviewAtUtc)}
                      </strong>
                    </div>
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      </Dialog>
    </div>
  );
};
