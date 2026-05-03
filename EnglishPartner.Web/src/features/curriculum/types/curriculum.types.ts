export interface TopicResponse {
  id: string
  title: string
  description: string | null
  level: string | null
  imageUrl: string | null
  sortOrder: number
}

export interface CreateTopicRequest {
  title: string
  languageCode: string
  description?: string | null
  level?: string | null
  imageUrl?: string | null
  sortOrder: number
}

export interface UpdateTopicRequest extends CreateTopicRequest {}

export interface VocabularyResponse {
  id: string
  word: string
  definition: string
  languageCode: string
  pronunciation: string | null
  phoneticTranscription: string | null
  partOfSpeech: string | null
  imageUrl: string | null
  audioUrl: string | null
  level: string | null
  topicId: string | null
  synonyms: string[]
}

export interface CreateVocabularyRequest {
  word: string
  definition: string
  languageCode: string
  pronunciation?: string | null
  phoneticTranscription?: string | null
  partOfSpeech?: string | null
  level?: string | null
  topicId?: string | null
  synonyms?: string[]
}

export interface UpdateVocabularyRequest extends CreateVocabularyRequest {}

export interface ExampleTranslationResponse {
  languageCode: string
  translatedText: string
}

export interface ExampleSentenceResponse {
  id: string
  text: string
  audioUrl: string | null
  note: string | null
  level: string | null
  sortOrder?: number
  translations: ExampleTranslationResponse[]
}

export interface CreateExampleSentenceRequest {
  text: string
  languageCode: string
  audioUrl?: string | null
  note?: string | null
  level?: string | null
  vocabularyId?: string | null
  grammarId?: string | null
  topicId?: string | null
  translations?: Record<string, string> | null
}

export interface UpdateExampleSentenceRequest {
  text: string
  audioUrl?: string | null
  note?: string | null
  level?: string | null
  vocabularyId?: string | null
  grammarId?: string | null
  topicId?: string | null
  translations?: Record<string, string> | null
}

export interface GrammarResponse {
  id: string
  title: string
  languageCode: string
  level: string | null
  explanation: string | null
  formula: string | null
  topicId: string | null
  examples?: ExampleSentenceResponse[]
}

export interface CreateGrammarRequest {
  title: string
  languageCode: string
  level?: string | null
  explanation?: string | null
  formula?: string | null
  topicId?: string | null
}

export interface UpdateGrammarRequest extends CreateGrammarRequest {}

export interface ExerciseOptionResponse {
  id: string
  text: string
  isCorrect: boolean
  sortOrder: number
}

export interface ExerciseResponse {
  id: string
  title: string
  instruction: string
  type: string
  correctAnswer: string
  languageCode: string
  level: string | null
  topicId: string | null
  vocabularyId: string | null
  grammarId: string | null
  content: string | null
  explanation: string | null
  xpReward: number
  options?: ExerciseOptionResponse[]
}

export interface CreateExerciseRequest {
  title: string
  instruction: string
  type: number
  correctAnswer: string
  languageCode: string
  level?: string | null
  topicId?: string | null
  vocabularyId?: string | null
  grammarId?: string | null
  content?: string | null
  explanation?: string | null
  xpReward: number
}

export interface UpdateExerciseRequest extends CreateExerciseRequest {}

export interface AddExerciseOptionRequest {
  text: string
  isCorrect: boolean
}

export interface ConversationLineResponse {
  id: string
  speaker: string
  text: string
  audioUrl: string | null
  sortOrder: number
  translations: ExampleTranslationResponse[]
}

export interface ConversationResponse {
  id: string
  title: string
  languageCode: string
  scenario: string | null
  level: string | null
  imageUrl: string | null
  topicId: string | null
  grammarId: string | null
  lines?: ConversationLineResponse[]
}

export interface CreateConversationRequest {
  title: string
  languageCode: string
  scenario?: string | null
  level?: string | null
  imageUrl?: string | null
  topicId?: string | null
  grammarId?: string | null
}

export interface UpdateConversationRequest extends CreateConversationRequest {}

export interface AddConversationLineRequest {
  speaker: string
  text: string
  translations?: Record<string, string> | null
  audioUrl?: string | null
}

export interface UpdateConversationLineRequest extends AddConversationLineRequest {}

export interface ReorderConversationLinesRequest {
  orderedLineIds: string[]
}

export interface PaginatedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

// ── Languages ────────────────────────────────────────────────────────────────

export interface LanguageResponse {
  id: string
  code: string
  name: string
  nativeName: string
  sortOrder: number
}

export interface CreateLanguageRequest {
  code: string
  name: string
  nativeName: string
  sortOrder: number
}

export interface UpdateLanguageRequest {
  name: string
  nativeName: string
  sortOrder: number
}

// ── AI Generate Request Types ────────────────────────────────────────────────

export interface GenerateVocabularyRequest {
  targetLanguage: string
  wordHint: string
  count?: number
  level?: string | null
  topicId?: string | null
  autoGenerateExamples?: boolean
  exampleCount?: number
  exampleTargetLanguageCodes?: string[]
}

export interface GenerateGrammarRequest {
  targetLanguage: string
  title: string
  count?: number
  level?: string | null
  topicId?: string | null
  exampleCount?: number
  targetLanguageCodes?: string[]
}

export interface GenerateConversationRequest {
  targetLanguage: string
  topicName: string
  count?: number
  level?: string | null
  scenario?: string | null
  lineCount?: number
  targetLanguageCodes?: string[]
  topicId?: string | null
  grammarId?: string | null
}

export interface GenerateExerciseRequest {
  targetLanguage: string
  context: string
  exerciseType: number
  count?: number
  level?: string | null
  topicId?: string | null
  vocabularyId?: string | null
  grammarId?: string | null
  xpReward?: number
}

export interface GenerateExampleSentenceRequest {
  targetLanguage: string
  context: string
  count: number
  level?: string | null
  targetLanguageCodes?: string[]
  topicId?: string | null
  vocabularyId?: string | null
  grammarId?: string | null
}
