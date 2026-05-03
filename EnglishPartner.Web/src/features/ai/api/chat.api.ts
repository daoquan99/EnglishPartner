import { apiClient } from '@/lib/axios'
import type {
  AiChatSession,
  ChatMessage,
  CreateSessionRequest,
  SendMessageRequest,
  SendMessageResponse,
  UpdateMessageTranslationRequest,
  UpdateMessageAudioUrlRequest
} from '../types/ai.types'

export const getSessions = async (): Promise<AiChatSession[]> => {
  const { data } = await apiClient.get<AiChatSession[]>('/api/v1/ai/chat/sessions')
  return data
}

export const createSession = async (body: CreateSessionRequest): Promise<{ sessionId: string }> => {
  const { data } = await apiClient.post<{ sessionId: string }>('/api/v1/ai/chat/sessions', body)
  return data
}

export const getSessionMessages = async (sessionId: string): Promise<{ sessionId: string, title: string, systemPrompt?: string, topic?: string, messages: ChatMessage[] }> => {
  const { data } = await apiClient.get<{ sessionId: string, title: string, systemPrompt?: string, topic?: string, messages: ChatMessage[] }>(`/api/v1/ai/chat/sessions/${sessionId}/messages`)
  return data
}

export const sendMessage = async (sessionId: string, body: SendMessageRequest): Promise<SendMessageResponse> => {
  const { data } = await apiClient.post<SendMessageResponse>(`/api/v1/ai/chat/sessions/${sessionId}/messages`, body)
  return data
}

export const updateMessageTranslation = async (sessionId: string, messageId: string, body: UpdateMessageTranslationRequest): Promise<void> => {
  await apiClient.patch(`/api/v1/ai/chat/sessions/${sessionId}/messages/${messageId}/translation`, body)
}

export const updateMessageAudioUrl = async (sessionId: string, messageId: string, body: UpdateMessageAudioUrlRequest): Promise<void> => {
  await apiClient.patch(`/api/v1/ai/chat/sessions/${sessionId}/messages/${messageId}/audio-url`, body)
}

export const uploadMessageAudio = async (sessionId: string, messageId: string, audioFile: Blob): Promise<{ audioUrl: string }> => {
  const formData = new FormData()
  formData.append('audioFile', audioFile, 'tts-audio.mp3')
  const { data } = await apiClient.post<{ audioUrl: string }>(`/api/v1/ai/chat/sessions/${sessionId}/messages/${messageId}/audio`, formData)
  return data
}
