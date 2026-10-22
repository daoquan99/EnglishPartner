/**
 * Canonical 16 Exercise Types Enum as specified in lesson-exercises-spec.md
 */
export enum ExerciseType {
  Flashcards = 'Flashcards',
  MultipleChoice = 'MultipleChoice',
  FillInTheBlanks = 'FillInTheBlanks',
  Matching = 'Matching',
  SpellingPractice = 'SpellingPractice',
  SentenceUnscramble = 'SentenceUnscramble',
  ErrorCorrection = 'ErrorCorrection',
  Translation = 'Translation',
  TrueFalse = 'TrueFalse',
  ReadingComprehension = 'ReadingComprehension',
  ListeningComprehension = 'ListeningComprehension',
  Dictation = 'Dictation',
  Shadowing = 'Shadowing',
  SpeakingPractice = 'SpeakingPractice',
  WritingPractice = 'WritingPractice',
  ConversationRolePlay = 'ConversationRolePlay',
}

export type ExerciseGroup = 'Learn' | 'Practice' | 'Skills';
export type GradingMode = 'Deterministic' | 'SelfAssessment' | 'RubricAssessment' | 'None';
export type LessonStatus = 'Draft' | 'Published' | 'Archived';
export type CefrLevel = 'A1' | 'A2' | 'B1' | 'B2' | 'C1' | 'C2' | 'All';

export interface ExerciseTypeMetadata {
  type: ExerciseType;
  typeOrdinal: number;
  group: ExerciseGroup;
  labelVi: string;
  labelEn: string;
  defaultGradingMode: GradingMode;
  requiresMedia: boolean;
  descriptionVi: string;
  descriptionEn: string;
}

export const EXERCISE_TYPE_CATALOG: Record<ExerciseType, ExerciseTypeMetadata> = {
  [ExerciseType.Flashcards]: {
    type: ExerciseType.Flashcards,
    typeOrdinal: 1,
    group: 'Learn',
    labelVi: 'Thẻ ghi nhớ (Flashcards)',
    labelEn: 'Flashcards',
    defaultGradingMode: 'SelfAssessment',
    requiresMedia: false,
    descriptionVi: 'Học từ vựng qua lật thẻ hai mặt, kèm phát âm và tự đánh giá độ nhớ.',
    descriptionEn: 'Learn vocabulary via double-sided cards, pronunciation audio, and self-ratings.',
  },
  [ExerciseType.MultipleChoice]: {
    type: ExerciseType.MultipleChoice,
    typeOrdinal: 2,
    group: 'Practice',
    labelVi: 'Trắc nghiệm chọn đáp án',
    labelEn: 'Multiple Choice',
    defaultGradingMode: 'Deterministic',
    requiresMedia: false,
    descriptionVi: 'Chọn một hoặc nhiều phương án đúng cho câu hỏi kiểm tra kiến thức.',
    descriptionEn: 'Select single or multiple correct answers for the given question prompt.',
  },
  [ExerciseType.FillInTheBlanks]: {
    type: ExerciseType.FillInTheBlanks,
    typeOrdinal: 3,
    group: 'Practice',
    labelVi: 'Điền vào chỗ trống',
    labelEn: 'Fill in the Blanks',
    defaultGradingMode: 'Deterministic',
    requiresMedia: false,
    descriptionVi: 'Điền từ hoặc cụm từ còn thiếu vào ngữ cảnh đoạn văn câu chữ.',
    descriptionEn: 'Fill in missing words or phrases in the given context passages.',
  },
  [ExerciseType.Matching]: {
    type: ExerciseType.Matching,
    typeOrdinal: 4,
    group: 'Practice',
    labelVi: 'Nối cặp từ / khái niệm',
    labelEn: 'Matching',
    defaultGradingMode: 'Deterministic',
    requiresMedia: false,
    descriptionVi: 'Nối tương ứng giữa từ vựng và định nghĩa, từ đồng nghĩa hoặc hình ảnh.',
    descriptionEn: 'Pair corresponding words with definitions, synonyms, or images.',
  },
  [ExerciseType.SpellingPractice]: {
    type: ExerciseType.SpellingPractice,
    typeOrdinal: 5,
    group: 'Practice',
    labelVi: 'Luyện chính tả từ vựng',
    labelEn: 'Spelling Practice',
    defaultGradingMode: 'Deterministic',
    requiresMedia: false,
    descriptionVi: 'Nhập chính xác ký tự từ vựng theo gợi ý nghĩa hoặc phát âm.',
    descriptionEn: 'Type correct word spelling based on definitions or pronunciation cues.',
  },
  [ExerciseType.SentenceUnscramble]: {
    type: ExerciseType.SentenceUnscramble,
    typeOrdinal: 6,
    group: 'Practice',
    labelVi: 'Sắp xếp câu hoàn chỉnh',
    labelEn: 'Sentence Unscramble',
    defaultGradingMode: 'Deterministic',
    requiresMedia: false,
    descriptionVi: 'Sắp xếp các khối từ bị xáo trộn thành câu đúng ngữ pháp tiếng Anh.',
    descriptionEn: 'Reorder shuffled token blocks into grammatically sound English sentences.',
  },
  [ExerciseType.ErrorCorrection]: {
    type: ExerciseType.ErrorCorrection,
    typeOrdinal: 7,
    group: 'Practice',
    labelVi: 'Tìm và sửa lỗi sai',
    labelEn: 'Error Correction',
    defaultGradingMode: 'Deterministic',
    requiresMedia: false,
    descriptionVi: 'Phát hiện lỗi ngữ pháp hoặc chính tả trong câu và viết lại cho chuẩn.',
    descriptionEn: 'Identify grammatical or lexical errors and submit the corrected sentence.',
  },
  [ExerciseType.Translation]: {
    type: ExerciseType.Translation,
    typeOrdinal: 8,
    group: 'Practice',
    labelVi: 'Dịch câu Anh - Việt',
    labelEn: 'Translation',
    defaultGradingMode: 'RubricAssessment',
    requiresMedia: false,
    descriptionVi: 'Dịch câu hai chiều Anh-Việt hoặc Việt-Anh, được AI chấm theo rubric.',
    descriptionEn: 'Translate sentences bidirectionally with AI rubric-based evaluation.',
  },
  [ExerciseType.TrueFalse]: {
    type: ExerciseType.TrueFalse,
    typeOrdinal: 9,
    group: 'Practice',
    labelVi: 'Đúng / Sai theo ngữ cảnh',
    labelEn: 'True / False',
    defaultGradingMode: 'Deterministic',
    requiresMedia: false,
    descriptionVi: 'Đọc thông tin và xác định các nhận định là Đúng (True) hay Sai (False).',
    descriptionEn: 'Evaluate whether statements are True or False according to context.',
  },
  [ExerciseType.ReadingComprehension]: {
    type: ExerciseType.ReadingComprehension,
    typeOrdinal: 10,
    group: 'Skills',
    labelVi: 'Đọc hiểu đoạn văn',
    labelEn: 'Reading Comprehension',
    defaultGradingMode: 'Deterministic',
    requiresMedia: false,
    descriptionVi: 'Đọc bài đọc dài và trả lời tổ hợp các câu hỏi trắc nghiệm, đúng/sai, tự luận.',
    descriptionEn: 'Read contextual text and answer mixed composite questions.',
  },
  [ExerciseType.ListeningComprehension]: {
    type: ExerciseType.ListeningComprehension,
    typeOrdinal: 11,
    group: 'Skills',
    labelVi: 'Nghe hiểu hội thoại / audio',
    labelEn: 'Listening Comprehension',
    defaultGradingMode: 'Deterministic',
    requiresMedia: true,
    descriptionVi: 'Nghe audio hội thoại hoặc thông báo rồi trả lời các câu hỏi nghe hiểu.',
    descriptionEn: 'Listen to recorded audio clips and answer comprehension sub-questions.',
  },
  [ExerciseType.Dictation]: {
    type: ExerciseType.Dictation,
    typeOrdinal: 12,
    group: 'Skills',
    labelVi: 'Nghe chép chính tả (Dictation)',
    labelEn: 'Dictation',
    defaultGradingMode: 'Deterministic',
    requiresMedia: true,
    descriptionVi: 'Nghe từng câu và gõ lại chính xác, chấm bằng khoảng cách Levenshtein từ.',
    descriptionEn: 'Listen and transcribe audio text, scored via word-level Levenshtein distance.',
  },
  [ExerciseType.Shadowing]: {
    type: ExerciseType.Shadowing,
    typeOrdinal: 13,
    group: 'Skills',
    labelVi: 'Luyện nói nhại (Shadowing)',
    labelEn: 'Shadowing',
    defaultGradingMode: 'RubricAssessment',
    requiresMedia: true,
    descriptionVi: 'Lắng nghe giọng bản xứ và ghi âm nói theo để luyện nhịp điệu và ngữ điệu.',
    descriptionEn: 'Listen to native audio and shadow speak to master rhythm and intonation.',
  },
  [ExerciseType.SpeakingPractice]: {
    type: ExerciseType.SpeakingPractice,
    typeOrdinal: 14,
    group: 'Skills',
    labelVi: 'Luyện nói theo chủ đề',
    labelEn: 'Speaking Practice',
    defaultGradingMode: 'RubricAssessment',
    requiresMedia: false,
    descriptionVi: 'Ghi âm bài nói theo gợi ý và tiêu chí, nhận phân tích phát âm và nội dung.',
    descriptionEn: 'Record open-ended spoken responses scored against CEFR speaking rubrics.',
  },
  [ExerciseType.WritingPractice]: {
    type: ExerciseType.WritingPractice,
    typeOrdinal: 15,
    group: 'Skills',
    labelVi: 'Luyện viết đoạn văn',
    labelEn: 'Writing Practice',
    defaultGradingMode: 'RubricAssessment',
    requiresMedia: false,
    descriptionVi: 'Viết bài theo yêu cầu đề bài và độ dài mục tiêu, AI chấm tiêu chuẩn CEFR.',
    descriptionEn: 'Write essays or paragraphs evaluated by AI against writing criteria rubrics.',
  },
  [ExerciseType.ConversationRolePlay]: {
    type: ExerciseType.ConversationRolePlay,
    typeOrdinal: 16,
    group: 'Skills',
    labelVi: 'Đóng vai hội thoại tương tác',
    labelEn: 'Conversation Role-play',
    defaultGradingMode: 'RubricAssessment',
    requiresMedia: false,
    descriptionVi: 'Tương tác đối thoại nhiều lượt theo vai diễn tình huống thực tế với gia sư AI.',
    descriptionEn: 'Multi-turn interactive scenario role-play conversation with AI tutor.',
  },
};

// ==========================================
// DTOs matching backend schema & endpoints
// ==========================================

export interface LessonSummaryDto {
  id: string;
  title: string;
  cefrLevel: string;
  topic: string;
  content: string;
  estimatedMinutes: number;
  status: LessonStatus;
  version: number;
  exerciseCount: number;
  totalMaxPoints: number;
  publishedAtUtc: string | null;
  rowVersion: string | null;
}

export interface ExerciseAuthorDto {
  id: string;
  key: string;
  type: ExerciseType;
  title: string;
  instructions: string;
  order: number;
  maxPoints: number;
  gradingMode: GradingMode;
  skillTags?: string[];
  content: Record<string, unknown>;
  grading?: Record<string, unknown>;
}

export interface ExerciseLearnerDto {
  id: string;
  key: string;
  type: ExerciseType;
  title: string;
  instructions: string;
  order: number;
  maxPoints: number;
  gradingMode: GradingMode;
  content: Record<string, unknown>;
}

export interface LessonDetailAuthorDto {
  id: string;
  title: string;
  cefrLevel: string;
  topic: string;
  learningObjectives: string[];
  targetVocabulary: string[];
  targetGrammar: string[];
  content: string;
  estimatedMinutes: number;
  status: LessonStatus;
  version: number;
  rowVersion: string | null;
  publishedAtUtc: string | null;
  exercises: ExerciseAuthorDto[];
}

export interface LessonDetailLearnerDto {
  id: string;
  title: string;
  cefrLevel: string;
  topic: string;
  learningObjectives: string[];
  targetVocabulary: string[];
  targetGrammar: string[];
  content: string;
  estimatedMinutes: number;
  version: number;
  exercises: ExerciseLearnerDto[];
}

export interface CreateLessonDraftRequest {
  title: string;
  cefrLevel: string;
  topic: string;
  content?: string;
  estimatedMinutes: number;
  learningObjectives?: string[];
  targetVocabulary?: string[];
  targetGrammar?: string[];
}

export interface UpdateLessonDraftRequest {
  title: string;
  cefrLevel: string;
  topic: string;
  content?: string;
  estimatedMinutes: number;
  learningObjectives?: string[];
  targetVocabulary?: string[];
  targetGrammar?: string[];
  rowVersion?: string | null;
}

export interface AddExerciseRequest {
  key: string;
  type: ExerciseType;
  title: string;
  instructions: string;
  maxPoints: number;
  gradingMode: GradingMode;
  content: Record<string, unknown>;
  grading?: Record<string, unknown> | null;
  skillTags?: string[];
  order?: number;
}

export interface UpdateExerciseRequest {
  key: string;
  type: ExerciseType;
  title: string;
  instructions: string;
  maxPoints: number;
  gradingMode: GradingMode;
  content: Record<string, unknown>;
  grading?: Record<string, unknown> | null;
  skillTags?: string[];
}

export interface PublishedLessonResultDto {
  lessonId: string;
  version: number;
  status: string;
  publishedAt: string;
  rowVersion: string | null;
  exerciseCount: number;
  totalMaxPoints: number;
}

export interface DraftRevisionResultDto {
  draftLessonId: string;
  publishedParentLessonId: string;
  version: number;
  status: string;
  rowVersion: string | null;
}

export interface CriterionScoreBreakdownDto {
  criterionCode: string;
  name: string;
  weight: number;
  rawScore: number;
  descriptor: string;
  feedback: string;
}

export interface AttemptProgressDto {
  completedExercisesCount: number;
  totalExercisesCount: number;
  pendingGradingCount: number;
  failedGradingCount: number;
  currentSubtotalPoints: number;
  totalMaxPoints: number;
  finalScore100?: number | null;
}

export interface LessonAiExecutionConfigDto {
  apiKey?: string | null;
  assessmentModelCode?: string | null;
}

export interface DictationWordSubstitutionDto {
  expected: string;
  actual: string;
}

export interface DictationAiReviewDto {
  reviewId: string;
  answerId: string;
  status: 'Completed' | 'Failed' | string;
  feedback?: string | null;
  missingWords?: string[] | null;
  extraWords?: string[] | null;
  wordSubstitutions?: DictationWordSubstitutionDto[] | null;
  explanationsVi?: string | null;
  actionableImprovements?: string[] | null;
  errorMessage?: string | null;
  reviewedAtUtc?: string | null;
}

export interface RequestDictationAiReviewRequest {
  aiConfig?: LessonAiExecutionConfigDto | null;
  expectedSubmissionHash?: string | null;
}

export interface ExerciseAnswerResultDto {
  answerId: string;
  attemptId: string;
  attemptRevision: number;
  exerciseId: string;
  subQuestionId?: string | null;
  status: 'Completed' | 'Pending' | 'Grading' | 'Failed' | 'PendingGrading' | 'FailedGrading' | 'NotAssessable';
  gradingMode: GradingMode;
  score100?: number | null;
  pointsEarned?: number | null;
  maxPoints: number;
  rubricCode?: string | null;
  rubricVersion?: number | null;
  criteriaBreakdown?: CriterionScoreBreakdownDto[] | null;
  strengths?: string[] | null;
  improvements?: string[] | null;
  suggestedAnswer?: string | null;
  feedback?: string | null;
  attemptProgress: AttemptProgressDto;
  errorMessage?: string | null;
  answerPayload?: Record<string, unknown> | null;
  aiReview?: DictationAiReviewDto | null;
}

export interface SubmittedAnswerSummaryDto {
  answerId: string;
  status: string;
  score100?: number | null;
  pointsEarned?: number | null;
  maxPoints: number;
  feedback?: string | null;
  errorMessage?: string | null;
  answerPayload?: Record<string, unknown> | null;
  aiReview?: DictationAiReviewDto | null;
  criteriaBreakdown?: CriterionScoreBreakdownDto[] | null;
  exerciseId?: string | null;
  subQuestionId?: string | null;
}

export interface LessonAttemptResumeDto {
  attemptId: string;
  lessonId: string;
  lessonTitle: string;
  pinnedLessonVersion: number;
  attemptRevision: number;
  status: 'InProgress' | 'Completed' | 'Abandoned';
  startedAt: string;
  completedAt?: string | null;
  progress: AttemptProgressDto;
  exercises: ExerciseLearnerDto[];
  submittedAnswers: Record<string, SubmittedAnswerSummaryDto>;
}

export interface LessonAttemptSummaryDto {
  attemptId: string;
  lessonId: string;
  lessonTitle: string;
  pinnedLessonVersion: number;
  status: string;
  startedAt: string;
  completedAt?: string | null;
  currentSubtotalPoints: number;
  totalMaxPoints: number;
  finalScore100?: number | null;
}

// ==========================================
// Lesson Rubric Types
// ==========================================

export interface RubricCriterionDto {
  id: string;
  code: string;
  name: string;
  description: string;
  weight: number;
  displayOrder: number;
  descriptorLevel0: string;
  descriptorLevel1: string;
  descriptorLevel2: string;
  descriptorLevel3: string;
  descriptorLevel4: string;
}

export interface LessonRubricSummaryDto {
  id: string;
  code: string;
  name: string;
  description: string;
  version: number;
  assessmentScope: string;
  exerciseType?: string | null;
  subQuestionKind?: string | null;
  cefrLevel: string;
  applicableExerciseTypes?: string[] | null;
  isActive: boolean;
  criteriaCount: number;
}

export interface LessonRubricDetailDto {
  id: string;
  code: string;
  name: string;
  description: string;
  version: number;
  assessmentScope: string;
  exerciseType?: string | null;
  subQuestionKind?: string | null;
  cefrLevel: string;
  applicableExerciseTypes?: string[] | null;
  isActive: boolean;
  criteria: RubricCriterionDto[];
}

export interface CreateRubricCriterionRequest {
  code: string;
  name: string;
  description: string;
  weight: number;
  displayOrder: number;
  descriptorLevel0?: string;
  descriptorLevel1?: string;
  descriptorLevel2?: string;
  descriptorLevel3?: string;
  descriptorLevel4?: string;
}

export interface CreateLessonRubricRequest {
  code: string;
  name: string;
  description: string;
  exerciseType?: string | null;
  subQuestionKind?: string | null;
  cefrLevel: string;
  applicableExerciseTypes?: string[] | null;
  criteria: CreateRubricCriterionRequest[];
}

// ==========================================
// Candidate Import & Validation Types
// ==========================================

export interface ValidationIssueDto {
  path: string;
  severity: 'Error' | 'Warning';
  message: string;
}

export interface LessonCandidateValidationResultDto {
  isValid: boolean;
  issues: ValidationIssueDto[];
  candidate?: Record<string, unknown> | null;
}

export interface CommitLessonImportRequest {
  targetLessonId?: string | null;
  targetRevision?: number | null;
  commitMode: 'Append' | 'Replace';
  candidate: Record<string, unknown>;
}
