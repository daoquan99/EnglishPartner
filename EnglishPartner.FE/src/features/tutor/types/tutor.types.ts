import type { CefrLevel, ConversationModeCode, TeacherVoiceName } from '@/shared/api/openapi.types';

export type SessionPhase =
  | 'idle'
  | 'connecting'
  | 'listening'
  | 'user_speaking'
  | 'ai_speaking'
  | 'reconnecting'
  | 'error';

export type HubConnectionState = 'disconnected' | 'connecting' | 'connected' | 'reconnecting' | 'error';

export interface ThreeHubStates {
  tutorHub: HubConnectionState;
  transcribeHub: HubConnectionState;
  translateHub: HubConnectionState;
}

export interface BilingualTurnItem {
  turnId: string;
  speaker: 'user' | 'ai';
  englishLine1: string;
  vietnameseLine2: string;
  isFinal: boolean;
  timestamp: number;
}

export interface GrammarErrorDetail {
  wrongSegment: string;
  correction: string;
  errorType: string;
  explanationVi: string;
}

export interface GrammarCorrectionItem {
  turnId: string;
  hasErrors: boolean;
  originalSentence: string;
  correctedSentence: string;
  naturalAlternative: string;
  overallTipVi: string;
  errors: GrammarErrorDetail[];
  timestamp: number;
}

export interface ConversationSummaryDto {
  id: string;
  title: string;
  cefrLevel: CefrLevel;
  voiceName: TeacherVoiceName;
  speechSpeed: number;
  conversationMode: ConversationModeCode;
  topic: string;
  customSystemPrompt?: string | null;
  extendedThinking: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  messageCount: number;
  correctionCount: number;
  lastMessagePreview: string;
}

export interface PersistedMessageDto {
  id: number;
  turnId: string;
  speaker: 'user' | 'ai';
  transcriptLine1: string;
  translationLine2: string;
  createdAtUtc: string;
}

export interface PersistedCorrectionDto {
  id: number;
  turnId: string;
  originalSentence: string;
  correctedSentence: string;
  naturalAlternative: string;
  overallTipVi: string;
  errorsJson: string;
  createdAtUtc: string;
}

export interface ConversationDetailDto {
  id: string;
  title: string;
  cefrLevel: CefrLevel;
  voiceName: TeacherVoiceName;
  speechSpeed: number;
  conversationMode: ConversationModeCode;
  topic: string;
  customSystemPrompt?: string | null;
  extendedThinking: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  messages: PersistedMessageDto[];
  corrections: PersistedCorrectionDto[];
}
