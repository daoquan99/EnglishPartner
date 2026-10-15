import React, { useState } from 'react';
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
  const [activeTab, setActiveTab] = useState<'pronunciation' | 'flashcard'>('pronunciation');

  if (!card) return null;

  const isWordTarget = targetType === 'word';
  const hasExample = !isWordTarget && Boolean(selectedExample);

  const title = isWordTarget
    ? `Lịch sử luyện tập: Từ "${card.word}"`
    : selectedExample
      ? `Lịch sử luyện tập: Câu ví dụ #${selectedExampleIndex + 1}`
      : 'Lịch sử luyện tập: Câu ví dụ';

  const description = isWordTarget
    ? `Nghĩa: ${card.meaningVi || card.definitionEn} · Trình độ CEFR ${card.cefrLevel}`
    : selectedExample
      ? `"${selectedExample.sentenceEn}"`
      : 'Từ này hiện chưa có câu ví dụ để xem lịch sử.';

  // Format date helper with user's local timezone
  const formatLocalDate = (isoString?: string | null): string => {
    if (!isoString) return 'Chưa có dữ liệu';
    try {
      const date = new Date(isoString);
      return new Intl.DateTimeFormat('vi-VN', {
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
        icon={<History className="w-5 h-5 text-emerald-600" />}
        title={title}
        description={description}
        footer={
          <div className="flex items-center justify-between gap-3 w-full">
            <span className="text-[11px] text-slate-500 font-medium">
              ID Thẻ: <span className="font-mono text-slate-700">{card.vocabularyId.slice(0, 8)}...</span> · Nghĩa:{' '}
              <span className="font-mono text-slate-700">{card.senseId.slice(0, 8)}...</span>
            </span>
            <Button variant="outline" size="sm" onClick={onClose}>
              Đóng hộp thoại
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
                <Mic className="w-3.5 h-3.5" />
                Lịch sử phát âm ({card.word})
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
                <BookOpen className="w-3.5 h-3.5" />
                Lịch sử học từ (Flashcard SM-2)
              </button>
            </div>
          ) : (
            <div className="flex border-b border-slate-200 gap-2">
              <div className="flex items-center gap-1.5 pb-2.5 px-3 font-bold border-b-2 border-emerald-600 text-emerald-700 text-xs">
                <Mic className="w-3.5 h-3.5" />
                Lịch sử phát âm câu ví dụ
              </div>
            </div>
          )}

          {/* If target is example and there are no examples, show empty message */}
          {!isWordTarget && !hasExample ? (
            <div className="p-6 text-center rounded-2xl bg-slate-50 border border-slate-200 space-y-2">
              <AlertCircle className="w-8 h-8 text-amber-500 mx-auto" />
              <div className="text-sm font-bold text-slate-800">
                Từ này chưa có câu ví dụ
              </div>
              <p className="text-xs text-slate-500 max-w-md mx-auto">
                Không thể xem lịch sử phát âm câu ví dụ vì từ vựng này chưa có dữ liệu ví dụ. Vui lòng chọn chế độ Từ đơn để xem lịch sử của từ.
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
                    Lịch sử đang chờ Backend hỗ trợ
                  </div>
                  <Badge variant="amber" className="ml-auto text-[10px]">
                    Contract Pending
                  </Badge>
                </div>
                <p className="text-xs text-amber-900 leading-relaxed">
                  {activeTab === 'pronunciation'
                    ? 'Endpoint phát âm `/api/pronunciation/assessments/history` hiện tại ở Backend chỉ nhận vocabularyId chung, chưa có bộ lọc theo nghĩa (senseId), câu ví dụ cụ thể (targetId) hoặc phân trang người dùng cá nhân.'
                    : 'Endpoint học từ `/api/learning/flashcards/history` hiện tại ở Backend chỉ lấy dữ liệu cố định của default-learner và chưa lọc theo từng từ/nghĩa.'}
                </p>
                <div className="pt-1.5 border-t border-amber-200/80 text-[11px] text-amber-800 flex flex-wrap gap-x-4 gap-y-1">
                  <span>
                    🎯 Mục tiêu:{' '}
                    <strong>
                      {isWordTarget
                        ? `Từ "${card.word}" (Word)`
                        : `Câu ví dụ #${selectedExampleIndex + 1} (Sentence)`}
                    </strong>
                  </span>
                  <span>
                    📋 Quy chuẩn:{' '}
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
                        Kết quả chấm phát âm vừa thực hiện trong phiên
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
                            ? 'Không thể chấm điểm bản ghi này'
                            : `Tổng điểm: ${recentAssessmentResult.overallScore}/100`}
                        </div>
                        <div className="text-[10px] text-slate-500">
                          Model: {recentAssessmentResult.modelCodeUsed} · Ngữ điệu:{' '}
                          {recentAssessmentResult.accent || 'US'} · Trạng thái: {recentAssessmentResult.status}
                        </div>
                      </div>
                    </div>
                    <Badge variant="emerald">Phiên hiện tại</Badge>
                  </div>

                  {/* Transcript */}
                  {recentAssessmentResult.recognizedTranscript && (
                    <div className="p-2.5 rounded-xl bg-white border border-slate-200 text-xs">
                      <span className="font-bold text-slate-700">Văn bản nhận diện AI:</span>{' '}
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
                              Trọng số: {c.weight}%
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
                          <strong className="text-slate-800">Nhận xét âm vị:</strong>{' '}
                          <span className="text-slate-600">
                            {recentAssessmentResult.phonemeAndStressFeedbackVi}
                          </span>
                        </p>
                      )}
                      {recentAssessmentResult.coachingSuggestionVi && (
                        <p>
                          <strong className="text-emerald-800">Gợi ý luyện tập:</strong>{' '}
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
                      Thông số ôn tập Flashcard (Thuật toán SM-2 hiện tại)
                    </div>

                    <div className="grid grid-cols-2 sm:grid-cols-4 gap-2.5 text-center">
                      <div className="p-2.5 rounded-xl bg-white border border-slate-200">
                        <div className="text-[10px] text-slate-500 font-bold uppercase">
                          Trạng thái
                        </div>
                        <div className="text-sm font-extrabold text-slate-900 mt-0.5">
                          {card.learningStatus || 'Learning'}
                        </div>
                      </div>
                      <div className="p-2.5 rounded-xl bg-white border border-slate-200">
                        <div className="text-[10px] text-slate-500 font-bold uppercase">
                          Lần lặp lại
                        </div>
                        <div className="text-sm font-extrabold text-slate-900 mt-0.5">
                          {card.repetitionCount}
                        </div>
                      </div>
                      <div className="p-2.5 rounded-xl bg-white border border-slate-200">
                        <div className="text-[10px] text-slate-500 font-bold uppercase">
                          Khoảng cách ôn
                        </div>
                        <div className="text-sm font-extrabold text-slate-900 mt-0.5">
                          {card.intervalDays} ngày
                        </div>
                      </div>
                      <div className="p-2.5 rounded-xl bg-white border border-slate-200">
                        <div className="text-[10px] text-slate-500 font-bold uppercase">
                          Hệ số dễ (Ease)
                        </div>
                        <div className="text-sm font-extrabold text-emerald-700 mt-0.5">
                          {card.easeFactor}
                        </div>
                      </div>
                    </div>

                    <div className="p-3 rounded-xl bg-white border border-slate-200 flex items-center justify-between text-xs">
                      <span className="text-slate-600">Thời gian ôn tập tiếp theo:</span>
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
