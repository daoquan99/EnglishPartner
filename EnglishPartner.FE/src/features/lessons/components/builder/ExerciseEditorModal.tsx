import React, { useState } from 'react';
import { X, Check, Code, Sparkles } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import {
  ExerciseType,
  EXERCISE_TYPE_CATALOG,
  type AddExerciseRequest,
  type ExerciseAuthorDto,
  type GradingMode,
} from '../../types/lessons.types';

interface ExerciseEditorModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSave: (data: AddExerciseRequest) => void;
  initialExercise?: ExerciseAuthorDto | null;
  isSaving?: boolean;
}

const TEMPLATES_BY_TYPE: Record<ExerciseType, { content: Record<string, unknown>; grading: Record<string, unknown> }> = {
  [ExerciseType.Flashcards]: {
    content: {
      cards: [
        { id: 'c1', term: 'appetizer', ipa: 'ˈæp.ə.taɪ.zɚ', meaningVi: 'món khai vị', exampleEn: 'We ordered appetizers first.', exampleVi: 'Chúng tôi gọi món khai vị trước.' },
        { id: 'c2', term: 'entree', ipa: 'ˈɑːn.treɪ', meaningVi: 'món chính', exampleEn: 'For my entree, I will have salmon.', exampleVi: 'Món chính của tôi là cá hồi.' },
      ],
    },
    grading: {},
  },
  [ExerciseType.MultipleChoice]: {
    content: {
      question: 'Which of the following phrases is the most polite way to ask for the menu?',
      options: [
        { id: 'opt_1', text: 'Give me the menu now.' },
        { id: 'opt_2', text: 'Could we please see the menu?' },
        { id: 'opt_3', text: 'Where is menu?' },
        { id: 'opt_4', text: 'Menu here.' },
      ],
      singleSelect: true,
    },
    grading: {
      correctOptionIds: ['opt_2'],
      explanation: "'Could we please see...' is the formal and polite phrasing for dining in restaurants.",
    },
  },
  [ExerciseType.FillInTheBlanks]: {
    content: {
      textWithBlanks: 'Could we have the [blank_1], please? We would like to [blank_2] our food now.',
      blanks: [
        { id: 'blank_1', hint: 'menu / bill' },
        { id: 'blank_2', hint: 'order / pay' },
      ],
    },
    grading: {
      blanks: [
        { id: 'blank_1', correctAnswers: ['menu'], caseSensitive: false, ignorePunctuation: true },
        { id: 'blank_2', correctAnswers: ['order'], caseSensitive: false, ignorePunctuation: true },
      ],
    },
  },
  [ExerciseType.Matching]: {
    content: {
      leftItems: [
        { id: 'l1', text: 'Rare' },
        { id: 'l2', text: 'Medium' },
        { id: 'l3', text: 'Well-done' },
      ],
      rightItems: [
        { id: 'r1', text: 'Thịt còn tái đỏ bên trong' },
        { id: 'r2', text: 'Thịt chín vừa, còn hồng nhạt' },
        { id: 'r3', text: 'Thịt chín kỹ hoàn toàn' },
      ],
    },
    grading: {
      pairs: [
        { leftId: 'l1', rightId: 'r1' },
        { leftId: 'l2', rightId: 'r2' },
        { leftId: 'l3', rightId: 'r3' },
      ],
    },
  },
  [ExerciseType.SpellingPractice]: {
    content: {
      prompt: 'Spelling challenge',
      definition: 'A formal printed or written list of food choices in a restaurant.',
      hint: 'Starts with M, ends with U (4 letters).',
    },
    grading: {
      targetWord: 'menu',
      acceptedSpellings: ['menu'],
    },
  },
  [ExerciseType.SentenceUnscramble]: {
    content: {
      translationHint: 'Tôi có thể xem qua danh sách món tráng miệng được không?',
      tokens: [
        { id: 't1', text: 'Could' },
        { id: 't2', text: 'I' },
        { id: 't3', text: 'see' },
        { id: 't4', text: 'the' },
        { id: 't5', text: 'dessert' },
        { id: 't6', text: 'menu?' },
      ],
    },
    grading: {
      validSequences: [['t1', 't2', 't3', 't4', 't5', 't6']],
    },
  },
  [ExerciseType.ErrorCorrection]: {
    content: {
      incorrectSentence: 'I would like order a bowl of mushroom soup.',
      errorFocus: 'Missing preposition after would like',
    },
    grading: {
      acceptedCorrections: [
        'I would like to order a bowl of mushroom soup.',
        'I would like to order a bowl of mushroom soup',
      ],
    },
  },
  [ExerciseType.Translation]: {
    content: {
      sourceText: 'Chúng tôi muốn đặt một bàn cho bốn người vào lúc bảy giờ tối.',
      direction: 'ViToEn',
      contextHint: 'Booking a restaurant table',
    },
    grading: {
      rubricCode: 'LESSON_TRANSLATION',
      rubricVersion: 1,
      referenceTranslations: [
        'We would like to reserve a table for four at seven PM.',
        'We would like to book a table for four at 7 PM.',
      ],
    },
  },
  [ExerciseType.TrueFalse]: {
    content: {
      passage: 'Service charge is already included in the total bill at this restaurant. Customers do not need to leave additional tips.',
      statements: [
        { id: 's1', text: 'Customers must pay tips separately in cash.' },
        { id: 's2', text: 'The bill already includes the service fee.' },
      ],
    },
    grading: {
      answers: {
        s1: false,
        s2: true,
      },
    },
  },
  [ExerciseType.ReadingComprehension]: {
    content: {
      passage: 'The Golden Fork restaurant is famous for its handmade pasta and seafood specialties. Reservations are highly recommended on weekends.',
      subQuestions: [
        {
          id: 'q1',
          kind: 'MultipleChoice',
          prompt: 'What is The Golden Fork famous for?',
          options: [
            { id: 'o1', text: 'Handmade pasta and seafood' },
            { id: 'o2', text: 'Fast food hamburgers' },
          ],
        },
        {
          id: 'q2',
          kind: 'TrueFalse',
          prompt: 'Reservations are required on weekdays only.',
        },
      ],
    },
    grading: {
      subQuestions: [
        { id: 'q1', correctOptionIds: ['o1'] },
        { id: 'q2', answers: { q2: false } },
      ],
    },
  },
  [ExerciseType.ListeningComprehension]: {
    content: {
      audioUrl: 'https://storage.googleapis.com/sample-audio/restaurant-dialogue.mp3',
      subQuestions: [
        {
          id: 'lq1',
          kind: 'MultipleChoice',
          prompt: 'What did the customer decide to drink?',
          options: [
            { id: 'o1', text: 'Sparkling water with lemon' },
            { id: 'o2', text: 'Hot green tea' },
          ],
        },
      ],
    },
    grading: {
      transcript: 'Customer: I would love a glass of sparkling water with lemon, please.',
      subQuestions: [{ id: 'lq1', correctOptionIds: ['o1'] }],
    },
  },
  [ExerciseType.Dictation]: {
    content: {
      audioUrl: 'https://storage.googleapis.com/sample-audio/dictation-chef.mp3',
      speed: 1.0,
    },
    grading: {
      transcript: 'Our chef prepares fresh handmade pasta with imported olive oil.',
    },
  },
  [ExerciseType.Shadowing]: {
    content: {
      audioUrl: 'https://storage.googleapis.com/sample-audio/shadowing-sample.mp3',
      referenceText: 'Good evening, do you have a table for two available tonight?',
    },
    grading: {
      rubricCode: 'LESSON_SHADOWING',
      rubricVersion: 1,
    },
  },
  [ExerciseType.SpeakingPractice]: {
    content: {
      prompt: 'Describe your favorite dining experience at a restaurant.',
      taskInstructions: 'Speak for 45-60 seconds. Mention the food, atmosphere, and service.',
      targetDurationSeconds: 60,
      cueQuestions: ['Where was it?', 'What did you eat?', 'Why was it memorable?'],
    },
    grading: {
      rubricCode: 'LESSON_SPEAKING',
      rubricVersion: 1,
    },
  },
  [ExerciseType.WritingPractice]: {
    content: {
      prompt: 'Write a short restaurant review giving your impressions and recommendations.',
      taskInstructions: 'Write between 50 to 80 words. Include recommendations for other guests.',
      minWords: 50,
      maxWords: 80,
    },
    grading: {
      rubricCode: 'LESSON_WRITING',
      rubricVersion: 1,
    },
  },
  [ExerciseType.ConversationRolePlay]: {
    content: {
      scenario: 'You are dining at an Italian restaurant and want to ask the waiter about gluten-free dishes.',
      learnerRole: 'Guest',
      aiRole: 'Italian Waiter',
      initialAiMessage: 'Buonasera! Welcome to Trattoria Roma. Here are the menus. May I start you off with something to drink?',
      maxTurns: 6,
    },
    grading: {
      rubricCode: 'LESSON_CONVERSATION',
      rubricVersion: 1,
    },
  },
};

export const ExerciseEditorModal: React.FC<ExerciseEditorModalProps> = ({
  isOpen,
  onClose,
  onSave,
  initialExercise,
  isSaving = false,
}) => {
  const [type, setType] = useState<ExerciseType>(
    initialExercise?.type ?? ExerciseType.MultipleChoice
  );
  const [key, setKey] = useState(initialExercise?.key ?? `ex_${Date.now()}`);
  const [title, setTitle] = useState(initialExercise?.title ?? '');
  const [instructions, setInstructions] = useState(initialExercise?.instructions ?? '');
  const [maxPoints, setMaxPoints] = useState<number>(initialExercise?.maxPoints ?? 10);
  const [gradingMode, setGradingMode] = useState<GradingMode>(
    initialExercise?.gradingMode ?? EXERCISE_TYPE_CATALOG[type]?.defaultGradingMode ?? 'Deterministic'
  );
  const [skillTags, setSkillTags] = useState(
    initialExercise?.skillTags?.join(', ') ?? ''
  );

  const [contentJson, setContentJson] = useState(
    JSON.stringify(initialExercise?.content ?? TEMPLATES_BY_TYPE[type].content, null, 2)
  );
  const [gradingJson, setGradingJson] = useState(
    JSON.stringify(initialExercise?.grading ?? TEMPLATES_BY_TYPE[type].grading, null, 2)
  );

  const [jsonError, setJsonError] = useState<string | null>(null);

  if (!isOpen) return null;

  const handleTypeChange = (newType: ExerciseType) => {
    setType(newType);
    const meta = EXERCISE_TYPE_CATALOG[newType];
    setGradingMode(meta.defaultGradingMode);
    setContentJson(JSON.stringify(TEMPLATES_BY_TYPE[newType].content, null, 2));
    setGradingJson(JSON.stringify(TEMPLATES_BY_TYPE[newType].grading, null, 2));
    if (!title || title.startsWith('Bài tập:')) {
      setTitle(`Bài tập: ${meta.labelVi}`);
    }
  };

  const handleApplyTemplate = () => {
    setContentJson(JSON.stringify(TEMPLATES_BY_TYPE[type].content, null, 2));
    setGradingJson(JSON.stringify(TEMPLATES_BY_TYPE[type].grading, null, 2));
    setJsonError(null);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setJsonError(null);

    let parsedContent: Record<string, unknown> = {};
    let parsedGrading: Record<string, unknown> | null = null;

    try {
      parsedContent = JSON.parse(contentJson);
    } catch {
      setJsonError('Cấu trúc Content JSON không hợp lệ. Vui lòng kiểm tra lại cú pháp.');
      return;
    }

    try {
      if (gradingJson.trim()) {
        parsedGrading = JSON.parse(gradingJson);
      }
    } catch {
      setJsonError('Cấu trúc Grading JSON không hợp lệ. Vui lòng kiểm tra lại cú pháp.');
      return;
    }

    const tags = skillTags
      .split(',')
      .map((s) => s.trim())
      .filter(Boolean);

    onSave({
      key: key.trim(),
      type,
      title: title.trim() || `Bài tập: ${EXERCISE_TYPE_CATALOG[type]?.labelVi}`,
      instructions: instructions.trim(),
      maxPoints: Number(maxPoints) || 0,
      gradingMode,
      skillTags: tags,
      content: parsedContent,
      grading: parsedGrading,
    });
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 p-4 overflow-y-auto">
      <div className="bg-white rounded-3xl border border-slate-200 shadow-2xl w-full max-w-3xl max-h-[90vh] flex flex-col my-8">
        {/* Modal Header */}
        <div className="flex items-center justify-between p-5 border-b border-slate-100">
          <div>
            <h3 className="text-base font-bold text-slate-900">
              {initialExercise ? 'Chỉnh sửa bài tập' : 'Thêm bài tập mới (16 loại)'}
            </h3>
            <p className="text-xs text-slate-500">
              Cấu hình nội dung hiển thị cho học viên và quy tắc chấm điểm.
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="p-1.5 text-slate-400 hover:text-slate-700 rounded-xl hover:bg-slate-100 cursor-pointer"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Modal Body */}
        <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto p-5 space-y-4">
          {jsonError && (
            <div className="p-3 bg-rose-50 border border-rose-200 text-rose-800 text-xs rounded-xl font-medium">
              {jsonError}
            </div>
          )}

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {/* Exercise Type */}
            <div className="space-y-1">
              <label className="text-xs font-bold text-slate-700">Loại bài tập (16 enum chuẩn)</label>
              <select
                value={type}
                onChange={(e) => handleTypeChange(e.target.value as ExerciseType)}
                className="w-full text-xs font-semibold rounded-xl border border-slate-200 p-2.5 bg-slate-50 focus:bg-white focus:ring-2 focus:ring-emerald-500"
              >
                {Object.values(ExerciseType).map((t) => {
                  const meta = EXERCISE_TYPE_CATALOG[t];
                  return (
                    <option key={t} value={t}>
                      {meta.typeOrdinal}. {meta.labelVi} ({meta.group})
                    </option>
                  );
                })}
              </select>
            </div>

            {/* Key / Slug */}
            <div className="space-y-1">
              <label className="text-xs font-bold text-slate-700">Khóa bài tập (Key/ID duy nhất)</label>
              <input
                type="text"
                required
                value={key}
                onChange={(e) => setKey(e.target.value)}
                className="w-full text-xs font-mono rounded-xl border border-slate-200 p-2.5 bg-slate-50 focus:bg-white focus:ring-2 focus:ring-emerald-500"
              />
            </div>

            {/* Title */}
            <div className="space-y-1">
              <label className="text-xs font-bold text-slate-700">Tiêu đề bài tập</label>
              <input
                type="text"
                required
                placeholder="vd: Gọi món tại nhà hàng Ý"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                className="w-full text-xs rounded-xl border border-slate-200 p-2.5 bg-slate-50 focus:bg-white focus:ring-2 focus:ring-emerald-500"
              />
            </div>

            {/* Max Points & Grading Mode */}
            <div className="grid grid-cols-2 gap-2">
              <div className="space-y-1">
                <label className="text-xs font-bold text-slate-700">Điểm tối đa</label>
                <input
                  type="number"
                  min={0}
                  max={100}
                  step={0.5}
                  value={maxPoints}
                  onChange={(e) => setMaxPoints(Number(e.target.value))}
                  className="w-full text-xs rounded-xl border border-slate-200 p-2.5 bg-slate-50 focus:bg-white focus:ring-2 focus:ring-emerald-500"
                />
              </div>

              <div className="space-y-1">
                <label className="text-xs font-bold text-slate-700">Cơ chế chấm</label>
                <select
                  value={gradingMode}
                  onChange={(e) => setGradingMode(e.target.value as GradingMode)}
                  className="w-full text-xs rounded-xl border border-slate-200 p-2.5 bg-slate-50 focus:bg-white focus:ring-2 focus:ring-emerald-500"
                >
                  <option value="Deterministic">Deterministic (Đáp án chuẩn)</option>
                  <option value="RubricAssessment">RubricAssessment (AI chấm)</option>
                  <option value="SelfAssessment">SelfAssessment (Tự đánh giá)</option>
                  <option value="None">None</option>
                </select>
              </div>
            </div>
          </div>

          {/* Instructions */}
          <div className="space-y-1">
            <label className="text-xs font-bold text-slate-700">Hướng dẫn làm bài</label>
            <input
              type="text"
              placeholder="vd: Lắng nghe đoạn đối thoại và chọn phương án đúng nhất..."
              value={instructions}
              onChange={(e) => setInstructions(e.target.value)}
              className="w-full text-xs rounded-xl border border-slate-200 p-2.5 bg-slate-50 focus:bg-white focus:ring-2 focus:ring-emerald-500"
            />
          </div>

          {/* Template helper button */}
          <div className="flex items-center justify-between pt-1">
            <span className="text-xs font-bold text-slate-700 flex items-center gap-1.5">
              <Code className="w-4 h-4 text-emerald-600" /> Cấu hình dữ liệu JSON
            </span>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={handleApplyTemplate}
              className="gap-1.5 text-xs text-emerald-700 border-emerald-200 hover:bg-emerald-50"
            >
              <Sparkles className="w-3.5 h-3.5" /> Điền dữ liệu mẫu của {EXERCISE_TYPE_CATALOG[type]?.labelVi}
            </Button>
          </div>

          {/* Content JSON & Grading JSON */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-slate-600">
                Content JSON (Được gửi cho người học):
              </label>
              <textarea
                rows={9}
                value={contentJson}
                onChange={(e) => setContentJson(e.target.value)}
                className="w-full text-xs font-mono bg-slate-900 text-emerald-400 p-3 rounded-2xl border border-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-500 leading-normal"
              />
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-slate-600">
                Grading JSON (Bảo mật, chỉ lưu trên server để chấm):
              </label>
              <textarea
                rows={9}
                value={gradingJson}
                onChange={(e) => setGradingJson(e.target.value)}
                className="w-full text-xs font-mono bg-slate-900 text-sky-400 p-3 rounded-2xl border border-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-500 leading-normal"
              />
            </div>
          </div>

          {/* Modal Footer */}
          <div className="flex items-center justify-end gap-2 pt-3 border-t border-slate-100">
            <Button type="button" variant="outline" onClick={onClose} disabled={isSaving}>
              Hủy bỏ
            </Button>
            <Button type="submit" variant="primary" disabled={isSaving} className="gap-1.5">
              <Check className="w-4 h-4" />
              {isSaving ? 'Đang lưu bài tập...' : 'Lưu bài tập'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
