import React from 'react';
import { useTranslation } from 'react-i18next';
import {
  ArrowRight,
  Volume2,
  Square,
  Lightbulb,
  Loader2,
  Wand2,
  AlertCircle,
  Sparkles,
} from 'lucide-react';
import type { GrammarCorrectionItem } from '../types/tutor.types';
import type { FeedbackAudioResponseDto } from '@/shared/api/openapi.types';
import { Badge } from '@/shared/components/ui/Badge';
import { Button } from '@/shared/components/ui/Button';
import { useFeedbackAudio } from '../hooks/useFeedbackAudio';

export const normalizeSentence = (str?: string | null): string =>
  (str || '')
    .toLowerCase()
    .replace(/[.,/#!$%^&*;:{}=\-_`~()?]/g, '')
    .replace(/\s+/g, ' ')
    .trim();

interface GrammarCorrectionPanelProps {
  corrections: GrammarCorrectionItem[];
  isAnalyzing?: boolean;
  canAnalyzeLatestTurn?: boolean;
  replayingTurnId?: string | null;
  onAnalyzeLatestTurn?: () => void;
  onReplayUserTurnAudio?: (turnId: string, fallbackText: string) => void;
  onRequestFeedbackAudio?: (
    correctionId: string,
    variant: 'corrected' | 'improved'
  ) => Promise<FeedbackAudioResponseDto | null>;
  onReanalyzeTurn?: (turnId: string, originalSentence: string) => void;
}

export const GrammarCorrectionPanel: React.FC<GrammarCorrectionPanelProps> = ({
  corrections,
  isAnalyzing = false,
  canAnalyzeLatestTurn = false,
  replayingTurnId = null,
  onAnalyzeLatestTurn,
  onReplayUserTurnAudio,
  onRequestFeedbackAudio,
  onReanalyzeTurn,
}) => {
  const { t, i18n } = useTranslation();
  const dateLocale = i18n.language === 'vi' ? 'vi-VN' : 'en-US';

  const { loadingKey, playFeedbackAudio } = useFeedbackAudio({
    onRequestFeedbackAudio,
  });

  const renderStatusBadge = (item: GrammarCorrectionItem) => {
    switch (item.status) {
      case 'has_errors':
        return <Badge variant="rose">{t('tutor.statusHasErrors', 'Cần sửa')}</Badge>;
      case 'can_improve':
        return <Badge variant="sky">{t('tutor.statusCanImprove', 'Có thể cải thiện')}</Badge>;
      case 'already_natural':
        return <Badge variant="emerald">{t('tutor.statusAlreadyNatural', 'Đã tự nhiên')}</Badge>;
      case 'analysis_failed':
        return <Badge variant="amber">{t('tutor.statusAnalysisFailed', 'Phân tích lỗi')}</Badge>;
      default:
        return <Badge variant="amber">{item.errorCategory || 'Grammar & Vocabulary'}</Badge>;
    }
  };

  if (corrections.length === 0) {
    return (
      <div className="bg-white rounded-3xl border border-slate-200/90 p-5 text-center space-y-3 shadow-xs">
        <div className="w-12 h-12 rounded-2xl bg-emerald-50 border border-emerald-100 flex items-center justify-center mx-auto p-2 shadow-2xs">
          {isAnalyzing ? (
            <Loader2 className="w-6 h-6 animate-spin text-amber-600" />
          ) : (
            <img src="/assets/icons/navigation/nav-rubrics.svg" alt="" className="w-7 h-7 object-contain" />
          )}
        </div>
        <div className="space-y-1">
          <h3 className="text-sm font-extrabold text-slate-800">
            {t('tutor.grammarRealtimeTitle', 'Sửa lỗi Ngữ pháp & Từ vựng Realtime')}
          </h3>
          <p className="text-xs text-slate-500 leading-relaxed">
            {isAnalyzing
              ? t(
                  'tutor.analyzingMsg',
                  'AI đang phân tích câu nói vừa rồi của bạn để gợi ý ngữ pháp & từ vựng...'
                )
              : t(
                  'tutor.idleGrammarMsg',
                  'Khi bạn nói tiếng Anh, AI sẽ tự động phân tích ngữ pháp, cấu trúc câu và gợi ý cách diễn đạt tự nhiên hơn tại đây.'
                )}
          </p>
        </div>

        {canAnalyzeLatestTurn && onAnalyzeLatestTurn && (
          <div className="pt-1">
            <Button
              variant="outline"
              size="sm"
              disabled={isAnalyzing}
              onClick={onAnalyzeLatestTurn}
              className="mx-auto border-amber-300 bg-amber-50/70 hover:bg-amber-100 text-amber-900 font-bold"
            >
              <Wand2 className="w-3.5 h-3.5 text-amber-700" />
              {t('tutor.analyzeLatestBtn', 'Phân tích lượt nói gần nhất')}
            </Button>
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="bg-white rounded-3xl border border-slate-200/90 p-4 sm:p-5 flex flex-col h-[calc(100vh-9.5rem)] min-h-[540px] shadow-xs">
      <div className="flex items-center justify-between gap-2 border-b border-slate-100 pb-3 shrink-0">
        <div className="flex items-center gap-2">
          <img src="/assets/icons/navigation/nav-rubrics.svg" alt="" className="w-4 h-4 object-contain shrink-0" />
          <h3 className="text-sm font-extrabold text-slate-900">
            {t('tutor.grammarListTitle', {
              count: corrections.length,
              defaultValue: `Sửa lỗi Ngữ pháp & Từ vựng (${corrections.length})`,
            })}
          </h3>
          {isAnalyzing && (
            <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-amber-700 bg-amber-50 px-2 py-0.5 rounded-full border border-amber-200">
              <Loader2 className="w-3 h-3 animate-spin" />
              {t('tutor.gradingBadge', 'Đang chấm...')}
            </span>
          )}
        </div>

        {canAnalyzeLatestTurn && onAnalyzeLatestTurn && (
          <button
            type="button"
            disabled={isAnalyzing}
            onClick={onAnalyzeLatestTurn}
            className="text-[11px] font-bold text-emerald-700 hover:underline inline-flex items-center gap-1 cursor-pointer disabled:opacity-50"
          >
            <Wand2 className="w-3 h-3" />
            {t('tutor.reanalyzeBtn', 'Chấm lại')}
          </button>
        )}
      </div>

      <div className="flex-1 overflow-y-auto space-y-3 pt-3 pr-1">
        {corrections.map((item) => {
          const normOriginal = normalizeSentence(item.originalSentence);
          const normCorrected = normalizeSentence(item.correctedSentence);
          const improvedText = (item.improvedText || item.naturalAlternative || '').trim();
          const normImproved = normalizeSentence(improvedText);

          const hasCorrected =
            Boolean(item.correctedSentence?.trim()) && normCorrected !== normOriginal;
          const hasImproved =
            Boolean(improvedText) &&
            normImproved !== normOriginal &&
            normImproved !== normCorrected;

          const isReplayingOriginal = replayingTurnId === item.turnId;

          return (
            <div
              key={`${item.turnId}_${item.id || item.timestamp}`}
              className="p-3.5 rounded-2xl bg-amber-50/45 border border-amber-200/80 space-y-2.5 text-xs shadow-2xs"
            >
              <div className="flex items-center justify-between gap-2">
                <div className="flex items-center gap-1.5 flex-wrap">
                  {renderStatusBadge(item)}
                  {item.errorCategory && item.status && (
                    <span className="text-[10px] text-slate-500 font-medium">
                      ({item.errorCategory})
                    </span>
                  )}
                </div>
                <span className="text-[10px] text-slate-400 font-medium">
                  {new Date(item.timestamp).toLocaleTimeString(dateLocale)}
                </span>
              </div>

              {/* Analysis failed banner with reanalyze action */}
              {item.status === 'analysis_failed' && (
                <div className="flex items-center justify-between gap-2 p-2.5 rounded-xl bg-amber-100/70 border border-amber-300/80 text-amber-900">
                  <div className="flex items-center gap-1.5 text-xs font-semibold">
                    <AlertCircle className="w-4 h-4 text-amber-700 shrink-0" />
                    <span>{t('tutor.statusAnalysisFailed', 'Phân tích lỗi')}</span>
                  </div>
                  {onReanalyzeTurn && (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => onReanalyzeTurn(item.turnId, item.originalSentence)}
                      className="text-[11px] font-bold text-amber-900 border-amber-300 bg-white/80 hover:bg-white h-7 px-2.5"
                    >
                      <Wand2 className="w-3 h-3 text-amber-700" />
                      {t('tutor.reanalyzeAction', 'Phân tích lại')}
                    </Button>
                  )}
                </div>
              )}

              {/* Original Utterance */}
              <div className="flex items-start justify-between gap-2 bg-rose-50/70 border border-rose-200/70 rounded-xl px-3 py-2">
                <div className="space-y-0.5">
                  <span className="text-[10px] font-bold uppercase tracking-wider text-rose-600 block">
                    {t('tutor.originalSentence', 'Câu gốc của bạn')}
                  </span>
                  <p
                    className={
                      !hasCorrected
                        ? 'text-slate-700 font-medium leading-relaxed'
                        : 'text-rose-700 line-through font-medium leading-relaxed'
                    }
                  >
                    {item.originalSentence}
                  </p>
                </div>
                {onReplayUserTurnAudio && (
                  <button
                    type="button"
                    onClick={() => {
                      onReplayUserTurnAudio(item.turnId, item.originalSentence);
                    }}
                    title={
                      isReplayingOriginal
                        ? t('tutor.stopReplayShort', 'Dừng phát lại')
                        : t(
                            'tutor.replayUserAudioShort',
                            'Nghe lại câu gốc (Giọng thu âm của bạn)'
                          )
                    }
                    className="p-1 rounded-lg text-rose-600 hover:bg-rose-100 shrink-0 cursor-pointer"
                  >
                    {isReplayingOriginal ? (
                      <Square className="w-3.5 h-3.5 fill-current animate-pulse" />
                    ) : (
                      <Volume2 className="w-3.5 h-3.5" />
                    )}
                  </button>
                )}
              </div>

              {/* Corrected Sentence with Issues Breakdown (hidden if identical to original) */}
              {hasCorrected && (
                <div className="flex flex-col gap-2 bg-emerald-50/80 border border-emerald-200 rounded-xl px-3 py-2">
                  <div className="flex items-start justify-between gap-2">
                    <div className="space-y-0.5">
                      <span className="text-[10px] font-bold uppercase tracking-wider text-emerald-700 flex items-center gap-1">
                        <ArrowRight className="w-3 h-3" />
                        {t('tutor.correctedSentence', 'Câu chuẩn / Nâng cấp diễn đạt')}
                      </span>
                      <p className="text-emerald-900 font-bold leading-relaxed">
                        {item.correctedSentence}
                      </p>
                    </div>
                    <button
                      type="button"
                      disabled={loadingKey === `${item.id || item.correctedSentence}_corrected`}
                      onClick={() =>
                        void playFeedbackAudio(item.id, 'corrected', item.correctedSentence)
                      }
                      title={t('tutor.listenCorrectedSpeech', 'Nghe phát âm mẫu câu đã sửa')}
                      className="p-1 rounded-lg text-emerald-700 hover:bg-emerald-100 shrink-0 cursor-pointer disabled:opacity-50"
                    >
                      {loadingKey === `${item.id || item.correctedSentence}_corrected` ? (
                        <Loader2 className="w-3.5 h-3.5 animate-spin" />
                      ) : (
                        <Volume2 className="w-3.5 h-3.5" />
                      )}
                    </button>
                  </div>

                  {item.issues && item.issues.length > 0 && (
                    <div className="mt-1 space-y-1.5 pt-1.5 border-t border-emerald-200/70">
                      <span className="text-[10px] font-bold text-emerald-800 uppercase tracking-wider block">
                        {t('tutor.issuesTitle', 'Chi tiết cần sửa:')}
                      </span>
                      <div className="space-y-1">
                        {item.issues.map((issue, idx) => (
                          <div
                            key={idx}
                            className="flex flex-wrap items-center gap-1.5 text-[11px] bg-white/70 px-2 py-1 rounded-lg border border-emerald-100"
                          >
                            <span className="line-through text-rose-600 font-medium">
                              {issue.original}
                            </span>
                            <ArrowRight className="w-3 h-3 text-slate-400 shrink-0" />
                            <span className="text-emerald-700 font-bold">{issue.fix}</span>
                            <span className="text-slate-500 text-[10px]">
                              ({issue.category}
                              {issue.explanationVi ? `: ${issue.explanationVi}` : ''})
                            </span>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              )}

              {/* Improved Sentence (hidden if identical to original or corrected) */}
              {hasImproved && (
                <div className="flex items-start justify-between gap-2 bg-sky-50/80 border border-sky-200 rounded-xl px-3 py-2">
                  <div className="space-y-0.5">
                    <span className="text-[10px] font-bold uppercase tracking-wider text-sky-700 flex items-center gap-1">
                      <Sparkles className="w-3 h-3" />
                      {t('tutor.improvedSentence', 'Câu tự nhiên hơn')}
                    </span>
                    <p className="text-sky-950 font-bold leading-relaxed">{improvedText}</p>
                  </div>
                  <button
                    type="button"
                    disabled={loadingKey === `${item.id || improvedText}_improved`}
                    onClick={() => void playFeedbackAudio(item.id, 'improved', improvedText)}
                    title={t('tutor.listenCorrectedSpeech', 'Nghe phát âm mẫu câu đã sửa')}
                    className="p-1 rounded-lg text-sky-700 hover:bg-sky-100 shrink-0 cursor-pointer disabled:opacity-50"
                  >
                    {loadingKey === `${item.id || improvedText}_improved` ? (
                      <Loader2 className="w-3.5 h-3.5 animate-spin" />
                    ) : (
                      <Volume2 className="w-3.5 h-3.5" />
                    )}
                  </button>
                </div>
              )}

              {(item.explanationVi || item.overallTipVi) && (
                <p className="text-slate-700 leading-relaxed bg-white/75 rounded-xl px-3 py-2 border border-slate-200/70">
                  <strong className="text-slate-900">
                    {t('tutor.explanationPrefix', 'Giải thích:')}
                  </strong>{' '}
                  {item.explanationVi || item.overallTipVi}
                </p>
              )}

              {item.cefrTip && (
                <div className="flex items-start gap-1.5 text-[11px] text-amber-900 bg-amber-100/60 border border-amber-200/70 rounded-xl px-2.5 py-2">
                  <Lightbulb className="w-3.5 h-3.5 text-amber-600 shrink-0 mt-0.5" />
                  <span className="font-medium leading-relaxed">{item.cefrTip}</span>
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
};
