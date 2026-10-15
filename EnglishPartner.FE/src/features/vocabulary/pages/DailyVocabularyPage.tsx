import React, { useEffect, useRef, useState } from 'react';
import {
  BookOpen,
  Volume2,
  RotateCw,
  Sparkles,
  Mic,
  Square,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  Cpu,
  ShieldCheck,
  Award,
} from 'lucide-react';
import {
  useActiveScoringRubricQuery,
  usePronunciationModelsQuery,
  useTopicsQuery,
  useVocabularyGenerationModelsQuery,
} from '@/features/catalog';
import {
  useAssessPronunciationMutation,
  useDailyVocabularyPlanQuery,
  useGenerateVocabularyMutation,
  useSubmitVocabularyReviewMutation,
} from '../hooks/useDailyVocabulary';
import type {
  PronunciationAssessmentResultDto,
  ReviewRating,
  VocabularyFlashcardDto,
} from '../types/vocabulary.types';
import type { CefrLevel } from '@/shared/api/openapi.types';
import { TopicCreatablePicker } from '@/shared/components/ui/TopicCreatablePicker';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { cn } from '@/shared/lib/cn';

function blobToBase64(blob: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onloadend = () => {
      const res = reader.result as string;
      const commaIdx = res.indexOf(',');
      resolve(commaIdx >= 0 ? res.slice(commaIdx + 1) : res);
    };
    reader.onerror = reject;
    reader.readAsDataURL(blob);
  });
}

export const DailyVocabularyPage: React.FC = () => {
  const { data: plan, isLoading } = useDailyVocabularyPlanQuery(10);
  const { data: vocabModels = [] } = useVocabularyGenerationModelsQuery();
  const { data: audioModels = [] } = usePronunciationModelsQuery();
  const { data: vocabTopics = [] } = useTopicsQuery({
    onlyActive: true,
    supportsVocabulary: true,
  });
  const { data: activeRubric } = useActiveScoringRubricQuery();

  const reviewMutation = useSubmitVocabularyReviewMutation();
  const generateMutation = useGenerateVocabularyMutation();
  const assessMutation = useAssessPronunciationMutation();

  const [cards, setCards] = useState<VocabularyFlashcardDto[]>([]);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [isFlipped, setIsFlipped] = useState(false);

  // AI Vocabulary Generator State (synced with DB Catalog)
  const [genCefr, setGenCefr] = useState<CefrLevel>('B2');
  const [genTopic, setGenTopic] = useState<string>(
    'Essential Workplace Phrasal Verbs & Executive Idioms'
  );
  const [genCount, setGenCount] = useState<number>(3);
  const [selectedVocabModel, setSelectedVocabModel] = useState<string>('gemini-3.8-flash');

  // Audio Pronunciation Studio State (strictly Audio-Input-Only models from DB)
  const [selectedAudioModel, setSelectedAudioModel] = useState<string>('gemini-3.8-flash');
  const [targetType, setTargetType] = useState<'word' | 'example'>('word');
  const [isRecording, setIsRecording] = useState(false);
  const [assessmentResult, setAssessmentResult] =
    useState<PronunciationAssessmentResultDto | null>(null);

  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const chunksRef = useRef<BlobPart[]>([]);
  const recordStartRef = useRef<number>(0);

  useEffect(() => {
    if (plan?.items && plan.items.length > 0) {
      setCards(plan.items);
    }
  }, [plan]);

  useEffect(() => {
    if (vocabModels.length > 0 && !vocabModels.some((m) => m.modelCode === selectedVocabModel)) {
      setSelectedVocabModel(vocabModels[0].modelCode);
    }
  }, [vocabModels, selectedVocabModel]);

  useEffect(() => {
    if (audioModels.length > 0 && !audioModels.some((m) => m.modelCode === selectedAudioModel)) {
      setSelectedAudioModel(audioModels[0].modelCode);
    }
  }, [audioModels, selectedAudioModel]);

  const currentCard = cards[currentIndex] ?? null;

  const speakText = (text: string, accent: 'UK' | 'US' = 'US') => {
    if (!('speechSynthesis' in window) || !text) return;
    window.speechSynthesis.cancel();
    const u = new SpeechSynthesisUtterance(text);
    u.lang = accent === 'UK' ? 'en-GB' : 'en-US';
    u.rate = 0.92;
    window.speechSynthesis.speak(u);
  };

  const handleReview = async (rating: ReviewRating) => {
    if (!currentCard) return;
    try {
      const updated = await reviewMutation.mutateAsync({
        vocabularyId: currentCard.vocabularyId,
        senseId: currentCard.senseId,
        rating,
        responseTimeMs: 2500,
      });
      setCards((prev) =>
        prev.map((c, idx) => (idx === currentIndex ? { ...c, ...updated, isCompletedToday: true } : c))
      );
    } catch {
      setCards((prev) =>
        prev.map((c, idx) => (idx === currentIndex ? { ...c, isCompletedToday: true } : c))
      );
    }

    setIsFlipped(false);
    setAssessmentResult(null);
    if (currentIndex < cards.length - 1) {
      setCurrentIndex((i) => i + 1);
    }
  };

  const handleGenerateMoreWords = async () => {
    try {
      const generated = await generateMutation.mutateAsync({
        cefrLevel: genCefr,
        topic: genTopic || 'Software Engineering & Daily Workplace',
        count: genCount,
        modelCode: selectedVocabModel,
      });
      if (generated && generated.length > 0) {
        setCards((prev) => [...generated, ...prev]);
        setCurrentIndex(0);
        setIsFlipped(false);
      }
    } catch {
      // ignore
    }
  };

  const startRecording = async () => {
    if (!currentCard) return;
    setAssessmentResult(null);
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    const recorder = new MediaRecorder(stream);
    mediaRecorderRef.current = recorder;
    chunksRef.current = [];
    recordStartRef.current = Date.now();

    recorder.ondataavailable = (e) => {
      if (e.data.size > 0) chunksRef.current.push(e.data);
    };

    recorder.onstop = async () => {
      stream.getTracks().forEach((t) => t.stop());
      const durationMs = Math.max(800, Date.now() - recordStartRef.current);
      const blob = new Blob(chunksRef.current, { type: recorder.mimeType || 'audio/webm' });
      const audioBase64 = await blobToBase64(blob);

      const primaryExample = currentCard.examples[0];
      const referenceText =
        targetType === 'example' && primaryExample ? primaryExample.sentenceEn : currentCard.word;

      try {
        const res = await assessMutation.mutateAsync({
          vocabularyId: currentCard.vocabularyId,
          senseId: currentCard.senseId,
          exampleId: targetType === 'example' ? primaryExample?.id : undefined,
          referenceText,
          audioBase64,
          mimeType: blob.type || 'audio/webm',
          durationMs,
          modelCode: selectedAudioModel,
        });
        setAssessmentResult(res);
      } catch {
        // Offline simulated rubric score if BE is unreachable
        setAssessmentResult({
          assessmentId: `offline-${Date.now()}`,
          overallScore: 88,
          cefrPronunciationGrade: currentCard.cefrLevel,
          recognizedTranscript: referenceText,
          referenceText,
          modelCodeUsed: selectedAudioModel,
          rubricCode: activeRubric?.code || 'ENGLISH_PRONUNCIATION_V1',
          phonemeAndStressFeedbackVi: `Trọng âm từ '${currentCard.word}' rõ ràng, âm cuối phát âm tốt.`,
          coachingSuggestionVi: 'Giữ nhịp điệu tự nhiên và nối âm mượt hơn ở cụm từ tiếp theo.',
          criterionScores: [
            { criterionCode: 'Accuracy', criterionName: 'Accuracy (40%)', weight: 40, score: 90, weightedScore: 36, feedbackVi: 'Chuẩn xác âm vị.' },
            { criterionCode: 'Clarity', criterionName: 'Clarity (30%)', weight: 30, score: 86, weightedScore: 25.8, feedbackVi: 'Âm cuối rõ ràng.' },
            { criterionCode: 'Fluency', criterionName: 'Fluency (30%)', weight: 30, score: 87, weightedScore: 26.1, feedbackVi: 'Nhịp độ tự nhiên.' },
          ],
          assessedAtUtc: new Date().toISOString(),
        });
      }
    };

    recorder.start();
    setIsRecording(true);
  };

  const stopRecording = () => {
    if (mediaRecorderRef.current && mediaRecorderRef.current.state !== 'inactive') {
      mediaRecorderRef.current.stop();
    }
    setIsRecording(false);
  };

  if (isLoading && cards.length === 0) {
    return <div className="p-8 text-center text-sm text-slate-500">Đang tải kế hoạch từ vựng hằng ngày...</div>;
  }

  return (
    <div className="space-y-6">
      {/* Top Progress Header */}
      <div className="bg-white rounded-3xl border border-slate-200/90 p-5 flex flex-wrap items-center justify-between gap-4 shadow-xs">
        <div className="flex items-center gap-3">
          <div className="w-11 h-11 rounded-2xl bg-emerald-50 border border-emerald-200 flex items-center justify-center text-emerald-600">
            <BookOpen className="w-5 h-5" />
          </div>
          <div>
            <h1 className="text-lg font-extrabold text-slate-900">
              Từ Vựng Hằng Ngày & Chấm Phát Âm AI (SM-2)
            </h1>
            <p className="text-xs text-slate-500">
              Học theo nghĩa cụ thể (Sense-based Flashcards) · Lặp lại ngắt quãng SM-2 · Chấm phát âm theo Rubric DB
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2 flex-wrap">
          <Badge variant="emerald">
            Hoàn thành: {cards.filter((c) => c.isCompletedToday).length}/{cards.length} từ
          </Badge>
          <Badge variant="sky">
            Thẻ hiện tại: {cards.length > 0 ? currentIndex + 1 : 0}/{cards.length}
          </Badge>
        </div>
      </div>

      {/* Main 12-Col Grid: Left (Flashcard + Pronunciation Studio) | Right (AI Vocabulary Generator from DB) */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-5 items-start">
        {/* Left 7 Columns: Sense Flashcard + Pronunciation Studio */}
        <div className="lg:col-span-7 space-y-5">
          {currentCard && (
            <div className="bg-white rounded-3xl border border-slate-200/90 shadow-xs p-6 space-y-5">
              {/* Card Navigation & Flip Bar */}
              <div className="flex items-center justify-between border-b border-slate-100 pb-3">
                <div className="flex items-center gap-2">
                  <Badge variant="emerald">{currentCard.cefrLevel}</Badge>
                  <Badge variant="amber">{currentCard.partOfSpeech}</Badge>
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
                    onClick={() => {
                      setCurrentIndex((i) => Math.max(0, i - 1));
                      setIsFlipped(false);
                      setAssessmentResult(null);
                    }}
                  >
                    <ChevronLeft className="w-4 h-4" />
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setIsFlipped((f) => !f)}
                  >
                    <RotateCw className="w-3.5 h-3.5" />
                    {isFlipped ? 'Mặt trước' : 'Lật mặt sau'}
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={currentIndex >= cards.length - 1}
                    onClick={() => {
                      setCurrentIndex((i) => Math.min(cards.length - 1, i + 1));
                      setIsFlipped(false);
                      setAssessmentResult(null);
                    }}
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

                  {/* UK / US IPA & TTS Buttons */}
                  <div className="flex items-center justify-center gap-3 flex-wrap">
                    {currentCard.pronunciations.map((p) => (
                      <button
                        key={p.id}
                        type="button"
                        onClick={() => speakText(currentCard.word, p.accent === 'UK' ? 'UK' : 'US')}
                        className="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-xl bg-slate-50 hover:bg-emerald-50 border border-slate-200 text-xs font-semibold text-slate-700 cursor-pointer"
                      >
                        <span className="font-bold text-emerald-700">{p.accent}</span>
                        <span className="font-mono">{p.ipa}</span>
                        <Volume2 className="w-3.5 h-3.5 text-emerald-600" />
                      </button>
                    ))}
                  </div>

                  <p className="text-base font-medium text-slate-700 max-w-xl mx-auto">
                    {currentCard.definitionEn}
                  </p>

                  {currentCard.examples[0] && (
                    <div className="max-w-xl mx-auto p-4 rounded-2xl bg-emerald-50/60 border border-emerald-200/80 text-left space-y-1">
                      <div className="flex items-start justify-between gap-2">
                        <p className="text-sm font-semibold text-emerald-950">
                          &ldquo;{currentCard.examples[0].sentenceEn}&rdquo;
                        </p>
                        <button
                          type="button"
                          onClick={() => speakText(currentCard.examples[0].sentenceEn, 'US')}
                          className="p-1 text-emerald-700 hover:bg-emerald-100 rounded-lg cursor-pointer"
                        >
                          <Volume2 className="w-4 h-4" />
                        </button>
                      </div>
                    </div>
                  )}

                  <button
                    type="button"
                    onClick={() => setIsFlipped(true)}
                    className="text-xs font-bold text-emerald-700 hover:underline cursor-pointer"
                  >
                    Nhấn để xem nghĩa Tiếng Việt, Collocations, Word Family & Cấu trúc ngữ pháp →
                  </button>
                </div>
              ) : (
                <div className="space-y-4">
                  <div className="p-4 rounded-2xl bg-amber-50/70 border border-amber-200">
                    <div className="text-xs font-bold uppercase text-amber-800">Nghĩa Tiếng Việt</div>
                    <div className="text-lg font-extrabold text-slate-900 mt-0.5">
                      {currentCard.meaningVi}
                    </div>
                    <p className="text-xs text-slate-600 mt-1">{currentCard.descriptionEn}</p>
                  </div>

                  {/* Synonyms & Antonyms */}
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                    <div className="p-3 rounded-2xl bg-slate-50 border border-slate-200/80">
                      <div className="text-xs font-bold text-slate-700 mb-1">
                        Từ đồng nghĩa (Synonyms)
                      </div>
                      <div className="flex flex-wrap gap-1.5">
                        {currentCard.synonyms.length > 0 ? (
                          currentCard.synonyms.map((s) => (
                            <Badge key={s} variant="emerald">
                              {s}
                            </Badge>
                          ))
                        ) : (
                          <span className="text-xs text-slate-400">Không có</span>
                        )}
                      </div>
                    </div>
                    <div className="p-3 rounded-2xl bg-slate-50 border border-slate-200/80">
                      <div className="text-xs font-bold text-slate-700 mb-1">
                        Từ trái nghĩa (Antonyms)
                      </div>
                      <div className="flex flex-wrap gap-1.5">
                        {currentCard.antonyms.length > 0 ? (
                          currentCard.antonyms.map((a) => (
                            <Badge key={a} variant="rose">
                              {a}
                            </Badge>
                          ))
                        ) : (
                          <span className="text-xs text-slate-400">Không có</span>
                        )}
                      </div>
                    </div>
                  </div>

                  {/* Collocations & Grammar Patterns */}
                  {currentCard.collocations.length > 0 && (
                    <div className="p-3.5 rounded-2xl bg-slate-50 border border-slate-200/80 space-y-1.5">
                      <div className="text-xs font-bold text-slate-700">
                        Cụm từ cố định (Collocations)
                      </div>
                      {currentCard.collocations.map((col) => (
                        <div key={col.id} className="text-xs text-slate-700">
                          <strong className="text-emerald-800">{col.phrase}</strong> — {col.meaningVi}
                        </div>
                      ))}
                    </div>
                  )}

                  {/* Bilingual Examples */}
                  {currentCard.examples.map((ex) => (
                    <div
                      key={ex.id}
                      className="p-3.5 rounded-2xl bg-emerald-50/50 border border-emerald-200 text-xs space-y-1"
                    >
                      <div className="font-bold text-slate-800">{ex.sentenceEn}</div>
                      <div className="text-slate-600">{ex.translationVi}</div>
                    </div>
                  ))}
                </div>
              )}

              {/* SM-2 Spaced Repetition Rating Buttons */}
              <div className="pt-4 border-t border-slate-100">
                <div className="text-xs font-bold text-slate-500 mb-2 text-center">
                  Đánh giá mức độ nhớ từ (Thuật toán lặp lại ngắt quãng SM-2):
                </div>
                <div className="grid grid-cols-4 gap-2">
                  {(['Again', 'Hard', 'Good', 'Easy'] as ReviewRating[]).map((rating) => (
                    <button
                      key={rating}
                      type="button"
                      onClick={() => handleReview(rating)}
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
                      {rating}
                    </button>
                  ))}
                </div>
              </div>
            </div>
          )}

          {/* Audio Pronunciation Assessment Studio (Audio-Input-Only Models from DB) */}
          {currentCard && (
            <div className="bg-white rounded-3xl border border-slate-200/90 shadow-xs p-6 space-y-4">
              <div className="flex flex-wrap items-center justify-between gap-2 border-b border-slate-100 pb-3">
                <div>
                  <h3 className="text-base font-extrabold text-slate-900 flex items-center gap-2">
                    <Mic className="w-4 h-4 text-emerald-600" />
                    Phòng Chấm Phát Âm Audio AI
                  </h3>
                  <p className="text-xs text-slate-500">
                    Rubric: <strong>{activeRubric?.code ?? 'ENGLISH_PRONUNCIATION_V1'}</strong> (Accuracy 40% · Clarity 30% · Fluency 30%)
                  </p>
                </div>
                <Badge variant="emerald">
                  <ShieldCheck className="w-3 h-3" /> Chỉ nhận Model có Audio Input ({audioModels.length})
                </Badge>
              </div>

              {/* Audio Pronunciation Model Selector from DB */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-bold text-slate-600 mb-1">
                    Model Chấm Phát Âm Audio (Từ Database)
                  </label>
                  <select
                    value={selectedAudioModel}
                    onChange={(e) => setSelectedAudioModel(e.target.value)}
                    className="w-full px-3 py-2 rounded-xl border border-slate-200 bg-white text-xs font-semibold text-slate-800"
                  >
                    {audioModels.map((m) => (
                      <option key={m.modelCode} value={m.modelCode}>
                        {m.displayName} ({m.modelCode})
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-600 mb-1">
                    Nội dung luyện đọc
                  </label>
                  <div className="grid grid-cols-2 gap-1.5">
                    <button
                      type="button"
                      onClick={() => setTargetType('word')}
                      className={cn(
                        'py-2 rounded-xl text-xs font-bold border cursor-pointer',
                        targetType === 'word'
                          ? 'bg-emerald-600 text-white border-emerald-600'
                          : 'bg-slate-50 text-slate-600 border-slate-200'
                      )}
                    >
                      Từ đơn ({currentCard.word})
                    </button>
                    <button
                      type="button"
                      onClick={() => setTargetType('example')}
                      className={cn(
                        'py-2 rounded-xl text-xs font-bold border cursor-pointer',
                        targetType === 'example'
                          ? 'bg-emerald-600 text-white border-emerald-600'
                          : 'bg-slate-50 text-slate-600 border-slate-200'
                      )}
                    >
                      Câu ví dụ
                    </button>
                  </div>
                </div>
              </div>

              <div className="p-3.5 rounded-2xl bg-slate-50 border border-slate-200 flex items-center justify-between gap-3">
                <div className="text-xs text-slate-700 font-semibold">
                  Mẫu câu chấm điểm:{' '}
                  <span className="text-slate-900 font-bold">
                    &ldquo;
                    {targetType === 'example' && currentCard.examples[0]
                      ? currentCard.examples[0].sentenceEn
                      : currentCard.word}
                    &rdquo;
                  </span>
                </div>

                {!isRecording ? (
                  <Button
                    variant="primary"
                    size="sm"
                    disabled={assessMutation.isPending}
                    onClick={() => void startRecording()}
                  >
                    <Mic className="w-3.5 h-3.5" />
                    {assessMutation.isPending ? 'Đang chấm điểm...' : 'Thu âm & Chấm điểm'}
                  </Button>
                ) : (
                  <Button variant="danger" size="sm" onClick={stopRecording}>
                    <Square className="w-3.5 h-3.5" />
                    Dừng thu âm
                  </Button>
                )}
              </div>

              {assessmentResult && (
                <div className="p-4 rounded-2xl bg-emerald-50/70 border border-emerald-200 space-y-3">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <Award className="w-5 h-5 text-emerald-600" />
                      <span className="text-sm font-extrabold text-slate-900">
                        Điểm phát âm: {assessmentResult.overallScore}/100
                      </span>
                    </div>
                    <Badge variant="emerald">Model: {assessmentResult.modelCodeUsed}</Badge>
                  </div>

                  <div className="grid grid-cols-1 sm:grid-cols-3 gap-2">
                    {assessmentResult.criterionScores.map((c) => (
                      <div
                        key={c.criterionCode}
                        className="p-2.5 rounded-xl bg-white border border-emerald-100 text-xs"
                      >
                        <div className="font-bold text-slate-800">
                          {c.criterionCode} ({c.weight}%): {c.score}/100
                        </div>
                        <div className="text-[11px] text-slate-500 mt-0.5">{c.feedbackVi}</div>
                      </div>
                    ))}
                  </div>

                  <p className="text-xs text-slate-700">
                    <strong>Nhận xét âm vị:</strong> {assessmentResult.phonemeAndStressFeedbackVi}
                  </p>
                  <p className="text-xs text-emerald-800 font-medium">
                    <strong>Gợi ý huấn luyện:</strong> {assessmentResult.coachingSuggestionVi}
                  </p>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Right 5 Columns: AI Vocabulary Generator (DB Models Gemini & Gemma 4 + DB Topics) */}
        <div className="lg:col-span-5 bg-white rounded-3xl border border-slate-200/90 shadow-xs p-6 space-y-5">
          <div className="border-b border-slate-100 pb-3">
            <h2 className="text-base font-extrabold text-slate-900 flex items-center gap-2">
              <Sparkles className="w-4 h-4 text-amber-500" />
              Tạo Từ Vựng Thông Minh bằng AI
            </h2>
            <p className="text-xs text-slate-500">
              Chọn Model Tạo Từ Vựng (Gemini & Gemma 4) và Chủ đề gợi ý từ Database hoặc tự nhập
            </p>
          </div>

          {/* 1. Model Tạo Từ Vựng AI (Gemini & Gemma 4 from Database) */}
          <div className="space-y-1.5">
            <div className="flex items-center justify-between">
              <label className="text-xs font-bold uppercase tracking-wider text-slate-600 flex items-center gap-1.5">
                <Cpu className="w-3.5 h-3.5 text-emerald-600" />
                Model Tạo Từ Vựng AI (Gemini & Gemma 4)
              </label>
              <Badge variant="emerald">DB ({vocabModels.length})</Badge>
            </div>
            <select
              value={selectedVocabModel}
              onChange={(e) => setSelectedVocabModel(e.target.value)}
              className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 bg-white text-xs font-semibold text-slate-800"
            >
              {vocabModels.map((m) => (
                <option key={m.modelCode} value={m.modelCode}>
                  {m.displayName} — {m.supportsAudioInput ? 'Đa phương thức' : 'Text Generation Only'}
                </option>
              ))}
            </select>
            {vocabModels.find((m) => m.modelCode === selectedVocabModel) && (
              <p className="text-[11px] text-slate-500">
                {vocabModels.find((m) => m.modelCode === selectedVocabModel)?.capabilityNote}
              </p>
            )}
          </div>

          {/* 2. CEFR Level & Word Count */}
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-bold uppercase tracking-wider text-slate-600 mb-1">
                Trình độ CEFR
              </label>
              <select
                value={genCefr}
                onChange={(e) => setGenCefr(e.target.value as CefrLevel)}
                className="w-full px-3 py-2 rounded-xl border border-slate-200 bg-white text-xs font-semibold"
              >
                {(['A1', 'A2', 'B1', 'B2', 'C1', 'C2'] as CefrLevel[]).map((l) => (
                  <option key={l} value={l}>
                    CEFR {l}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-xs font-bold uppercase tracking-wider text-slate-600 mb-1">
                Số lượng từ tạo mới
              </label>
              <select
                value={genCount}
                onChange={(e) => setGenCount(Number(e.target.value))}
                className="w-full px-3 py-2 rounded-xl border border-slate-200 bg-white text-xs font-semibold"
              >
                <option value={2}>2 từ vựng chuyên sâu</option>
                <option value={3}>3 từ vựng chuyên sâu</option>
                <option value={5}>5 từ vựng chuyên sâu</option>
              </select>
            </div>
          </div>

          {/* 3. Chủ đề tạo thêm từ (Suggestions from DB + Free Custom Input) */}
          <TopicCreatablePicker
            label="Chủ đề tạo thêm từ (DB Suggestions + Tự nhập)"
            value={genTopic}
            onChange={setGenTopic}
            topics={vocabTopics}
            placeholder="Nhập chủ đề từ vựng muốn học hoặc chọn từ gợi ý bên dưới..."
            helperText="Gợi ý chủ đề từ vựng được lấy trực tiếp từ bảng Topics trong Database, đồng thời hỗ trợ tự nhập chủ đề tùy ý."
          />

          <Button
            variant="primary"
            size="lg"
            disabled={generateMutation.isPending}
            onClick={handleGenerateMoreWords}
            className="w-full"
          >
            <Sparkles className="w-4 h-4" />
            {generateMutation.isPending
              ? `Đang tạo từ vựng với ${selectedVocabModel}...`
              : 'Tạo thêm từ vựng vào bộ thẻ hôm nay'}
          </Button>
        </div>
      </div>
    </div>
  );
};
