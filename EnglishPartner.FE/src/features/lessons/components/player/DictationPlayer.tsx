import React, { useState, useRef, useEffect } from 'react';
import { Play, Pause, RotateCcw, Check, Sparkles, Loader2, AlertCircle, ArrowRight } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { LessonAudioButton } from './LessonAudioButton';
import { useRequestDictationAiReviewMutation } from '../../hooks/useLessonQueries';
import { useTutorSessionStore } from '@/features/tutor';
import type {
  ExerciseAnswerResultDto,
  SubmittedAnswerSummaryDto,
  DictationAiReviewDto,
} from '../../types/lessons.types';

interface DictationPlayerProps {
  content: {
    audioUrl?: string;
    speed?: number;
    hint?: string;
  };
  onSubmit: (payload: { transcribedText: string }) => void;
  isSubmitting?: boolean;
  isSubmitted?: boolean;
  initialTranscribedText?: string;
  attemptId?: string;
  answerId?: string;
  result?: ExerciseAnswerResultDto | SubmittedAnswerSummaryDto | null;
}

export const DictationPlayer: React.FC<DictationPlayerProps> = ({
  content,
  onSubmit,
  isSubmitting = false,
  isSubmitted = false,
  initialTranscribedText = '',
  attemptId,
  answerId,
  result,
}) => {
  const [transcribedText, setTranscribedText] = useState(initialTranscribedText);
  const [isPlaying, setIsPlaying] = useState(false);
  const audioRef = useRef<HTMLAudioElement | null>(null);

  const apiKey = useTutorSessionStore((s) => s.apiKey);
  const assessmentModelCode = useTutorSessionStore((s) => s.assessmentModelCode);

  const requestReviewMutation = useRequestDictationAiReviewMutation();
  const [localReview, setLocalReview] = useState<DictationAiReviewDto | null>(
    result?.aiReview ?? null
  );
  const [reviewError, setReviewError] = useState<string | null>(null);

  useEffect(() => {
    if (result?.aiReview) {
      setLocalReview(result.aiReview);
    }
  }, [result?.aiReview]);

  useEffect(() => {
    if (initialTranscribedText) {
      setTranscribedText(initialTranscribedText);
    }
  }, [initialTranscribedText]);

  const togglePlay = () => {
    if (!audioRef.current) return;
    if (isPlaying) {
      audioRef.current.pause();
      setIsPlaying(false);
    } else {
      audioRef.current.playbackRate = content.speed ?? 1.0;
      void audioRef.current.play();
      setIsPlaying(true);
    }
  };

  const handleReplay = () => {
    if (!audioRef.current) return;
    audioRef.current.currentTime = 0;
    audioRef.current.playbackRate = content.speed ?? 1.0;
    void audioRef.current.play();
    setIsPlaying(true);
  };

  const handleRequestReview = async () => {
    if (!attemptId || !answerId) return;
    setReviewError(null);
    try {
      const rev = await requestReviewMutation.mutateAsync({
        attemptId,
        answerId,
        payload: {
          aiConfig: {
            apiKey: apiKey || null,
            assessmentModelCode: assessmentModelCode || null,
          },
        },
      });
      setLocalReview(rev);
    } catch (err: any) {
      setReviewError(
        err?.response?.data?.error ||
          err?.response?.data?.detail ||
          'Không thể kết nối AI để nhận xét bài chép. Vui lòng kiểm tra API Key và thử lại.'
      );
    }
  };

  const effectiveAnswerId = answerId ?? result?.answerId;

  return (
    <div className="space-y-6 max-w-xl mx-auto">
      {/* Audio Playback Controls */}
      <div className="bg-slate-900 text-white p-6 rounded-3xl text-center space-y-4 shadow-md">
        <span className="text-xs uppercase font-extrabold tracking-wider text-emerald-400">
          Nghe chép chính tả (Dictation)
        </span>

        {content.audioUrl ? (
          <audio
            ref={audioRef}
            src={content.audioUrl}
            onEnded={() => setIsPlaying(false)}
            className="hidden"
          />
        ) : null}

        {!content.audioUrl && <LessonAudioButton label="Nghe câu cần chép" />}
        {content.audioUrl && (
          <div className="flex items-center justify-center gap-4 py-1">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={handleReplay}
              className="rounded-full text-white border-slate-700 hover:bg-slate-800"
            >
              <RotateCcw className="w-4 h-4" />
            </Button>

            <button
              type="button"
              onClick={togglePlay}
              disabled={!content.audioUrl}
              className="w-14 h-14 rounded-full bg-emerald-500 hover:bg-emerald-400 text-slate-950 flex items-center justify-center shadow-lg transition-transform active:scale-95 cursor-pointer disabled:opacity-50"
            >
              {isPlaying ? (
                <Pause className="w-6 h-6 fill-current" />
              ) : (
                <Play className="w-6 h-6 fill-current ml-0.5" />
              )}
            </button>
          </div>
        )}

        <p className="text-xs text-slate-400">
          Nghe kỹ từng từ và gõ lại chính xác những gì bạn nghe được vào ô bên dưới.
        </p>
      </div>

      {/* Transcription Input */}
      <div className="space-y-2">
        <label className="text-xs font-bold text-slate-700 block">
          Nội dung bạn nghe được:
        </label>
        <textarea
          rows={3}
          value={transcribedText}
          onChange={(e) => setTranscribedText(e.target.value)}
          disabled={isSubmitted}
          placeholder="Gõ văn bản nghe được..."
          className="w-full text-base font-medium rounded-2xl border border-slate-200 bg-white p-4 focus:outline-none focus:ring-2 focus:ring-emerald-500 shadow-xs"
        />
      </div>

      {/* Submit Action */}
      {!isSubmitted && (
        <div className="pt-2">
          <Button
            type="button"
            variant="primary"
            onClick={() => onSubmit({ transcribedText: transcribedText.trim() })}
            disabled={transcribedText.trim().length === 0 || isSubmitting}
            className="w-full gap-2 py-3 rounded-2xl"
          >
            <Check className="w-4 h-4" />
            {isSubmitting ? 'Đang chấm điểm Levenshtein...' : 'Xác nhận nộp bài chép'}
          </Button>
        </div>
      )}

      {/* AI Formative Review Section for Submitted Answer */}
      {isSubmitted && effectiveAnswerId && (
        <div className="pt-2 space-y-3">
          {!localReview && (
            <div className="p-4 rounded-2xl border border-purple-200 bg-purple-50/60 text-center space-y-2">
              <div className="text-xs text-purple-900 font-semibold">
                Bài chép đã được chấm điểm tự động. Bạn có thể yêu cầu AI phân tích lỗi ngữ âm, hiện tượng nuốt âm/nối âm để cải thiện kỹ năng nghe.
              </div>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={handleRequestReview}
                disabled={requestReviewMutation.isPending}
                className="gap-2 bg-purple-600 hover:bg-purple-700 text-white border-transparent rounded-xl"
              >
                {requestReviewMutation.isPending ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin" />
                    AI đang phân tích bài chép...
                  </>
                ) : (
                  <>
                    <Sparkles className="w-4 h-4 text-amber-300" />
                    AI nhận xét bài chép
                  </>
                )}
              </Button>
              {reviewError && (
                <div className="flex items-center justify-center gap-1.5 text-xs font-semibold text-rose-600 pt-1">
                  <AlertCircle className="w-4 h-4 shrink-0" />
                  <span>{reviewError}</span>
                </div>
              )}
            </div>
          )}

          {localReview && (
            <div className="p-5 rounded-3xl border border-purple-200 bg-gradient-to-b from-purple-50/70 to-white space-y-4 shadow-xs text-left">
              <div className="flex items-center justify-between border-b border-purple-100 pb-3">
                <div className="flex items-center gap-2">
                  <Sparkles className="w-4 h-4 text-purple-600" />
                  <h4 className="text-sm font-bold text-slate-900">Nhận xét chi tiết từ AI Tutor</h4>
                </div>
                <Badge variant="violet" className="text-[10px]">
                  Formative Review
                </Badge>
              </div>

              {localReview.feedback && (
                <p className="text-xs text-slate-700 leading-relaxed font-medium">
                  {localReview.feedback}
                </p>
              )}

              {/* Missing & Extra Words */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-1">
                {localReview.missingWords && localReview.missingWords.length > 0 && (
                  <div className="p-3 bg-amber-50/80 rounded-2xl border border-amber-200 space-y-1.5">
                    <span className="text-[11px] font-bold text-amber-900 block">
                      Từ bị bỏ sót (Missing):
                    </span>
                    <div className="flex flex-wrap gap-1.5">
                      {localReview.missingWords.map((w, idx) => (
                        <span
                          key={idx}
                          className="px-2 py-0.5 rounded-lg bg-amber-100 text-amber-900 text-xs font-bold"
                        >
                          {w}
                        </span>
                      ))}
                    </div>
                  </div>
                )}

                {localReview.extraWords && localReview.extraWords.length > 0 && (
                  <div className="p-3 bg-rose-50/80 rounded-2xl border border-rose-200 space-y-1.5">
                    <span className="text-[11px] font-bold text-rose-900 block">
                      Từ thừa (Extra):
                    </span>
                    <div className="flex flex-wrap gap-1.5">
                      {localReview.extraWords.map((w, idx) => (
                        <span
                          key={idx}
                          className="px-2 py-0.5 rounded-lg bg-rose-100 text-rose-900 text-xs font-bold"
                        >
                          {w}
                        </span>
                      ))}
                    </div>
                  </div>
                )}
              </div>

              {/* Substituted Words */}
              {localReview.wordSubstitutions && localReview.wordSubstitutions.length > 0 && (
                <div className="p-3 bg-slate-50 rounded-2xl border border-slate-200 space-y-2">
                  <span className="text-[11px] font-bold text-slate-700 block">
                    Cặp từ nhầm lẫn / đồng âm (Substitutions):
                  </span>
                  <div className="space-y-1">
                    {localReview.wordSubstitutions.map((sub, idx) => (
                      <div
                        key={idx}
                        className="flex items-center gap-2 text-xs font-medium bg-white px-2.5 py-1.5 rounded-xl border border-slate-200"
                      >
                        <span className="text-emerald-700 font-bold">Đúng: &quot;{sub.expected}&quot;</span>
                        <ArrowRight className="w-3.5 h-3.5 text-slate-400" />
                        <span className="text-rose-600 font-bold">Bạn nghe thành: &quot;{sub.actual}&quot;</span>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* Vietnamese Phonetics / Connected Speech Explanations */}
              {localReview.explanationsVi && (
                <div className="p-3.5 bg-blue-50/60 rounded-2xl border border-blue-200 space-y-1">
                  <span className="text-[11px] font-bold text-blue-900 block">
                    Giải thích hiện tượng ngữ âm tiếng Anh:
                  </span>
                  <p className="text-xs text-blue-950 leading-relaxed font-normal whitespace-pre-wrap">
                    {localReview.explanationsVi}
                  </p>
                </div>
              )}

              {/* Actionable Tips */}
              {localReview.actionableImprovements &&
                localReview.actionableImprovements.length > 0 && (
                  <div className="space-y-1.5 pt-1">
                    <span className="text-[11px] font-bold text-slate-700 block">
                      Gợi ý luyện tập thêm:
                    </span>
                    <ul className="list-disc list-inside text-xs text-slate-600 space-y-1">
                      {localReview.actionableImprovements.map((tip, idx) => (
                        <li key={idx}>{tip}</li>
                      ))}
                    </ul>
                  </div>
                )}
            </div>
          )}
        </div>
      )}
    </div>
  );
};
