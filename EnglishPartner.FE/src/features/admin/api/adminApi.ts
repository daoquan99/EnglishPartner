import { httpClient } from '@/shared/api/httpClient';
import type {
  AdminCreateUserRequestDto,
  AdminUpdateUserRequestDto,
  AiModelOptionDto,
  CefrLevelPromptDto,
  ConversationModePromptDto,
  PermissionDto,
  RoleDto,
  ScoringRubricDto,
  SpeechSpeedOptionDto,
  TeacherVoiceDto,
  TopicDto,
  UpsertAiModelConfigRequestDto,
  UpsertSpeechSpeedRequestDto,
  UpsertTopicRequestDto,
  UserProfileDto,
} from '@/shared/api/openapi.types';
import {
  DEFAULT_AI_MODELS,
  DEFAULT_CEFR_PROMPTS,
  DEFAULT_CONVERSATION_MODES,
  DEFAULT_SCORING_RUBRIC,
  DEFAULT_SPEECH_SPEEDS,
  DEFAULT_TEACHER_VOICES,
  DEFAULT_TOPICS,
} from '@/features/catalog';
import { FALLBACK_VOCABULARY_CARDS, type VocabularyFlashcardDto } from '@/features/vocabulary';

export interface UpsertVocabularyAdminPayload {
  word: string;
  partOfSpeech: string;
  cefrLevel: string;
  definitionEn: string;
  meaningVi: string;
  descriptionEn: string;
  topicCategory?: string;
  frequencyRank?: number;
  ukIpa: string;
  usIpa: string;
  stressPattern?: string;
  synonyms: string[];
  antonyms: string[];
  wordFamilies: Array<{ wordForm: string; partOfSpeech: string; note?: string }>;
  collocations: Array<{ phrase: string; meaningVi: string; exampleSentence?: string }>;
  examples: Array<{ sentenceEn: string; translationVi: string; cefrLevel: string }>;
  grammarPatterns: Array<{ pattern: string; explanationVi: string; sampleSentence?: string }>;
}

const FALLBACK_PERMISSIONS: PermissionDto[] = [
  { id: '60000000-0000-0000-0000-000000000001', code: 'conversations.access', name: 'Access Live Tutor Conversations', module: 'Conversations', description: 'Create, view, and stream live AI tutor conversations' },
  { id: '60000000-0000-0000-0000-000000000002', code: 'vocabularies.read', name: 'View Vocabulary & Flashcards', module: 'Vocabulary', description: 'Access vocabulary catalog and daily flashcards' },
  { id: '60000000-0000-0000-0000-000000000003', code: 'vocabularies.generate', name: 'Generate Vocabulary with AI', module: 'Vocabulary', description: 'Generate new structured vocabulary items via Gemini/Gemma' },
  { id: '60000000-0000-0000-0000-000000000004', code: 'pronunciation.assess', name: 'Submit Pronunciation Assessments', module: 'Pronunciation', description: 'Record and score spoken English pronunciation' },
  { id: '60000000-0000-0000-0000-000000000005', code: 'rubrics.manage', name: 'Manage Pronunciation Scoring Rubrics & AI Models', module: 'Pronunciation', description: 'Update scoring rubric criteria, weights, and AI model catalog' },
  { id: '60000000-0000-0000-0000-000000000006', code: 'prompts.manage', name: 'Manage CEFR, Session Modes & Speech Speeds', module: 'Prompts', description: 'Create and update CEFR level guidance, coaching modes, and speech speeds' },
  { id: '60000000-0000-0000-0000-000000000007', code: 'voices.manage', name: 'Manage Teacher Voices Catalog', module: 'Voices', description: 'Update and configure prebuilt teacher voices' },
  { id: '60000000-0000-0000-0000-000000000008', code: 'users.read', name: 'View Users', module: 'Identity', description: 'List and inspect user profiles' },
  { id: '60000000-0000-0000-0000-000000000009', code: 'users.manage', name: 'Manage Users & Role Assignments', module: 'Identity', description: 'Create, update, assign roles, and delete users' },
  { id: '60000000-0000-0000-0000-000000000010', code: 'roles.manage', name: 'Manage Roles & Permissions', module: 'Identity', description: 'Create roles and configure role-permission mappings' },
  { id: '60000000-0000-0000-0000-000000000011', code: 'topics.manage', name: 'Manage Topics Catalog', module: 'Topics', description: 'Create, update, and curate conversation & vocabulary topics' },
  { id: '60000000-0000-0000-0000-000000000012', code: 'vocabularies.manage', name: 'Manage Vocabulary & Flashcards Catalog', module: 'Vocabulary', description: 'Create, update, and delete vocabulary flashcards in database' },
];

const FALLBACK_USERS: UserProfileDto[] = [
  {
    id: '80000000-0000-0000-0000-000000000001',
    username: 'admin',
    email: 'admin@lingomint.local',
    fullName: 'LingoMint Administrator',
    preferredCefrLevel: 'C1',
    preferredVoiceName: 'Aoede',
    timeZoneId: 'Asia/Ho_Chi_Minh',
    isActive: true,
    roles: ['ADMIN', 'TEACHER'],
    permissions: FALLBACK_PERMISSIONS.map((p) => p.code),
  },
  {
    id: '80000000-0000-0000-0000-000000000002',
    username: 'default-learner',
    email: 'learner@lingomint.local',
    fullName: 'Học viên Mặc định',
    preferredCefrLevel: 'B1',
    preferredVoiceName: 'Aoede',
    timeZoneId: 'Asia/Ho_Chi_Minh',
    isActive: true,
    roles: ['LEARNER'],
    permissions: [
      'conversations.access',
      'vocabularies.read',
      'vocabularies.generate',
      'pronunciation.assess',
    ],
  },
];

const FALLBACK_ROLES: RoleDto[] = [
  {
    id: '70000000-0000-0000-0000-000000000001',
    code: 'ADMIN',
    name: 'System Administrator',
    description: 'Toàn quyền quản trị hệ thống, người dùng, từ vựng, prompt, giọng đọc, chủ đề và rubric.',
    isSystemRole: true,
    isActive: true,
    permissions: FALLBACK_PERMISSIONS.map((p) => p.code),
  },
  {
    id: '70000000-0000-0000-0000-000000000002',
    code: 'TEACHER',
    name: 'Curriculum & Coaching Teacher',
    description: 'Quản lý từ vựng, chủ đề, tiêu chí chấm phát âm, cấu hình CEFR prompt và giọng đọc giáo viên.',
    isSystemRole: true,
    isActive: true,
    permissions: [
      'conversations.access',
      'vocabularies.read',
      'vocabularies.generate',
      'vocabularies.manage',
      'pronunciation.assess',
      'rubrics.manage',
      'prompts.manage',
      'voices.manage',
      'topics.manage',
    ],
  },
  {
    id: '70000000-0000-0000-0000-000000000003',
    code: 'LEARNER',
    name: 'English Learner',
    description: 'Học viên luyện hội thoại trực tiếp, học flashcard từ vựng hằng ngày và chấm phát âm.',
    isSystemRole: true,
    isActive: true,
    permissions: [
      'conversations.access',
      'vocabularies.read',
      'vocabularies.generate',
      'pronunciation.assess',
    ],
  },
];

export const adminApi = {
  // ---------------------------------------------------------------------------
  // 1. Vocabularies / Flashcards CRUD (/api/vocabularies)
  // ---------------------------------------------------------------------------
  async listVocabularies(params?: {
    search?: string;
    cefrLevel?: string;
    topicCategory?: string;
  }): Promise<VocabularyFlashcardDto[]> {
    try {
      const { data } = await httpClient.get<VocabularyFlashcardDto[]>('/api/vocabularies', {
        params,
      });
      return Array.isArray(data) && data.length > 0 ? data : FALLBACK_VOCABULARY_CARDS;
    } catch {
      return FALLBACK_VOCABULARY_CARDS;
    }
  },

  async createVocabulary(payload: UpsertVocabularyAdminPayload): Promise<VocabularyFlashcardDto> {
    const { data } = await httpClient.post<VocabularyFlashcardDto>('/api/vocabularies', payload);
    return data;
  },

  async updateVocabulary(
    id: string,
    payload: UpsertVocabularyAdminPayload
  ): Promise<VocabularyFlashcardDto> {
    const { data } = await httpClient.put<VocabularyFlashcardDto>(
      `/api/vocabularies/${id}`,
      payload
    );
    return data;
  },

  async deleteVocabulary(id: string): Promise<void> {
    await httpClient.delete(`/api/vocabularies/${id}`);
  },

  // ---------------------------------------------------------------------------
  // 2. Topics Aggregate CRUD (/api/topics)
  // ---------------------------------------------------------------------------
  async listAllTopics(): Promise<TopicDto[]> {
    try {
      const { data } = await httpClient.get<TopicDto[]>('/api/topics', {
        params: { onlyActive: false },
      });
      return Array.isArray(data) && data.length > 0 ? data : DEFAULT_TOPICS;
    } catch {
      return DEFAULT_TOPICS;
    }
  },

  async createTopic(payload: UpsertTopicRequestDto): Promise<TopicDto> {
    const { data } = await httpClient.post<TopicDto>('/api/topics', payload);
    return data;
  },

  async updateTopic(id: string, payload: UpsertTopicRequestDto): Promise<TopicDto> {
    const { data } = await httpClient.put<TopicDto>(`/api/topics/${id}`, payload);
    return data;
  },

  async deleteTopic(id: string): Promise<void> {
    await httpClient.delete(`/api/topics/${id}`);
  },

  // ---------------------------------------------------------------------------
  // 3. Prompts: CEFR Levels, 19 Modes & Teacher Speech Speeds (DB)
  // ---------------------------------------------------------------------------
  async upsertCefrLevelPrompt(
    cefrLevel: string,
    payload: { displayName: string; levelGuidance: string; displayOrder: number; isActive: boolean }
  ): Promise<CefrLevelPromptDto> {
    const { data } = await httpClient.put<CefrLevelPromptDto>(
      `/api/prompts/cefr-levels/${cefrLevel}`,
      payload
    );
    return data;
  },

  async deleteCefrLevelPrompt(cefrLevel: string): Promise<void> {
    await httpClient.delete(`/api/prompts/cefr-levels/${cefrLevel}`);
  },

  async upsertConversationModePrompt(payload: {
    modeCode: string;
    category: string;
    displayName: string;
    directiveTemplate: string;
    displayOrder: number;
    isDefault: boolean;
    isActive: boolean;
  }): Promise<ConversationModePromptDto> {
    const { data } = await httpClient.put<ConversationModePromptDto>('/api/prompts/modes', payload);
    return data;
  },

  async deleteConversationModePrompt(modeCode: string): Promise<void> {
    await httpClient.delete(`/api/prompts/modes/${modeCode}`);
  },

  async listAllSpeechSpeeds(): Promise<SpeechSpeedOptionDto[]> {
    try {
      const { data } = await httpClient.get<SpeechSpeedOptionDto[]>('/api/prompts/speech-speeds', {
        params: { onlyActive: false },
      });
      return Array.isArray(data) && data.length > 0 ? data : DEFAULT_SPEECH_SPEEDS;
    } catch {
      return DEFAULT_SPEECH_SPEEDS;
    }
  },

  async createSpeechSpeed(payload: UpsertSpeechSpeedRequestDto): Promise<SpeechSpeedOptionDto> {
    const { data } = await httpClient.post<SpeechSpeedOptionDto>(
      '/api/prompts/speech-speeds',
      payload
    );
    return data;
  },

  async updateSpeechSpeed(
    id: string,
    payload: UpsertSpeechSpeedRequestDto
  ): Promise<SpeechSpeedOptionDto> {
    const { data } = await httpClient.put<SpeechSpeedOptionDto>(
      `/api/prompts/speech-speeds/${id}`,
      payload
    );
    return data;
  },

  async deleteSpeechSpeed(id: string): Promise<void> {
    await httpClient.delete(`/api/prompts/speech-speeds/${id}`);
  },

  async previewPrompt(payload: {
    cefrLevel: string;
    conversationMode: string;
    voiceName: string;
    speechSpeed: number;
    topic: string;
    customSystemPrompt?: string;
    extendedThinking: boolean;
  }): Promise<string> {
    try {
      const { data } = await httpClient.post<{ systemInstruction: string }>(
        '/api/prompts/preview',
        payload
      );
      return data.systemInstruction;
    } catch {
      const cefr = DEFAULT_CEFR_PROMPTS.find((c) => c.cefrLevel === payload.cefrLevel);
      const mode = DEFAULT_CONVERSATION_MODES.find((m) => m.modeCode === payload.conversationMode);
      return [
        `=== SYSTEM INSTRUCTION PREVIEW (${payload.cefrLevel} · ${payload.conversationMode}) ===`,
        `Voice: ${payload.voiceName} | Speed: ${payload.speechSpeed}x | Thinking: ${payload.extendedThinking}`,
        `Level Guidance: ${cefr?.levelGuidance ?? ''}`,
        `Mode Directive: ${mode?.directiveTemplate ?? ''}`,
        payload.topic ? `Topic Focus: ${payload.topic}` : '',
        payload.customSystemPrompt ? `Custom Override: ${payload.customSystemPrompt}` : '',
      ]
        .filter(Boolean)
        .join('\n\n');
    }
  },

  // ---------------------------------------------------------------------------
  // 4. Teacher Voices Catalog CRUD (/api/voices)
  // ---------------------------------------------------------------------------
  async listAllTeacherVoices(): Promise<TeacherVoiceDto[]> {
    try {
      const { data } = await httpClient.get<TeacherVoiceDto[]>('/api/voices', {
        params: { onlyEnabled: false, onlyFeatured: false },
      });
      return Array.isArray(data) && data.length > 0 ? data : DEFAULT_TEACHER_VOICES;
    } catch {
      return DEFAULT_TEACHER_VOICES;
    }
  },

  async upsertTeacherVoice(
    id: string,
    payload: Omit<TeacherVoiceDto, 'id'>
  ): Promise<TeacherVoiceDto> {
    const { data } = await httpClient.put<TeacherVoiceDto>(`/api/voices/${id}`, payload);
    return data;
  },

  async deleteTeacherVoice(id: string): Promise<void> {
    await httpClient.delete(`/api/voices/${id}`);
  },

  // ---------------------------------------------------------------------------
  // 5. Scoring Rubrics & AI Models Catalog CRUD (/api/pronunciation/rubrics & /api/ai-models)
  // ---------------------------------------------------------------------------
  async listScoringRubrics(): Promise<ScoringRubricDto[]> {
    try {
      const { data } = await httpClient.get<ScoringRubricDto[]>('/api/pronunciation/rubrics');
      return Array.isArray(data) && data.length > 0 ? data : [DEFAULT_SCORING_RUBRIC];
    } catch {
      return [DEFAULT_SCORING_RUBRIC];
    }
  },

  async upsertScoringRubric(
    code: string,
    payload: {
      name: string;
      description: string;
      isActive: boolean;
      criteria: Array<{
        code: string;
        name: string;
        description: string;
        weight: number;
        displayOrder: number;
        isEnabled: boolean;
      }>;
    }
  ): Promise<ScoringRubricDto> {
    const { data } = await httpClient.put<ScoringRubricDto>(
      `/api/pronunciation/rubrics/${code}`,
      payload
    );
    return data;
  },

  async deleteScoringRubric(code: string): Promise<void> {
    await httpClient.delete(`/api/pronunciation/rubrics/${code}`);
  },

  async listAllAiModels(): Promise<AiModelOptionDto[]> {
    try {
      const { data } = await httpClient.get<AiModelOptionDto[]>('/api/ai-models');
      return Array.isArray(data) && data.length > 0 ? data : DEFAULT_AI_MODELS;
    } catch {
      return DEFAULT_AI_MODELS;
    }
  },

  async createAiModel(payload: UpsertAiModelConfigRequestDto): Promise<AiModelOptionDto> {
    const { data } = await httpClient.post<AiModelOptionDto>('/api/ai-models', payload);
    return data;
  },

  async updateAiModel(
    id: string,
    payload: UpsertAiModelConfigRequestDto
  ): Promise<AiModelOptionDto> {
    const { data } = await httpClient.put<AiModelOptionDto>(`/api/ai-models/${id}`, payload);
    return data;
  },

  async deleteAiModel(id: string): Promise<void> {
    await httpClient.delete(`/api/ai-models/${id}`);
  },

  // ---------------------------------------------------------------------------
  // 6. Users, Roles & Permissions CRUD (/api/users, /api/roles, /api/permissions)
  // ---------------------------------------------------------------------------
  async listUsers(): Promise<UserProfileDto[]> {
    try {
      const { data } = await httpClient.get<UserProfileDto[]>('/api/users');
      return Array.isArray(data) && data.length > 0 ? data : FALLBACK_USERS;
    } catch {
      return FALLBACK_USERS;
    }
  },

  async createUser(payload: AdminCreateUserRequestDto): Promise<UserProfileDto> {
    const { data } = await httpClient.post<UserProfileDto>('/api/users', payload);
    return data;
  },

  async updateUser(id: string, payload: AdminUpdateUserRequestDto): Promise<UserProfileDto> {
    const { data } = await httpClient.put<UserProfileDto>(`/api/users/${id}`, payload);
    return data;
  },

  async assignUserRoles(userId: string, roleCodes: string[]): Promise<UserProfileDto> {
    const { data } = await httpClient.put<UserProfileDto>(`/api/users/${userId}/roles`, {
      roleCodes,
    });
    return data;
  },

  async deleteUser(id: string): Promise<void> {
    await httpClient.delete(`/api/users/${id}`);
  },

  async listRoles(): Promise<RoleDto[]> {
    try {
      const { data } = await httpClient.get<RoleDto[]>('/api/roles');
      return Array.isArray(data) && data.length > 0 ? data : FALLBACK_ROLES;
    } catch {
      return FALLBACK_ROLES;
    }
  },

  async createRole(payload: {
    code: string;
    name: string;
    description: string;
    permissionCodes: string[];
  }): Promise<RoleDto> {
    const { data } = await httpClient.post<RoleDto>('/api/roles', payload);
    return data;
  },

  async updateRolePermissions(roleId: string, permissionCodes: string[]): Promise<RoleDto> {
    const { data } = await httpClient.put<RoleDto>(`/api/roles/${roleId}/permissions`, {
      permissionCodes,
    });
    return data;
  },

  async deleteRole(id: string): Promise<void> {
    await httpClient.delete(`/api/roles/${id}`);
  },

  async listPermissions(): Promise<PermissionDto[]> {
    try {
      const { data } = await httpClient.get<PermissionDto[]>('/api/permissions');
      return Array.isArray(data) && data.length > 0 ? data : FALLBACK_PERMISSIONS;
    } catch {
      return FALLBACK_PERMISSIONS;
    }
  },

  async createPermission(payload: {
    code: string;
    name: string;
    module: string;
    description: string;
  }): Promise<PermissionDto> {
    const { data } = await httpClient.post<PermissionDto>('/api/permissions', payload);
    return data;
  },

  async updatePermission(
    id: string,
    payload: { name: string; module: string; description: string }
  ): Promise<PermissionDto> {
    const { data } = await httpClient.put<PermissionDto>(`/api/permissions/${id}`, payload);
    return data;
  },

  async deletePermission(id: string): Promise<void> {
    await httpClient.delete(`/api/permissions/${id}`);
  },
};
