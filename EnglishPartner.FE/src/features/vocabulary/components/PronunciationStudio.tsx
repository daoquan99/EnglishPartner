import React from 'react';
import { useTranslation } from 'react-i18next';
import {
  AlertCircle,
  Award,
  ChevronLeft,
  ChevronRight,
  Cpu,
  Mic,
  Square,
  Volume2,
} from 'lucide-react';
import { Badge } from '@/shared/components/ui/Badge';
import { Button } from '@/shared/components/ui/Button';
import { cn } from '@/shared/lib/cn';
import { toast } from '@/shared/lib/toast';
import type {
  PronunciationAssessmentResultDto,
  VocabularyExampleDto,
  VocabularyFlashcardDto,
} from '../types/vocabulary.types';

export interface PronunciationStudioProps {
  currentCard: VocabularyFlashcardDto;
  targetType: 'word' | 'example';
  onSelectTargetType: (type: 'word' | 'example') => void;
  examples: VocabularyExampleDto[];
  selectedExample: VocabularyExampleDto | null;
  selectedExampleIndex: number;
  hasExamples: boolean;
  onSelectExampleIndex: (index: number) => void;
  onOpenHistory: () => void;
  onOpenTtsSettings: () => void;
  aiProvider: 'gemini' | 'agy';
  onSetAiProvider: (provider: 'gemini' | 'agy') => void;
  effectiveAssessmentModel: string;
  onSynthesizeTeacherSpeech: (
    textToSpeak: string,
    kind: 'word' | 'sentence',
    overrideAccent?: 'US' | 'UK',
    targetId?: string,
    vocabularyId?: string,
    senseId?: string
  ) => Promise<void>;
  isSynthesizing: boolean;
  isRecording: boolean;
  onStartRecording: () => void;
  onStopRecording: () => void;
  isAssessing: boolean;
  activeRubricCode?: string;
  assessmentResult: PronunciationAssessmentResultDto | null;
  assessmentError: string | null;
  onClearAssessmentError: () => void;
}

export const PronunciationStudio: React.FC<PronunciationStudioProps> = React.memo(
  ({
    currentCard,
    targetType,
    onSelectTargetType,
    examples,
    selectedExample,
    selectedExampleIndex,
    hasExamples,
    onSelectExampleIndex,
    onOpenHistory,
    onOpenTtsSettings,
    aiProvider,
    onSetAiProvider,
    effectiveAssessmentModel,
    onSynthesizeTeacherSpeech,
    isSynthesizing,
    isRecording,
    onStartRecording,
    onStopRecording,
    isAssessing,
    activeRubricCode,
    assessmentResult,
    assessmentError,
    onClearAssessmentError,
  }) => {
    const { t } = useTranslation();

    return (
      <div className="bg-white rounded-3xl border border-slate-200/90 shadow-xs p-6 space-y-4">
        <div className="flex flex-wrap items-center justify-between gap-2 border-b border-slate-100 pb-3">
          <div>
            <h3 className="text-base font-extrabold text-slate-900 flex items-center gap-2">
              <img src="/assets/icons/navigation/nav-voices.svg" alt="" className="w-5 h-5 object-contain" />
              {t('vocab.pronunciationStudio')}
            </h3>
            <p className="text-xs text-slate-500">
              {t('vocab.syncedWithCard')}{' '}
              <strong className="text-slate-900">{currentCard.word}</strong>
            </p>
          </div>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={onOpenHistory}
              title={t('vocab.viewHistory')}
            >
              <img src="/assets/icons/navigation/nav-history.svg" alt="" className="w-3.5 h-3.5 object-contain" />
              {t('vocab.viewHistory')}
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={onOpenTtsSettings}
              title="Cấu hình AI Speech Synthesis"
              className="flex items-center gap-1.5"
            >
              <Cpu className="w-3.5 h-3.5 text-purple-600" />
              Cấu hình AI
            </Button>
          </div>
        </div>

        {/* Cổng AI Model hệ thống & Model Chấm điểm */}
        <div className="p-3.5 rounded-2xl bg-slate-50 border border-slate-200/90 space-y-2.5">
          <div className="flex items-center justify-between gap-2">
            <div className="flex items-center gap-1.5 text-xs font-bold text-slate-800">
              <Cpu className="w-4 h-4 text-emerald-600 shrink-0" />
              <span>Cổng AI Model hệ thống (Sinh nội dung & Chấm điểm)</span>
              <Badge variant={aiProvider === 'agy' ? 'emerald' : 'sky'} className="text-[10px] py-0 px-1.5">
                {aiProvider === 'agy' ? 'AGY Local' : 'Google Gemini API'}
              </Badge>
            </div>
            <button
              type="button"
              onClick={onOpenTtsSettings}
              className="text-xs font-bold text-emerald-700 hover:text-emerald-800 hover:underline shrink-0 cursor-pointer"
            >
              Cấu hình
            </button>
          </div>

          <div className="text-xs text-slate-600 flex items-center justify-between bg-white px-3 py-2 rounded-xl border border-slate-200/80">
            <span className="text-slate-500 font-medium">
              {aiProvider === 'agy'
                ? 'Model Antigravity CLI Local được chọn:'
                : 'Mô hình AI Chấm điểm & Nhận xét bài tập:'}
            </span>
            <span className="font-bold text-slate-800 text-[11px] font-mono">
              {effectiveAssessmentModel}
            </span>
          </div>

          {aiProvider === 'agy' && (
            <div className="p-3 rounded-2xl bg-amber-50/90 border border-amber-200 text-xs text-amber-900 flex flex-wrap sm:flex-nowrap items-center justify-between gap-2.5">
              <div className="flex items-start gap-2 min-w-0">
                <AlertCircle className="w-4 h-4 text-amber-600 shrink-0 mt-0.5" />
                <span className="text-[11px] leading-relaxed">
                  Chấm phát âm bằng giọng nói (Audio) yêu cầu Google Gemini Cloud API.
                </span>
              </div>
              <Button
                type="button"
                size="sm"
                variant="primary"
                onClick={() => {
                  onSetAiProvider('gemini');
                  toast.success('Đã chuyển sang cổng Google Gemini API (Cloud)');
                }}
                className="text-xs font-bold py-1 px-3 shrink-0 shadow-2xs"
              >
                Chuyển sang Google Gemini API
              </Button>
            </div>
          )}
        </div>

        <div className="space-y-3">
          <div>
            <label className="block text-xs font-bold text-slate-600 mb-1">
              {t('vocab.practiceMode')}
            </label>
            <div className="grid grid-cols-2 gap-2">
              <button
                type="button"
                onClick={() => onSelectTargetType('word')}
                className={cn(
                  'py-2 rounded-xl text-xs font-bold border transition-all cursor-pointer',
                  targetType === 'word'
                    ? 'bg-emerald-600 text-white border-emerald-600 shadow-2xs'
                    : 'bg-slate-50 text-slate-600 border-slate-200 hover:bg-slate-100'
                )}
              >
                {t('vocab.singleWord')} ({currentCard.word})
              </button>
              <button
                type="button"
                onClick={() => onSelectTargetType('example')}
                className={cn(
                  'py-2 rounded-xl text-xs font-bold border transition-all cursor-pointer',
                  targetType === 'example'
                    ? 'bg-emerald-600 text-white border-emerald-600 shadow-2xs'
                    : 'bg-slate-50 text-slate-600 border-slate-200 hover:bg-slate-100'
                )}
              >
                {t('vocab.exampleSentence')} {hasExamples ? `(${examples.length})` : ''}
              </button>
            </div>
          </div>
        </div>

        {/* Target Sentence Box + Listen Sample US/UK + Record Button */}
        <div className="p-4 rounded-2xl bg-slate-50 border border-slate-200 space-y-3">
          <div className="flex items-center justify-between gap-2">
            <div className="text-xs text-slate-500 font-bold uppercase tracking-wider">
              {t('vocab.sampleContent')}
            </div>
            {targetType === 'example' && examples.length > 1 && (
              <div className="flex items-center gap-1.5">
                <span className="text-xs font-semibold text-slate-600">
                  {t('vocab.sentenceIndex')} {selectedExampleIndex + 1}/{examples.length}
                </span>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  className="h-7 px-2 text-xs"
                  disabled={selectedExampleIndex === 0}
                  onClick={() => onSelectExampleIndex(selectedExampleIndex - 1)}
                  title={t('vocab.prevSentence')}
                >
                  <ChevronLeft className="w-3.5 h-3.5" />
                  <span className="hidden sm:inline">{t('vocab.prevSentence')}</span>
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  className="h-7 px-2 text-xs"
                  disabled={selectedExampleIndex >= examples.length - 1}
                  onClick={() => onSelectExampleIndex(selectedExampleIndex + 1)}
                  title={t('vocab.nextSentence')}
                >
                  <span className="hidden sm:inline">{t('vocab.nextSentence')}</span>
                  <ChevronRight className="w-3.5 h-3.5" />
                </Button>
              </div>
            )}
          </div>

          <div className="max-h-36 overflow-y-auto space-y-1.5 pr-1">
            {targetType === 'word' ? (
              <div className="text-base sm:text-lg font-extrabold text-slate-900 break-words leading-relaxed">
                &ldquo;{currentCard.word}&rdquo;
              </div>
            ) : hasExamples && selectedExample ? (
              <>
                <div className="text-sm sm:text-base font-extrabold text-slate-900 break-words leading-relaxed">
                  &ldquo;{selectedExample.sentenceEn}&rdquo;
                </div>
                {selectedExample.translationVi && (
                  <div className="text-xs text-slate-500 italic break-words">
                    {selectedExample.translationVi}
                  </div>
                )}
              </>
            ) : (
              <div className="p-3 rounded-xl bg-amber-50 border border-amber-200 text-amber-800 text-xs flex items-center gap-2">
                <AlertCircle className="w-4 h-4 shrink-0 text-amber-600" />
                <span>{t('vocab.noExamples')}</span>
              </div>
            )}
          </div>

          <div className="flex flex-wrap items-center justify-between gap-2 pt-2 border-t border-slate-200/80">
            <div className="flex items-center gap-1.5">
              <Button
                variant="outline"
                size="sm"
                disabled={
                  isSynthesizing || (targetType === 'example' && !hasExamples)
                }
                onClick={() => {
                  const isExample = targetType === 'example';
                  if (isExample && !selectedExample) return;
                  const text = isExample ? selectedExample!.sentenceEn : currentCard.word;
                  const kind = isExample ? 'sentence' : 'word';
                  const targetId = isExample
                    ? selectedExample!.id
                    : currentCard.vocabularyId;
                  const senseId = isExample ? currentCard.senseId : undefined;
                  void onSynthesizeTeacherSpeech(
                    text,
                    kind,
                    'US',
                    targetId,
                    currentCard.vocabularyId,
                    senseId
                  );
                }}
              >
                <Volume2 className="w-3.5 h-3.5 text-emerald-600" />
                {t('vocab.sampleUs')}
              </Button>
              <Button
                variant="outline"
                size="sm"
                disabled={
                  isSynthesizing || (targetType === 'example' && !hasExamples)
                }
                onClick={() => {
                  const isExample = targetType === 'example';
                  if (isExample && !selectedExample) return;
                  const text = isExample ? selectedExample!.sentenceEn : currentCard.word;
                  const kind = isExample ? 'sentence' : 'word';
                  const targetId = isExample
                    ? selectedExample!.id
                    : currentCard.vocabularyId;
                  const senseId = isExample ? currentCard.senseId : undefined;
                  void onSynthesizeTeacherSpeech(
                    text,
                    kind,
                    'UK',
                    targetId,
                    currentCard.vocabularyId,
                    senseId
                  );
                }}
              >
                <Volume2 className="w-3.5 h-3.5 text-sky-600" />
                {t('vocab.sampleUk')}
              </Button>
            </div>

            {!isRecording ? (
              <Button
                variant="primary"
                size="sm"
                disabled={
                  isAssessing || (targetType === 'example' && !hasExamples)
                }
                onClick={onStartRecording}
              >
                <Mic className="w-3.5 h-3.5" />
                {isAssessing ? t('vocab.scoring') : t('vocab.recordAndScore')}
              </Button>
            ) : (
              <Button variant="danger" size="sm" onClick={onStopRecording}>
                <Square className="w-3.5 h-3.5" />
                {t('vocab.stopRecording')}
              </Button>
            )}
          </div>
        </div>

        {/* Scoring Rubric Breakdown */}
        <div className="p-3.5 rounded-2xl bg-slate-50/70 border border-slate-200/80 space-y-2">
          <div className="flex items-center justify-between text-[11px] font-bold text-slate-600">
            <span>{t('vocab.rubricStandard')} {activeRubricCode ?? 'ENGLISH_PRONUNCIATION_V1'}</span>
            <span>Accuracy 40% · Clarity 30% · Fluency 30%</span>
          </div>

          {assessmentError && (
            <div role="alert" className="text-xs text-red-700 bg-red-50 border border-red-200 rounded-xl p-3 flex flex-wrap items-center justify-between gap-2">
              <span className="flex-1 min-w-[200px]">{assessmentError}</span>
              {aiProvider === 'agy' && (
                <Button
                  type="button"
                  size="sm"
                  variant="primary"
                  onClick={() => {
                    onSetAiProvider('gemini');
                    onClearAssessmentError();
                    toast.success('Đã chuyển sang cổng Google Gemini API (Cloud)');
                  }}
                  className="text-xs py-1 px-3 shrink-0"
                >
                  Chuyển sang Google Gemini API ngay
                </Button>
              )}
            </div>
          )}

          {assessmentResult ? (
            <div className="pt-2 space-y-3">
              <div className="flex items-center justify-between p-3 rounded-xl bg-emerald-600 text-white">
                <div className="flex items-center gap-2">
                  <Award className="w-5 h-5" />
                  <span className="text-sm font-extrabold">
                    {assessmentResult.overallScore === null
                      ? t('vocab.cannotScore')
                      : `${t('vocab.totalScore')} ${assessmentResult.overallScore}/100`}
                  </span>
                </div>
                <span className="text-xs font-bold bg-white/20 px-2.5 py-0.5 rounded-full">
                  {assessmentResult.modelCodeUsed}
                </span>
              </div>

              <div className="grid grid-cols-3 gap-2">
                {assessmentResult.criterionScores.map((c) => (
                  <div
                    key={c.criterionCode}
                    className="p-2.5 rounded-xl bg-white border border-emerald-200 text-xs"
                  >
                    <div className="font-extrabold text-slate-900">
                      {c.criterionCode}: {c.score}/100
                    </div>
                    <div className="text-[10px] text-emerald-700 font-semibold">
                      Weight: {c.weight}%
                    </div>
                    <div className="text-[11px] text-slate-500 mt-0.5">{c.feedbackVi}</div>
                  </div>
                ))}
              </div>

              <div className="text-xs text-slate-700 bg-white p-3 rounded-xl border border-slate-200 space-y-1">
                <p>
                  <strong>{t('vocab.phonemeFeedback')}</strong>{' '}
                  {assessmentResult.phonemeAndStressFeedbackVi}
                </p>
                <p className="text-emerald-800 font-medium">
                  <strong>{t('vocab.coachingSuggestion')}</strong> {assessmentResult.coachingSuggestionVi}
                </p>
              </div>
            </div>
          ) : (
            <p className="text-[11px] text-slate-500">
              {t('vocab.audioTtsGuide')}
            </p>
          )}
        </div>
      </div>
    );
  }
);

PronunciationStudio.displayName = 'PronunciationStudio';
