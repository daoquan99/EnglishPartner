import { apiClient } from '@/lib/axios'
import type {
  DashboardResponse,
  LeaderboardEntry,
  ReviewDueItem,
  SubmitReviewRequest,
  StartSessionRequest,
  SubmitAnswerRequest,
  SubmitAnswerResponse,
  CompleteSessionRequest,
  AdjustXpRequest,
  SubmitAudioAnswerRequest,
  SubmitAudioAnswerResponse,
  EvaluateConversationLineRequest,
  EvaluateConversationLineResponse,
  PronunciationEvaluationRequest,
  PronunciationEvaluationResponse,
} from '../types/learning.types'

// ── Dashboard & Leaderboard ──────────────────────────────────────────────────

export const getDashboard = async (): Promise<DashboardResponse> => {
  const { data } = await apiClient.get<DashboardResponse>('/api/v1/learning/dashboard')
  return data
}

export const getLeaderboard = async (count = 50): Promise<LeaderboardEntry[]> => {
  const { data } = await apiClient.get<LeaderboardEntry[]>('/api/v1/learning/leaderboard', { params: { count } })
  return data
}

// ── Reviews ──────────────────────────────────────────────────────────────────

export const getReviewsDue = async (): Promise<ReviewDueItem[]> => {
  const { data } = await apiClient.get<ReviewDueItem[]>('/api/v1/learning/reviews/due')
  return data
}

export const submitReview = async (body: SubmitReviewRequest): Promise<void> => {
  await apiClient.post('/api/v1/learning/reviews', body)
}

// ── Study Sessions ───────────────────────────────────────────────────────────

export const startSession = async (body: StartSessionRequest): Promise<string> => {
  const { data } = await apiClient.post<string>('/api/v1/learning/sessions', body)
  return data
}

export const submitAnswer = async (sessionId: string, body: SubmitAnswerRequest): Promise<SubmitAnswerResponse> => {
  const { data } = await apiClient.post<SubmitAnswerResponse>(`/api/v1/learning/sessions/${sessionId}/answer`, body)
  return data
}

export const pingSession = async (sessionId: string): Promise<void> => {
  await apiClient.post(`/api/v1/learning/sessions/${sessionId}/ping`)
}

export const completeSession = async (sessionId: string, body: CompleteSessionRequest): Promise<void> => {
  await apiClient.put(`/api/v1/learning/sessions/${sessionId}/submit`, body)
}

export const submitAudioAnswer = async (sessionId: string, body: SubmitAudioAnswerRequest): Promise<SubmitAudioAnswerResponse> => {
  const formData = new FormData()
  formData.append('itemId', body.itemId)
  formData.append('itemType', body.itemType.toString())
  formData.append('timeSpentSeconds', body.timeSpentSeconds.toString())
  formData.append('audioFile', body.audioFile)

  const { data } = await apiClient.post<SubmitAudioAnswerResponse>(`/api/v1/learning/sessions/${sessionId}/answer/audio`, formData)
  return data
}

// ── Conversations ────────────────────────────────────────────────────────────

export const evaluateConversationLine = async (body: EvaluateConversationLineRequest): Promise<EvaluateConversationLineResponse> => {
  const formData = new FormData()
  formData.append('conversationId', body.conversationId)
  formData.append('lineId', body.lineId)
  formData.append('audioFile', body.audioFile)

  const { data } = await apiClient.post<EvaluateConversationLineResponse>('/api/v1/learning/conversations/evaluate-line', formData, {
    timeout: 30000
  })
  return data
}

// ── Pronunciation ────────────────────────────────────────────────────────────

export const evaluatePronunciation = async (body: PronunciationEvaluationRequest): Promise<PronunciationEvaluationResponse> => {
  const formData = new FormData()
  formData.append('expectedText', body.expectedText)
  formData.append('audioFile', body.audioFile)
  if (body.languageCode) formData.append('languageCode', body.languageCode)

  const { data } = await apiClient.post<PronunciationEvaluationResponse>('/api/v1/learning/pronunciation/evaluate', formData, {
    timeout: 30000
  })
  return data
}

// ── Admin ────────────────────────────────────────────────────────────────────

export const adjustXp = async (userId: string, body: AdjustXpRequest): Promise<void> => {
  await apiClient.put(`/api/v1/admin/learning/users/${userId}/xp`, body)
}

export const resetXp = async (userId: string): Promise<void> => {
  await apiClient.post(`/api/v1/admin/learning/users/${userId}/xp/reset`)
}

export const deleteItemMastery = async (userId: string, itemId: string): Promise<void> => {
  await apiClient.delete(`/api/v1/admin/learning/users/${userId}/masteries/${itemId}`)
}

export const resetAllMasteries = async (userId: string): Promise<void> => {
  await apiClient.post(`/api/v1/admin/learning/users/${userId}/masteries/reset`)
}

export const forceAbandonSession = async (sessionId: string): Promise<void> => {
  await apiClient.post(`/api/v1/admin/learning/sessions/${sessionId}/abandon`)
}
