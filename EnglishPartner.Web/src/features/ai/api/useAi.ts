import { useQuery, useMutation } from '@tanstack/react-query'
import * as api from './ai.api'
import type { TtsRequest, GeminiTtsRequest, SttRequest, TranslateRequest, TranslateResponse } from '../types/ai.types'

export const useTranslate = () => {
  return useMutation({
    mutationFn: (body: TranslateRequest) => api.translateText(body),
  })
}

export const useTts = () => {
  return useMutation({
    mutationFn: (body: TtsRequest) => api.textToSpeech(body),
  })
}

export const useGeminiTts = () => {
  return useMutation({
    mutationFn: (body: GeminiTtsRequest) => api.geminiTextToSpeech(body),
  })
}

export const useTtsVoices = () => {
  return useQuery({
    queryKey: ['ai', 'tts', 'voices'],
    queryFn: api.getTtsVoices,
    staleTime: 24 * 60 * 60 * 1000, // 24h — voices rarely change
  })
}

export const useLiveVoices = () => {
  return useQuery({
    queryKey: ['ai', 'live-voices'],
    queryFn: api.getLiveVoices,
    staleTime: 24 * 60 * 60 * 1000,
  })
}

export const useStt = () => {
  return useMutation({
    mutationFn: (body: SttRequest) => api.speechToText(body),
  })
}

