import React, { useState, useMemo, useEffect } from 'react';
import {
  Sparkles,
  Copy,
  Check,
  UploadCloud,
  FileCheck,
  Layers,
  ArrowRight,
  BookOpen,
  CheckSquare,
  Square,
  AlertCircle,
  HelpCircle,
  CheckCircle2,
  Cpu,
  Key,
  Loader2,
  Code2,
} from 'lucide-react';
import { Dialog } from '@/shared/components/ui/Dialog';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { TopicCreatablePicker } from '@/shared/components/ui/TopicCreatablePicker';
import { useTopicsQuery, useVocabularyGenerationModelsQuery } from '@/features/catalog/hooks/useCatalogQueries';
import { useTutorSessionStore } from '@/features/tutor';
import { toast } from '@/shared/lib/toast';
import {
  ExerciseType,
  EXERCISE_TYPE_CATALOG,
  type CefrLevel,
  type ValidationIssueDto,
} from '../../types/lessons.types';
import {
  useValidateCandidateMutation,
  useCommitLessonImportMutation,
  useBuildLessonPromptMutation,
  useGenerateLessonExercisesMutation,
} from '../../hooks/useLessonQueries';

interface AiLessonExerciseGeneratorModalProps {
  isOpen: boolean;
  onClose: () => void;
  targetLessonId?: string;
  lessonMetadata?: {
    title: string;
    cefrLevel: string;
    topic: string;
    targetVocabulary?: string[];
    targetGrammar?: string[];
  };
  onSuccess?: () => void;
}

const ALL_EXERCISE_TYPES = Object.values(ExerciseType);

const VOCAB_GRAMMAR_TYPES = [
  ExerciseType.Flashcards,
  ExerciseType.MultipleChoice,
  ExerciseType.FillInTheBlanks,
  ExerciseType.Matching,
  ExerciseType.TrueFalse,
  ExerciseType.SpellingPractice,
];

const LISTENING_SPEAKING_TYPES = [
  ExerciseType.Dictation,
  ExerciseType.ListeningComprehension,
  ExerciseType.SpeakingPractice,
  ExerciseType.Shadowing,
  ExerciseType.ConversationRolePlay,
];

const READING_WRITING_TYPES = [
  ExerciseType.ReadingComprehension,
  ExerciseType.ErrorCorrection,
  ExerciseType.SentenceUnscramble,
  ExerciseType.Translation,
  ExerciseType.WritingPractice,
];

export const AiLessonExerciseGeneratorModal: React.FC<AiLessonExerciseGeneratorModalProps> = ({
  isOpen,
  onClose,
  targetLessonId,
  lessonMetadata,
  onSuccess,
}) => {
  const { data: topics = [] } = useTopicsQuery({ onlyActive: true });
  const [activeTab, setActiveTab] = useState<'prompt' | 'import'>('prompt');

  // Generator Options State
  const [cefrLevel, setCefrLevel] = useState<string>(lessonMetadata?.cefrLevel || 'B1');
  const [targetCount, setTargetCount] = useState<number>(5);
  const [topic, setTopic] = useState<string>(lessonMetadata?.topic || '');
  const [title, setTitle] = useState<string>(lessonMetadata?.title || '');
  const [selectedTypes, setSelectedTypes] = useState<ExerciseType[]>([
    ExerciseType.Flashcards,
    ExerciseType.MultipleChoice,
    ExerciseType.FillInTheBlanks,
    ExerciseType.Dictation,
    ExerciseType.ConversationRolePlay,
  ]);
  const [customInstructions, setCustomInstructions] = useState<string>('');

  // Generated Prompt State
  const [generatedPrompt, setGeneratedPrompt] = useState<string>('');
  const [isCopied, setIsCopied] = useState<boolean>(false);

  // Import / JSON Candidate State
  const [jsonText, setJsonText] = useState<string>('');
  const [commitMode, setCommitMode] = useState<'Append' | 'Replace'>('Append');
  const [validationIssues, setValidationIssues] = useState<ValidationIssueDto[]>([]);
  const [isValidated, setIsValidated] = useState<boolean>(false);
  const [parsedCandidate, setParsedCandidate] = useState<Record<string, unknown> | null>(null);

  const { data: vocabModels = [] } = useVocabularyGenerationModelsQuery();
  const { apiKey, setApiKey } = useTutorSessionStore();

  const validateMutation = useValidateCandidateMutation();
  const commitMutation = useCommitLessonImportMutation();
  const buildPromptMutation = useBuildLessonPromptMutation();
  const generateMutation = useGenerateLessonExercisesMutation();

  const [selectedModel, setSelectedModel] = useState<string>('gemini-3.8-flash');
  const [apiKeyInput, setApiKeyInput] = useState<string>(apiKey || '');
  const [isGeneratingAi, setIsGeneratingAi] = useState<boolean>(false);
  const [isBuildingPrompt, setIsBuildingPrompt] = useState<boolean>(false);

  useEffect(() => {
    if (apiKey && !apiKeyInput) {
      setApiKeyInput(apiKey);
    }
  }, [apiKey]);

  useEffect(() => {
    if (vocabModels.length > 0 && !vocabModels.some((m) => m.modelCode === selectedModel)) {
      setSelectedModel(vocabModels[0].modelCode);
    }
  }, [vocabModels, selectedModel]);

  const handleSelectAllTypes = () => setSelectedTypes([...ALL_EXERCISE_TYPES]);
  const handleClearTypes = () => setSelectedTypes([]);
  const handleSelectVocabGrammar = () => setSelectedTypes([...VOCAB_GRAMMAR_TYPES]);
  const handleSelectListeningSpeaking = () => setSelectedTypes([...LISTENING_SPEAKING_TYPES]);
  const handleSelectReadingWriting = () => setSelectedTypes([...READING_WRITING_TYPES]);

  const toggleType = (t: ExerciseType) => {
    setSelectedTypes((prev) =>
      prev.includes(t) ? prev.filter((item) => item !== t) : [...prev, t]
    );
  };

  const handleDirectAiGenerate = async () => {
    const keyToUse = apiKeyInput.trim() || apiKey.trim();
    if (!keyToUse) {
      toast.error('Vui lòng nhập Google AI Studio API Key để sinh bài tập AI.');
      return;
    }

    const finalTopic = lessonMetadata?.topic || topic.trim() || 'Daily Communication';
    const finalCefr = lessonMetadata?.cefrLevel || cefrLevel;
    const finalTitle = lessonMetadata?.title || title.trim() || `Lesson: ${finalTopic}`;
    const chosenTypes = selectedTypes.length > 0 ? selectedTypes : [ExerciseType.MultipleChoice];

    setIsGeneratingAi(true);
    try {
      const res = await generateMutation.mutateAsync({
        apiKey: keyToUse,
        modelCode: selectedModel,
        targetLessonId: targetLessonId || undefined,
        topic: finalTopic,
        cefrLevel: finalCefr,
        lessonTitle: finalTitle,
        preferredExerciseTypes: chosenTypes,
        targetExerciseCount: targetCount,
        customInstructions: customInstructions.trim() || undefined,
        commitMode: targetLessonId ? commitMode : 'PreviewOnly',
      });

      if (res.candidate) {
        setParsedCandidate(res.candidate);
        setJsonText(JSON.stringify(res.candidate, null, 2));
      }
      setValidationIssues(res.issues || []);
      setIsValidated(res.isValid);

      if (targetLessonId && res.isValid) {
        toast.success(`AI đã tạo và lưu thành công ${targetCount} bài tập vào bài học!`);
        onSuccess?.();
      } else {
        toast.success(`AI đã tạo xong bài tập! Chuyển sang xem trước kết quả.`);
      }

      setActiveTab('import');
    } catch (err: any) {
      toast.error(err?.message || 'Lỗi khi gọi AI sinh bài tập.');
    } finally {
      setIsGeneratingAi(false);
    }
  };

  const handleGeneratePrompt = async () => {
    const finalTopic = lessonMetadata?.topic || topic.trim() || 'Daily Communication';
    const finalCefr = lessonMetadata?.cefrLevel || cefrLevel;
    const finalTitle = lessonMetadata?.title || title.trim() || `Lesson: ${finalTopic}`;
    const chosenTypes = selectedTypes.length > 0 ? selectedTypes : [ExerciseType.MultipleChoice];

    setIsBuildingPrompt(true);
    try {
      const res = await buildPromptMutation.mutateAsync({
        topic: finalTopic,
        cefrLevel: finalCefr,
        lessonTitle: finalTitle,
        preferredExerciseTypes: chosenTypes,
        targetExerciseCount: targetCount,
        customInstructions: customInstructions.trim() || undefined,
      });

      const fullPrompt = res.promptText || `${res.systemPrompt}\n\n${res.userPrompt}`;
      setGeneratedPrompt(fullPrompt);
      toast.success('Đã tạo Prompt thành công từ API hệ thống!');
    } catch (err: any) {
      toast.error(err?.message || 'Lỗi khi gọi API tạo Prompt.');
    } finally {
      setIsBuildingPrompt(false);
    }
  };

  const handleCopyPrompt = async () => {
    if (!generatedPrompt) return;
    try {
      await navigator.clipboard.writeText(generatedPrompt);
      setIsCopied(true);
      toast.success('Đã sao chép prompt vào clipboard!');
      setTimeout(() => setIsCopied(false), 2500);
    } catch {
      toast.error('Không thể sao chép vào bộ nhớ tạm.');
    }
  };

  const handleValidateJson = async () => {
    setValidationIssues([]);
    setIsValidated(false);
    setParsedCandidate(null);

    let parsed: Record<string, unknown>;
    try {
      parsed = JSON.parse(jsonText.trim());
      setParsedCandidate(parsed);
    } catch {
      setValidationIssues([
        {
          path: '$',
          severity: 'Error',
          message: 'Cú pháp JSON không hợp lệ. Vui lòng kiểm tra lại dấu ngoặc kép hoặc dấu phẩy thừa.',
        },
      ]);
      return;
    }

    try {
      const res = await validateMutation.mutateAsync(parsed);
      setValidationIssues(res.issues);
      setIsValidated(res.isValid);
      if (res.isValid) {
        toast.success('JSON hợp lệ và chuẩn hóa theo schema v1!');
      } else {
        toast.error('JSON phát hiện lỗi schema, vui lòng xem danh sách bên dưới.');
      }
    } catch (err: any) {
      setValidationIssues([
        {
          path: '$',
          severity: 'Error',
          message: err?.message || 'Lỗi khi gọi API kiểm chứng.',
        },
      ]);
    }
  };

  const handleCommitImport = async () => {
    if (!parsedCandidate) return;
    const operationKey = `import_${Date.now()}_${Math.random().toString(36).substring(2, 9)}`;

    try {
      await commitMutation.mutateAsync({
        data: {
          targetLessonId: targetLessonId || undefined,
          commitMode: targetLessonId ? commitMode : 'Append',
          candidate: parsedCandidate,
        },
        operationKey,
      });

      toast.success(
        targetLessonId
          ? 'Đã nạp thành công các bài tập vào bài học!'
          : 'Đã tạo bài học mới thành công từ AI!'
      );
      onSuccess?.();
      onClose();
    } catch (err: any) {
      toast.error(err?.message || 'Lỗi khi lưu bài tập vào hệ thống.');
    }
  };

  const parsedExercisesList = useMemo(() => {
    if (!parsedCandidate) return [];
    const arr = (parsedCandidate as any)?.exercises || (parsedCandidate as any)?.lesson?.exercises;
    return Array.isArray(arr) ? arr : [];
  }, [parsedCandidate]);

  const errorCount = validationIssues.filter((i) => i.severity === 'Error').length;
  const warningCount = validationIssues.filter((i) => i.severity === 'Warning').length;

  return (
    <Dialog
      isOpen={isOpen}
      onClose={onClose}
      maxWidth="3xl"
      title="Tạo Bài Tập & Giáo Án Bằng AI"
      description="Chọn loại bài tập, số lượng và sinh Prompt chuyên sâu hoặc dán kết quả JSON để nạp vào bài học."
      icon={<Sparkles className="w-5 h-5 text-amber-500" />}
      footer={
        <div className="flex items-center justify-between w-full">
          <div className="text-xs text-slate-500 flex items-center gap-1.5">
            <HelpCircle className="w-3.5 h-3.5 text-slate-400" />
            <span>Chuẩn hợp đồng bài học: <strong>lesson-exercises.v1</strong> (16 loại bài tập)</span>
          </div>

          <div className="flex items-center gap-2">
            <Button type="button" variant="outline" size="sm" onClick={onClose}>
              Đóng
            </Button>
            {activeTab === 'import' && (
              <Button
                type="button"
                variant="primary"
                size="sm"
                onClick={handleCommitImport}
                disabled={!isValidated || errorCount > 0 || commitMutation.isPending}
                className="gap-1.5"
              >
                <CheckCircle2 className="w-4 h-4" />
                {commitMutation.isPending ? 'Đang lưu bài tập...' : 'Lưu vào bài học'}
              </Button>
            )}
          </div>
        </div>
      }
    >
      <div className="space-y-5 text-xs">
        {/* Navigation Tabs */}
        <div className="flex border-b border-slate-200 gap-2">
          <button
            type="button"
            onClick={() => setActiveTab('prompt')}
            className={`flex items-center gap-2 pb-3 px-3.5 font-bold border-b-2 transition-all cursor-pointer text-xs ${
              activeTab === 'prompt'
                ? 'border-emerald-600 text-emerald-700'
                : 'border-transparent text-slate-500 hover:text-slate-800'
            }`}
          >
            <Sparkles className="w-4 h-4 text-amber-500" />
            1. Cấu hình & Sinh Prompt AI
          </button>
          <button
            type="button"
            onClick={() => setActiveTab('import')}
            className={`flex items-center gap-2 pb-3 px-3.5 font-bold border-b-2 transition-all cursor-pointer text-xs ${
              activeTab === 'import'
                ? 'border-emerald-600 text-emerald-700'
                : 'border-transparent text-slate-500 hover:text-slate-800'
            }`}
          >
            <UploadCloud className="w-4 h-4 text-emerald-600" />
            2. Dán & Nhập JSON từ AI
            {isValidated && errorCount === 0 && (
              <Badge variant="emerald" className="text-[10px] py-0 px-1.5 ml-1">
                Sẵn sàng
              </Badge>
            )}
          </button>
        </div>

        {activeTab === 'prompt' ? (
          <div className="space-y-4">
            {/* If lessonMetadata is provided (e.g. from Lesson Builder), use lesson context directly */}
            {lessonMetadata ? (
              <div className="p-3.5 bg-slate-50 border border-slate-200/90 rounded-2xl flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 shadow-2xs">
                <div className="space-y-1">
                  <div className="flex items-center gap-2">
                    <BookOpen className="w-4 h-4 text-emerald-600 shrink-0" />
                    <span className="text-sm font-bold text-slate-900">{lessonMetadata.title}</span>
                  </div>
                  <div className="flex flex-wrap items-center gap-2 text-xs text-slate-600">
                    <span className="font-semibold">Chủ đề (Topic):</span>
                    <Badge variant="outline" className="font-bold text-emerald-700 bg-emerald-50 border-emerald-200">
                      {lessonMetadata.topic}
                    </Badge>
                    <span className="text-slate-400">•</span>
                    <span className="font-semibold">Trình độ:</span>
                    <Badge variant="outline" className="font-extrabold text-slate-700 bg-white border-slate-200">
                      CEFR {lessonMetadata.cefrLevel}
                    </Badge>
                  </div>
                </div>

                <div className="w-full sm:w-auto">
                  <label className="block text-[11px] font-bold uppercase tracking-wider text-slate-700 mb-1">
                    Số lượng bài tập *
                  </label>
                  <select
                    value={targetCount}
                    onChange={(e) => setTargetCount(Number(e.target.value))}
                    className="w-full sm:w-44 px-3 py-1.5 rounded-xl border border-slate-200 bg-white font-semibold text-slate-800 text-xs focus:ring-2 focus:ring-emerald-500"
                  >
                    <option value={3}>3 bài tập (Khởi động)</option>
                    <option value={5}>5 bài tập (Tiêu chuẩn)</option>
                    <option value={8}>8 bài tập (Chuyên sâu)</option>
                    <option value={10}>10 bài tập (Mở rộng)</option>
                    <option value={16}>16 bài tập (Toàn diện)</option>
                  </select>
                </div>
              </div>
            ) : (
              <>
                {/* Row 1: CEFR Level & Target Count (when creating lesson from scratch) */}
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
                  <div>
                    <label className="block font-bold uppercase tracking-wider text-slate-700 mb-1">
                      Trình độ CEFR mục tiêu *
                    </label>
                    <select
                      value={cefrLevel}
                      onChange={(e) => setCefrLevel(e.target.value)}
                      className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 bg-white font-semibold text-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-500"
                    >
                      {['A1', 'A2', 'B1', 'B2', 'C1', 'C2'].map((lvl) => (
                        <option key={lvl} value={lvl}>
                          CEFR Level {lvl} {lvl === 'A1' ? '(Cơ bản)' : lvl === 'B1' ? '(Trung cấp)' : lvl === 'C1' ? '(Nâng cao)' : ''}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div>
                    <label className="block font-bold uppercase tracking-wider text-slate-700 mb-1">
                      Số lượng bài tập mong muốn *
                    </label>
                    <select
                      value={targetCount}
                      onChange={(e) => setTargetCount(Number(e.target.value))}
                      className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 bg-white font-semibold text-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-500"
                    >
                      <option value={3}>3 bài tập (Khởi động / Rút gọn)</option>
                      <option value={5}>5 bài tập (Tiêu chuẩn / Đầy đủ kỹ năng)</option>
                      <option value={8}>8 bài tập (Chuyên sâu)</option>
                      <option value={10}>10 bài tập (Luyện tập mở rộng)</option>
                      <option value={16}>16 bài tập (Toàn diện tất cả các dạng)</option>
                    </select>
                  </div>
                </div>

                {/* Row 2: Title & Topic (when creating lesson from scratch) */}
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
                  <div>
                    <label className="block font-bold uppercase tracking-wider text-slate-700 mb-1">
                      Tiêu đề bài học
                    </label>
                    <input
                      type="text"
                      value={title}
                      onChange={(e) => setTitle(e.target.value)}
                      placeholder="Ví dụ: Dining Out & Ordering Food"
                      className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 bg-white font-medium text-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-500"
                    />
                  </div>

                  <div>
                    <TopicCreatablePicker
                      label="Chủ đề (Topic)"
                      value={topic}
                      onChange={setTopic}
                      topics={topics}
                      placeholder="Chọn chủ đề gợi ý hoặc gõ chủ đề mới..."
                      helperText="Gợi ý từ danh mục chủ đề của hệ thống hoặc nhập tự do."
                    />
                  </div>
                </div>
              </>
            )}

            {/* AI Model & API Key Configuration */}
            <div className="p-3.5 bg-emerald-50/40 border border-emerald-200/80 rounded-2xl space-y-3">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label className="block font-bold uppercase tracking-wider text-slate-700 mb-1 flex items-center gap-1.5 text-[11px]">
                    <Cpu className="w-3.5 h-3.5 text-emerald-600" />
                    Chọn Model AI sinh bài tập *
                  </label>
                  <select
                    value={selectedModel}
                    onChange={(e) => setSelectedModel(e.target.value)}
                    className="w-full px-3 py-2 rounded-xl border border-slate-200 bg-white font-semibold text-slate-800 text-xs focus:outline-none focus:ring-2 focus:ring-emerald-500"
                  >
                    {vocabModels.length > 0 ? (
                      vocabModels.map((m) => (
                        <option key={m.modelCode} value={m.modelCode}>
                          {m.displayName} ({m.modelCode})
                        </option>
                      ))
                    ) : (
                      <>
                        <option value="gemini-3.8-flash">Gemini 3.8 Flash (Mặc định)</option>
                        <option value="gemini-2.5-flash">Gemini 2.5 Flash</option>
                        <option value="gemini-2.0-flash">Gemini 2.0 Flash</option>
                      </>
                    )}
                  </select>
                </div>

                <div>
                  <label className="block font-bold uppercase tracking-wider text-slate-700 mb-1 flex items-center justify-between text-[11px]">
                    <span className="flex items-center gap-1.5">
                      <Key className="w-3.5 h-3.5 text-amber-600" />
                      Google AI API Key
                    </span>
                    {apiKeyInput.trim() || apiKey.trim() ? (
                      <span className="text-[10px] text-emerald-600 font-bold lowercase">
                        (đã cấu hình)
                      </span>
                    ) : (
                      <span className="text-[10px] text-rose-500 font-bold">
                        (cần nhập API Key)
                      </span>
                    )}
                  </label>
                  <input
                    type="password"
                    value={apiKeyInput}
                    onChange={(e) => {
                      setApiKeyInput(e.target.value);
                      setApiKey(e.target.value.trim());
                    }}
                    placeholder="Nhập Google AI Studio API Key..."
                    className="w-full px-3 py-2 rounded-xl border border-slate-200 bg-white font-mono text-xs text-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-500"
                  />
                </div>
              </div>
            </div>

            {/* Row 3: Exercise Types Selection with Quick Filter Chips */}
            <div className="space-y-2.5 pt-1 border-t border-slate-100">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <span className="font-bold text-slate-800 flex items-center gap-1.5 uppercase tracking-wider text-[11px]">
                  <Layers className="w-3.5 h-3.5 text-emerald-600" />
                  Chọn loại bài tập ({selectedTypes.length}/{ALL_EXERCISE_TYPES.length})
                </span>

                {/* Quick Filters */}
                <div className="flex flex-wrap items-center gap-1.5">
                  <button
                    type="button"
                    onClick={handleSelectAllTypes}
                    className="px-2 py-1 rounded-lg text-[11px] font-bold bg-slate-100 hover:bg-slate-200 text-slate-700 transition-colors"
                  >
                    Tất cả (16)
                  </button>
                  <button
                    type="button"
                    onClick={handleSelectVocabGrammar}
                    className="px-2 py-1 rounded-lg text-[11px] font-bold bg-emerald-50 hover:bg-emerald-100 text-emerald-800 transition-colors"
                  >
                    Từ vựng & Trắc nghiệm
                  </button>
                  <button
                    type="button"
                    onClick={handleSelectListeningSpeaking}
                    className="px-2 py-1 rounded-lg text-[11px] font-bold bg-sky-50 hover:bg-sky-100 text-sky-800 transition-colors"
                  >
                    Nghe & Nói
                  </button>
                  <button
                    type="button"
                    onClick={handleSelectReadingWriting}
                    className="px-2 py-1 rounded-lg text-[11px] font-bold bg-purple-50 hover:bg-purple-100 text-purple-800 transition-colors"
                  >
                    Đọc & Viết
                  </button>
                  <button
                    type="button"
                    onClick={handleClearTypes}
                    className="px-2 py-1 rounded-lg text-[11px] font-bold text-slate-400 hover:text-slate-600 transition-colors"
                  >
                    Bỏ chọn
                  </button>
                </div>
              </div>

              {/* Grid of 16 Exercise Types */}
              <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-2 max-h-48 overflow-y-auto p-1 bg-slate-50/80 rounded-2xl border border-slate-200/80">
                {ALL_EXERCISE_TYPES.map((t) => {
                  const meta = EXERCISE_TYPE_CATALOG[t];
                  const isChecked = selectedTypes.includes(t);
                  return (
                    <div
                      key={t}
                      onClick={() => toggleType(t)}
                      className={`flex items-center gap-2 p-2 rounded-xl border text-xs cursor-pointer transition-all select-none ${
                        isChecked
                          ? 'bg-white border-emerald-500 shadow-2xs font-bold text-slate-900'
                          : 'bg-transparent border-transparent hover:bg-white/60 text-slate-600'
                      }`}
                    >
                      {isChecked ? (
                        <CheckSquare className="w-4 h-4 text-emerald-600 shrink-0" />
                      ) : (
                        <Square className="w-4 h-4 text-slate-300 shrink-0" />
                      )}
                      <div className="min-w-0 flex-1">
                        <div className="truncate text-[11px] leading-tight">
                          {meta?.labelVi || t}
                        </div>
                        <div className="text-[10px] font-mono text-slate-400 truncate">
                          {t}
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>

            {/* Teacher Custom Instructions */}
            <div>
              <label className="block font-bold uppercase tracking-wider text-slate-700 mb-1">
                Yêu cầu bổ sung của giáo viên (Tùy chọn)
              </label>
              <textarea
                value={customInstructions}
                onChange={(e) => setCustomInstructions(e.target.value)}
                placeholder="Ví dụ: Tập trung vào từ vựng gọi món tại nhà hàng, hỏi giá tiền và yêu cầu thanh toán bằng thẻ tín dụng..."
                rows={2}
                className="w-full px-3.5 py-2 rounded-xl border border-slate-200 bg-white text-slate-800 placeholder:text-slate-400 text-xs focus:outline-none focus:ring-2 focus:ring-emerald-500"
              />
            </div>

            {/* Action Buttons: Direct AI Generation + Generate Prompt */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-1">
              <Button
                type="button"
                variant="primary"
                onClick={handleDirectAiGenerate}
                disabled={isGeneratingAi || isBuildingPrompt}
                className="w-full gap-2 shadow-xs py-2.5 font-bold"
              >
                {isGeneratingAi ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin text-white" />
                    Đang sinh bài tập với AI...
                  </>
                ) : (
                  <>
                    <Sparkles className="w-4 h-4 text-amber-300" />
                    Tạo trực tiếp bằng AI (Backend)
                  </>
                )}
              </Button>

              <Button
                type="button"
                variant="outline"
                onClick={handleGeneratePrompt}
                disabled={isGeneratingAi || isBuildingPrompt}
                className="w-full gap-2 py-2.5 font-bold border-slate-200 hover:border-emerald-500 text-slate-700 hover:text-emerald-800"
              >
                {isBuildingPrompt ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin text-emerald-600" />
                    Đang gọi API tạo Prompt...
                  </>
                ) : (
                  <>
                    <Code2 className="w-4 h-4 text-emerald-600" />
                    Tạo Prompt AI (Cho ChatGPT / Claude)
                  </>
                )}
              </Button>
            </div>

            {/* Generated Prompt Output Box */}
            {generatedPrompt && (
              <div className="space-y-2.5 pt-3 border-t border-slate-200">
                <div className="flex items-center justify-between">
                  <span className="font-bold text-slate-800 flex items-center gap-1.5">
                    <CheckCircle2 className="w-4 h-4 text-emerald-600" />
                    Prompt đã sẵn sàng để gửi tới AI (ChatGPT / Gemini / Claude):
                  </span>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={handleCopyPrompt}
                    className="gap-1.5 text-xs text-emerald-700 border-slate-200 hover:border-emerald-500 hover:bg-emerald-50"
                  >
                    {isCopied ? <Check className="w-4 h-4 text-emerald-600" /> : <Copy className="w-4 h-4" />}
                    {isCopied ? 'Đã sao chép!' : 'Sao chép Prompt'}
                  </Button>
                </div>

                <div className="relative">
                  <textarea
                    readOnly
                    rows={8}
                    value={generatedPrompt}
                    className="w-full text-[11px] font-mono bg-slate-900 text-slate-200 p-3.5 rounded-2xl border border-slate-800 select-all leading-relaxed focus:outline-none"
                  />
                </div>

                <div className="bg-amber-50/80 border border-amber-200/90 rounded-2xl p-3 text-xs text-amber-900 space-y-1">
                  <span className="font-bold block flex items-center gap-1.5">
                    <AlertCircle className="w-3.5 h-3.5 text-amber-600" />
                    Các bước tiếp theo:
                  </span>
                  <ol className="list-decimal list-inside space-y-0.5 text-amber-800 text-[11px]">
                    <li>Nhấn nút <strong>Sao chép Prompt</strong> ở trên và dán vào ChatGPT / Claude / Gemini.</li>
                    <li>Sau khi AI phản hồi khối JSON, sao chép toàn bộ khối JSON kết quả.</li>
                    <li>Chuyển sang tab <strong>&quot;2. Dán & Nhập JSON từ AI&quot;</strong> để kiểm tra schema và lưu bài tập.</li>
                  </ol>
                  <div className="pt-2">
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() => setActiveTab('import')}
                      className="gap-1.5 text-xs text-amber-900 border-amber-300 hover:bg-amber-100"
                    >
                      Chuyển sang Bước 2: Dán & Lưu JSON <ArrowRight className="w-3.5 h-3.5" />
                    </Button>
                  </div>
                </div>
              </div>
            )}
          </div>
        ) : (
          <div className="space-y-4">
            {/* Tab 2: Import & Preview */}
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <label className="font-bold uppercase tracking-wider text-slate-700">
                  Dán nội dung JSON từ AI vào đây:
                </label>
                {jsonText.trim() && (
                  <button
                    type="button"
                    onClick={() => {
                      setJsonText('');
                      setParsedCandidate(null);
                      setIsValidated(false);
                      setValidationIssues([]);
                    }}
                    className="text-[11px] text-slate-400 hover:text-rose-600 cursor-pointer"
                  >
                    Xóa nội dung
                  </button>
                )}
              </div>

              <textarea
                value={jsonText}
                onChange={(e) => {
                  setJsonText(e.target.value);
                  setIsValidated(false);
                }}
                rows={9}
                placeholder='{\n  "schemaVersion": "lesson-exercises.v1",\n  "lesson": { ... },\n  "exercises": [ ... ]\n}'
                className="w-full text-xs font-mono bg-slate-900 text-emerald-400 p-3.5 rounded-2xl border border-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-500 leading-relaxed placeholder:text-slate-600"
              />
            </div>

            {/* Validate Button */}
            <div className="flex flex-wrap items-center justify-between gap-2">
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={handleValidateJson}
                disabled={!jsonText.trim() || validateMutation.isPending}
                className="gap-1.5 text-xs text-slate-700 border-slate-200 hover:border-emerald-500 hover:text-emerald-700 hover:bg-emerald-50"
              >
                <FileCheck className="w-4 h-4 text-emerald-600" />
                {validateMutation.isPending ? 'Đang kiểm chứng schema...' : 'Kiểm tra tính hợp lệ (Validate Schema)'}
              </Button>

              {isValidated && errorCount === 0 && (
                <Badge variant="emerald" className="font-bold py-1">
                  JSON hoàn toàn hợp lệ theo schema contract v1!
                </Badge>
              )}
            </div>

            {/* Validation Issues if any */}
            {validationIssues.length > 0 && (
              <div className="p-3.5 rounded-2xl bg-rose-50 border border-rose-200 space-y-2">
                <div className="flex items-center justify-between text-xs font-bold text-rose-800">
                  <span>Phát hiện {errorCount} lỗi, {warningCount} cảnh báo:</span>
                </div>
                <div className="max-h-36 overflow-y-auto space-y-1 text-[11px]">
                  {validationIssues.map((issue, idx) => (
                    <div
                      key={idx}
                      className={`p-1.5 rounded-lg border font-mono ${
                        issue.severity === 'Error'
                          ? 'bg-rose-100/60 border-rose-200 text-rose-900'
                          : 'bg-amber-100/60 border-amber-200 text-amber-900'
                      }`}
                    >
                      <strong>{issue.path}:</strong> {issue.message}
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Preview of Parsed Exercises */}
            {parsedExercisesList.length > 0 && (
              <div className="space-y-2.5 pt-2 border-t border-slate-100">
                <div className="flex items-center justify-between">
                  <span className="font-bold text-slate-800 flex items-center gap-1.5">
                    <Layers className="w-4 h-4 text-emerald-600" />
                    Danh sách bài tập nhận diện được ({parsedExercisesList.length} bài tập):
                  </span>
                </div>

                <div className="space-y-1.5 max-h-48 overflow-y-auto pr-1">
                  {parsedExercisesList.map((ex: any, idx: number) => {
                    const meta = EXERCISE_TYPE_CATALOG[ex.type as ExerciseType];
                    return (
                      <div
                        key={idx}
                        className="flex items-center justify-between p-2.5 rounded-xl border border-slate-200 bg-slate-50 text-xs"
                      >
                        <div className="flex items-center gap-2 truncate">
                          <span className="text-[10px] font-mono font-bold bg-slate-200 px-1.5 py-0.5 rounded">
                            #{idx + 1}
                          </span>
                          <span className="font-bold text-slate-800 truncate">
                            {ex.title || ex.key}
                          </span>
                          <Badge variant="outline" className="text-[10px] py-0">
                            {meta?.labelVi || ex.type}
                          </Badge>
                        </div>
                        <span className="text-[11px] font-semibold text-emerald-700 shrink-0">
                          {ex.maxPoints || 0} điểm
                        </span>
                      </div>
                    );
                  })}
                </div>

                {/* Commit Mode (Append vs Replace) if modifying existing lesson */}
                {targetLessonId && (
                  <div className="p-3 rounded-2xl bg-slate-50 border border-slate-200 flex items-center justify-between gap-4">
                    <span className="font-bold text-slate-700">Chế độ nạp vào bài học:</span>
                    <div className="flex items-center gap-3">
                      <label className="flex items-center gap-1.5 cursor-pointer font-medium text-slate-700">
                        <input
                          type="radio"
                          name="commitMode"
                          checked={commitMode === 'Append'}
                          onChange={() => setCommitMode('Append')}
                          className="text-emerald-600 focus:ring-emerald-500"
                        />
                        Thêm vào cuối (Append)
                      </label>
                      <label className="flex items-center gap-1.5 cursor-pointer font-medium text-rose-700">
                        <input
                          type="radio"
                          name="commitMode"
                          checked={commitMode === 'Replace'}
                          onChange={() => setCommitMode('Replace')}
                          className="text-rose-600 focus:ring-rose-500"
                        />
                        Thay thế toàn bộ (Replace)
                      </label>
                    </div>
                  </div>
                )}
              </div>
            )}
          </div>
        )}
      </div>
    </Dialog>
  );
};
