import { apiClient } from '@/lib/axios'
import type {
  TopicResponse,
  CreateTopicRequest,
  UpdateTopicRequest,
  VocabularyResponse,
  CreateVocabularyRequest,
  UpdateVocabularyRequest,
  GrammarResponse,
  CreateGrammarRequest,
  UpdateGrammarRequest,
  ExerciseResponse,
  CreateExerciseRequest,
  UpdateExerciseRequest,
  AddExerciseOptionRequest,
  ConversationResponse,
  CreateConversationRequest,
  UpdateConversationRequest,
  AddConversationLineRequest,
  UpdateConversationLineRequest,
  ReorderConversationLinesRequest,
  ExampleSentenceResponse,
  CreateExampleSentenceRequest,
  UpdateExampleSentenceRequest,
  PaginatedResult,
  GenerateVocabularyRequest,
  GenerateGrammarRequest,
  GenerateConversationRequest,
  GenerateExerciseRequest,
  GenerateExampleSentenceRequest,
  LanguageResponse,
  CreateLanguageRequest,
  UpdateLanguageRequest,
} from '../types/curriculum.types'

// ── Topics ───────────────────────────────────────────────────────────────────

export const getTopics = async (params: { languageCode: string; searchTerm?: string }): Promise<TopicResponse[]> => {
  const { data } = await apiClient.get<TopicResponse[]>('/api/v1/curriculum/topics', { params })
  return data
}

export const getTopic = async (id: string): Promise<TopicResponse> => {
  const { data } = await apiClient.get<TopicResponse>(`/api/v1/curriculum/topics/${id}`)
  return data
}

export const createTopic = async (body: CreateTopicRequest): Promise<string> => {
  const { data } = await apiClient.post<string>('/api/v1/admin/curriculum/topics', body)
  return data
}

export const updateTopic = async (id: string, body: UpdateTopicRequest): Promise<void> => {
  await apiClient.put(`/api/v1/admin/curriculum/topics/${id}`, { ...body, id })
}

export const deleteTopic = async (id: string): Promise<void> => {
  await apiClient.delete(`/api/v1/admin/curriculum/topics/${id}`)
}

// ── Vocabularies ─────────────────────────────────────────────────────────────

export const getVocabularies = async (params: {
  languageCode?: string
  page?: number
  pageSize?: number
  searchTerm?: string
  level?: string
  topicId?: string
}): Promise<PaginatedResult<VocabularyResponse>> => {
  const { data } = await apiClient.get<PaginatedResult<VocabularyResponse>>('/api/v1/curriculum/vocabularies', { params })
  return data
}

export const getVocabulary = async (id: string): Promise<VocabularyResponse> => {
  const { data } = await apiClient.get<VocabularyResponse>(`/api/v1/curriculum/vocabularies/${id}`)
  return data
}

export const createVocabulary = async (body: CreateVocabularyRequest): Promise<string> => {
  const { data } = await apiClient.post<string>('/api/v1/admin/curriculum/vocabularies', body)
  return data
}

export const updateVocabulary = async (id: string, body: UpdateVocabularyRequest): Promise<void> => {
  await apiClient.put(`/api/v1/admin/curriculum/vocabularies/${id}`, { ...body, id })
}

export const deleteVocabulary = async (id: string): Promise<void> => {
  await apiClient.delete(`/api/v1/admin/curriculum/vocabularies/${id}`)
}

// ── Grammars ─────────────────────────────────────────────────────────────────

export const getGrammars = async (params: { languageCode?: string; page?: number; pageSize?: number; searchTerm?: string }): Promise<PaginatedResult<GrammarResponse>> => {
  const { data } = await apiClient.get<PaginatedResult<GrammarResponse>>('/api/v1/curriculum/grammars', { params })
  return data
}

export const getGrammar = async (id: string): Promise<GrammarResponse> => {
  const { data } = await apiClient.get<GrammarResponse>(`/api/v1/curriculum/grammars/${id}`)
  return data
}

export const createGrammar = async (body: CreateGrammarRequest): Promise<string> => {
  const { data } = await apiClient.post<string>('/api/v1/admin/curriculum/grammars', body)
  return data
}

export const updateGrammar = async (id: string, body: UpdateGrammarRequest): Promise<void> => {
  await apiClient.put(`/api/v1/admin/curriculum/grammars/${id}`, { ...body, id })
}

export const deleteGrammar = async (id: string): Promise<void> => {
  await apiClient.delete(`/api/v1/admin/curriculum/grammars/${id}`)
}

// ── Exercises ────────────────────────────────────────────────────────────────

export const getExercises = async (params: {
  languageCode: string
  page?: number
  pageSize?: number
  level?: string
  topicId?: string
  vocabularyId?: string
  grammarId?: string
  exerciseType?: number
}): Promise<PaginatedResult<ExerciseResponse>> => {
  const { data } = await apiClient.get<PaginatedResult<ExerciseResponse>>('/api/v1/curriculum/exercises', { params })
  return data
}

export const getExercise = async (id: string): Promise<ExerciseResponse> => {
  const { data } = await apiClient.get<ExerciseResponse>(`/api/v1/curriculum/exercises/${id}`)
  return data
}


export const submitStandaloneExercise = async (id: string, isCorrect: boolean): Promise<void> => {
  await apiClient.post(`/api/v1/curriculum/exercises/${id}/submit`, { isCorrect })
}

export const createExercise = async (body: CreateExerciseRequest): Promise<string> => {
  const { data } = await apiClient.post<string>('/api/v1/admin/curriculum/exercises', body)
  return data
}

export const addExerciseOption = async (exerciseId: string, body: AddExerciseOptionRequest): Promise<void> => {
  await apiClient.post(`/api/v1/admin/curriculum/exercises/${exerciseId}/options`, body)
}

export const updateExercise = async (id: string, body: UpdateExerciseRequest): Promise<void> => {
  await apiClient.put(`/api/v1/admin/curriculum/exercises/${id}`, { ...body, id })
}

export const deleteExercise = async (id: string): Promise<void> => {
  await apiClient.delete(`/api/v1/admin/curriculum/exercises/${id}`)
}

// ── Conversations ────────────────────────────────────────────────────────────

export const getConversations = async (params: { languageCode: string; page?: number; pageSize?: number; topicId?: string; level?: string }): Promise<PaginatedResult<ConversationResponse>> => {
  const { data } = await apiClient.get<PaginatedResult<ConversationResponse>>('/api/v1/curriculum/conversations', { params })
  return data
}

export const getConversation = async (id: string): Promise<ConversationResponse> => {
  const { data } = await apiClient.get<ConversationResponse>(`/api/v1/curriculum/conversations/${id}`)
  return data
}

export const createConversation = async (body: CreateConversationRequest): Promise<string> => {
  const { data } = await apiClient.post<string>('/api/v1/admin/curriculum/conversations', body)
  return data
}

export const addConversationLine = async (conversationId: string, body: AddConversationLineRequest): Promise<void> => {
  await apiClient.post(`/api/v1/admin/curriculum/conversations/${conversationId}/lines`, body)
}

export const updateConversationLine = async (conversationId: string, lineId: string, body: UpdateConversationLineRequest): Promise<void> => {
  await apiClient.put(`/api/v1/admin/curriculum/conversations/${conversationId}/lines/${lineId}`, body)
}

export const deleteConversationLine = async (conversationId: string, lineId: string): Promise<void> => {
  await apiClient.delete(`/api/v1/admin/curriculum/conversations/${conversationId}/lines/${lineId}`)
}

export const reorderConversationLines = async (conversationId: string, body: ReorderConversationLinesRequest): Promise<void> => {
  await apiClient.put(`/api/v1/admin/curriculum/conversations/${conversationId}/lines/reorder`, body)
}

export const updateConversation = async (id: string, body: UpdateConversationRequest): Promise<void> => {
  await apiClient.put(`/api/v1/admin/curriculum/conversations/${id}`, { ...body, id })
}

export const deleteConversation = async (id: string): Promise<void> => {
  await apiClient.delete(`/api/v1/admin/curriculum/conversations/${id}`)
}

// ── Example Sentences ────────────────────────────────────────────────────────

export const getExampleSentences = async (params: {
  languageCode?: string
  page?: number
  pageSize?: number
  searchTerm?: string
  vocabularyId?: string
  grammarId?: string
  topicId?: string
}): Promise<PaginatedResult<ExampleSentenceResponse>> => {
  const { data } = await apiClient.get<PaginatedResult<ExampleSentenceResponse>>('/api/v1/curriculum/example-sentences', { params })
  return data
}

export const createExampleSentence = async (body: CreateExampleSentenceRequest): Promise<string> => {
  const { data } = await apiClient.post<string>('/api/v1/admin/curriculum/example-sentences', body)
  return data
}

export const updateExampleSentence = async (id: string, body: UpdateExampleSentenceRequest): Promise<void> => {
  await apiClient.put(`/api/v1/admin/curriculum/example-sentences/${id}`, { ...body, id })
}

export const deleteExampleSentence = async (id: string): Promise<void> => {
  await apiClient.delete(`/api/v1/admin/curriculum/example-sentences/${id}`)
}

// ── Languages ────────────────────────────────────────────────────────────────

export const getLanguages = async (): Promise<LanguageResponse[]> => {
  const { data } = await apiClient.get<LanguageResponse[]>('/api/v1/curriculum/languages')
  return data
}

export const createLanguage = async (body: CreateLanguageRequest): Promise<string> => {
  const { data } = await apiClient.post<{ id: string }>('/api/v1/curriculum/languages', body)
  return data.id
}

export const updateLanguage = async (id: string, body: UpdateLanguageRequest): Promise<void> => {
  await apiClient.put(`/api/v1/curriculum/languages/${id}`, body)
}

export const deleteLanguage = async (id: string): Promise<void> => {
  await apiClient.delete(`/api/v1/curriculum/languages/${id}`)
}

// ── AI Generate ──────────────────────────────────────────────────────────────

export const generateVocabulary = async (body: GenerateVocabularyRequest): Promise<VocabularyResponse[]> => {
  const { data } = await apiClient.post<VocabularyResponse[]>('/api/v1/admin/curriculum/generate/vocabulary', body)
  return data
}

export const generateGrammar = async (body: GenerateGrammarRequest): Promise<GrammarResponse[]> => {
  const { data } = await apiClient.post<GrammarResponse[]>('/api/v1/admin/curriculum/generate/grammar', body)
  return data
}

export const generateConversation = async (body: GenerateConversationRequest): Promise<ConversationResponse[]> => {
  const { data } = await apiClient.post<ConversationResponse[]>('/api/v1/admin/curriculum/generate/conversation', body)
  return data
}

export const generateExercise = async (body: GenerateExerciseRequest): Promise<ExerciseResponse[]> => {
  const { data } = await apiClient.post<ExerciseResponse[]>('/api/v1/admin/curriculum/generate/exercise', body)
  return data
}

export const generateExampleSentences = async (body: GenerateExampleSentenceRequest): Promise<ExampleSentenceResponse[]> => {
  const { data } = await apiClient.post<ExampleSentenceResponse[]>('/api/v1/admin/curriculum/generate/example-sentences', body)
  return data
}

// ── Admin Audio Generate ─────────────────────────────────────────────────────

export const generateLineAudio = async (conversationId: string, lineId: string, body?: { voice?: string }): Promise<{ audioUrl: string }> => {
  const { data } = await apiClient.post<{ audioUrl: string }>(`/api/v1/admin/curriculum/conversations/${conversationId}/lines/${lineId}/generate-audio`, body ?? {})
  return data
}

export const generateAllAudio = async (conversationId: string, body?: { voice?: string }): Promise<{ totalLines: number; generatedCount: number; skippedCount: number; generatedLines: { lineId: string; audioUrl: string }[] }> => {
  const { data } = await apiClient.post(`/api/v1/admin/curriculum/conversations/${conversationId}/generate-all-audio`, body ?? {})
  return data
}

