/**
 * OpenAPI-aligned TypeScript Contracts for EnglishLiveTutor Backend (.NET 10).
 * Rule: API types are centralized from OpenAPI schema to prevent FE/BE drift.
 */

export type CefrLevel = 'A1' | 'A2' | 'B1' | 'B2' | 'C1' | 'C2';

export type ConversationModeCode =
  | 'practice_interview'
  | 'real_interview'
  | 'hr_interview'
  | 'technical_interview'
  | 'system_design'
  | 'behavioral_star'
  | 'stress_interview'
  | 'rapid_fire'
  | 'deep_dive'
  | 'english_coach'
  | 'pronunciation_coach'
  | 'resume_deep_dive'
  | 'daily_conversation'
  | 'daily_standup'
  | 'technical_discussion'
  | 'problem_solving'
  | 'client_meeting'
  | 'debate_mode'
  | 'roleplay_mode'
  | string;

export type TeacherVoiceName =
  | 'Aoede'
  | 'Puck'
  | 'Kore'
  | 'Charon'
  | 'Fenrir'
  | 'Leda'
  | 'Zephyr'
  | 'Orus'
  | 'Callirrhoe'
  | 'Autonoe'
  | 'Enceladus'
  | 'Iapetus'
  | 'Umbriel'
  | 'Algieba'
  | 'Despina'
  | 'Erinome'
  | 'Algenib'
  | 'Rasalgethi'
  | 'Laomedeia'
  | 'Achernar'
  | 'Alnilam'
  | 'Schedar'
  | 'Gacrux'
  | 'Pulcherrima'
  | 'Achird'
  | 'Zubenelgenubi'
  | 'Vindemiatrix'
  | 'Sadachbia'
  | 'Sadaltager'
  | 'Sulafat'
  | string;

// --- Identity & RBAC ---
export interface UserProfileDto {
  id: string;
  username: string;
  email: string;
  fullName: string;
  avatarUrl?: string | null;
  preferredCefrLevel: CefrLevel;
  preferredVoiceName: TeacherVoiceName;
  timeZoneId: string;
  isActive: boolean;
  lastLoginAtUtc?: string | null;
  roles: string[];
  permissions: string[];
}

export interface AuthResponseDto {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  user: UserProfileDto;
}

export interface LoginRequestDto {
  emailOrUsername: string;
  password: string;
  deviceInfo?: string;
}

export interface RegisterUserRequestDto {
  username: string;
  email: string;
  password: string;
  fullName: string;
  preferredCefrLevel: CefrLevel;
  preferredVoiceName: TeacherVoiceName;
  timeZoneId?: string;
}

export interface RefreshTokenRequestDto {
  refreshToken?: string;
  deviceInfo?: string;
}

export interface RoleDto {
  id: string;
  code: string;
  name: string;
  description: string;
  isSystemRole: boolean;
  isActive: boolean;
  permissions: string[];
}

export interface PermissionDto {
  id: string;
  code: string;
  name: string;
  module: string;
  description: string;
}

// --- Database-Driven Prompts, Modes, Voices, Speech Speeds & Topics ---
export interface CefrLevelPromptDto {
  id: string;
  cefrLevel: CefrLevel;
  displayName: string;
  levelGuidance: string;
  displayOrder: number;
  isActive: boolean;
}

export interface ConversationModePromptDto {
  id: string;
  modeCode: ConversationModeCode;
  category: string;
  displayName: string;
  directiveTemplate: string;
  displayOrder: number;
  isDefault: boolean;
  isActive: boolean;
}

export interface TeacherVoiceDto {
  id: string;
  name: string;
  displayName: string;
  label: string;
  type: string;
  gender: 'Female' | 'Male' | string;
  persona: string;
  style: string;
  description: string;
  isFeatured: boolean;
  isEnabled: boolean;
  displayOrder: number;
}

export interface SpeechSpeedOptionDto {
  value: number;
  label: string;
  descriptionVi: string;
  recommendedCefrRange: string;
  isDefault: boolean;
}

export interface TopicDto {
  id: string;
  code: string;
  name: string;
  displayNameVi: string;
  category: string;
  modeCode?: string | null;
  suggestedCefrLevel: CefrLevel;
  supportsConversation: boolean;
  supportsVocabulary: boolean;
  description: string;
  isFeatured: boolean;
  isActive: boolean;
  displayOrder: number;
}

export interface UpsertTopicRequestDto {
  code: string;
  name: string;
  displayNameVi: string;
  category: string;
  modeCode?: string | null;
  suggestedCefrLevel: CefrLevel;
  supportsConversation: boolean;
  supportsVocabulary: boolean;
  description: string;
  isFeatured: boolean;
  isActive: boolean;
  displayOrder: number;
}

// --- AI Model Catalog ---
export interface AiModelOptionDto {
  id: string;
  displayName: string;
  modelCode: string;
  routedModelCode?: string | null;
  supportsTextGeneration: boolean;
  supportsStructuredOutput: boolean;
  supportsAudioInput: boolean;
  supportsPronunciationAssessment: boolean;
  supportsLiveAudio?: boolean;
  isAvailable: boolean;
  isDeprecated: boolean;
  isEnabled: boolean;
  capabilityNote: string;
  displayOrder: number;
}

// --- Vocabulary & Pronunciation Rubrics ---
export interface ScoringRubricCriterionDto {
  id: string;
  code: string;
  name: string;
  description: string;
  weight: number;
  displayOrder: number;
  minimumScore: number;
  maximumScore: number;
  isEnabled: boolean;
}

export interface ScoringRubricDto {
  id: string;
  code: string;
  name: string;
  description: string;
  version: number;
  assessmentType: string;
  isActive: boolean;
  criteria: ScoringRubricCriterionDto[];
}
