// ── Translation ────────────────────────────────────────────────────────────────

export interface TranslateRequest {
  text: string
  targetLanguageCode: string
}

export interface TranslateResponse {
  translation: string
}

// ── TTS ──────────────────────────────────────────────────────────────────────

export interface TtsRequest {
  text: string
  voice?: string
  speakingRate?: number
}

export interface GeminiTtsRequest {
  text: string
  voice?: string
  stylePrompt?: string
  languageCode?: string
}

export interface TtsVoice {
  id: string
  displayName: string
  accent: string
  gender: string
  languageCode: string
}

// ── STT (Speech-to-Text) ────────────────────────────────────────────────────

export interface SttRequest {
  audioFile: File | Blob
  languages?: string
}

export interface SttResponse {
  transcript: string
  confidence: number
  isFinal?: boolean
  detectedLanguage: string
  error?: string
  accumulated?: string
}

// ── Admin Audio Generate ─────────────────────────────────────────────────────

export interface GenerateLineAudioRequest {
  voice?: string
}

export interface GenerateLineAudioResponse {
  audioUrl: string
}

export interface GenerateAllAudioResponse {
  totalLines: number
  generatedCount: number
  skippedCount: number
  generatedLines: { lineId: string; audioUrl: string }[]
}

// ── Chat (AI Tutor) ─────────────────────────────────────────────────────────

export interface AiChatSession {
  id: string
  title: string
  systemPrompt?: string | null
  topic?: string | null
  sessionType?: string | null
  voiceName?: string | null
  createdAt: string
}

export interface ChatMessage {
  id: string
  role: 'user' | 'model'
  text: string
  timestamp: string
  order: number
  audioUrl?: string | null
  translation?: string | null
}

export interface CreateSessionRequest {
  title: string
  systemPrompt?: string | null
  topic?: string | null
}

export interface SendMessageRequest {
  text: string
}

export interface UpdateMessageAudioUrlRequest {
  audioUrl: string
}

export interface UpdateMessageTranslationRequest {
  translation: string
}

export interface SendMessageResponse {
  messageId: string
  reply: string
}
