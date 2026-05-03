import { apiClient } from '@/lib/axios'
import type {
  TranslateRequest,
  TranslateResponse,
  TtsRequest,
  GeminiTtsRequest,
  TtsVoice,
  SttRequest,
  SttResponse,
} from '../types/ai.types'

// ── Translation ──────────────────────────────────────────────────────────────

export const translateText = async (body: TranslateRequest): Promise<TranslateResponse> => {
  const { data } = await apiClient.post<TranslateResponse>('/api/v1/ai/translate', body)
  return data
}

// ── Text-to-Speech ──────────────────────────────────────────────────────────

export const textToSpeech = async (body: TtsRequest): Promise<Blob> => {
  const { data } = await apiClient.post('/api/v1/ai/tts', body, { responseType: 'blob' })
  return data
}

export const geminiTextToSpeech = async (body: GeminiTtsRequest): Promise<Blob> => {
  const { data } = await apiClient.post('/api/v1/ai/gemini-tts', body, { responseType: 'blob' })
  return data
}

export const getTtsVoices = async (): Promise<TtsVoice[]> => {
  const { data } = await apiClient.get<TtsVoice[]>('/api/v1/ai/tts/voices')
  return data
}

export const getLiveVoices = async (): Promise<TtsVoice[]> => {
  const { data } = await apiClient.get<TtsVoice[]>('/api/v1/ai/live-voices')
  return data
}

// ── Speech-to-Text ──────────────────────────────────────────────────────────

export const speechToText = async (body: SttRequest): Promise<SttResponse> => {
  const formData = new FormData()
  formData.append('audioFile', body.audioFile, 'recording.webm')
  if (body.languages) formData.append('languages', body.languages)

  const { data } = await apiClient.post<SttResponse>('/api/v1/ai/stt', formData)
  return data
}
