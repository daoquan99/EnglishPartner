import { httpClient } from '@/shared/api/httpClient';
import type {
  CefrLevelPromptDto,
  ConversationModePromptDto,
  PermissionDto,
  RoleDto,
  ScoringRubricDto,
  TeacherVoiceDto,
  TopicDto,
  UpsertTopicRequestDto,
  UserProfileDto,
} from '@/shared/api/openapi.types';
import {
  DEFAULT_CEFR_PROMPTS,
  DEFAULT_CONVERSATION_MODES,
  DEFAULT_SCORING_RUBRIC,
  DEFAULT_TEACHER_VOICES,
  DEFAULT_TOPICS,
} from '@/features/catalog';

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
    permissions: [
      'conversations.access',
      'vocabularies.read',
      'vocabularies.generate',
      'pronunciation.assess',
      'rubrics.manage',
      'prompts.manage',
      'voices.manage',
      'topics.manage',
      'users.read',
      'users.manage',
      'roles.manage',
    ],
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

const FALLBACK_PERMISSIONS: PermissionDto[] = [
  { id: '60000000-0000-0000-0000-000000000001', code: 'conversations.access', name: 'Access Live Tutor Conversations', module: 'Conversations', description: 'Create, view, and stream live AI tutor conversations' },
  { id: '60000000-0000-0000-0000-000000000002', code: 'vocabularies.read', name: 'View Vocabulary & Flashcards', module: 'Vocabulary', description: 'Access vocabulary catalog and daily flashcards' },
  { id: '60000000-0000-0000-0000-000000000003', code: 'vocabularies.generate', name: 'Generate Vocabulary with AI', module: 'Vocabulary', description: 'Generate new structured vocabulary items via Gemini/Gemma' },
  { id: '60000000-0000-0000-0000-000000000004', code: 'pronunciation.assess', name: 'Submit Pronunciation Assessments', module: 'Pronunciation', description: 'Record and score spoken English pronunciation' },
  { id: '60000000-0000-0000-0000-000000000005', code: 'rubrics.manage', name: 'Manage Pronunciation Scoring Rubrics', module: 'Pronunciation', description: 'Update scoring rubric criteria and weights' },
  { id: '60000000-0000-0000-0000-000000000006', code: 'prompts.manage', name: 'Manage CEFR & Session Mode Prompts', module: 'Prompts', description: 'Create and update CEFR level guidance and coaching mode directives' },
  { id: '60000000-0000-0000-0000-000000000007', code: 'voices.manage', name: 'Manage Teacher Voices Catalog', module: 'Voices', description: 'Update and configure prebuilt teacher voices' },
  { id: '60000000-0000-0000-0000-000000000008', code: 'users.read', name: 'View Users', module: 'Identity', description: 'List and inspect user profiles' },
  { id: '60000000-0000-0000-0000-000000000009', code: 'users.manage', name: 'Manage Users & Role Assignments', module: 'Identity', description: 'Assign roles and activate/deactivate users' },
  { id: '60000000-0000-0000-0000-000000000010', code: 'roles.manage', name: 'Manage Roles & Permissions', module: 'Identity', description: 'Create roles and configure role-permission mappings' },
  { id: '60000000-0000-0000-0000-000000000011', code: 'topics.manage', name: 'Manage Topics Catalog', module: 'Topics', description: 'Create, update, and curate conversation & vocabulary topics' },
];

const FALLBACK_ROLES: RoleDto[] = [
  {
    id: '70000000-0000-0000-0000-000000000001',
    code: 'ADMIN',
    name: 'System Administrator',
    description: 'Toàn quyền quản trị hệ thống, người dùng, prompt, giọng đọc, chủ đề và rubric.',
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
  // Users
  async listUsers(): Promise<UserProfileDto[]> {
    try {
      const { data } = await httpClient.get<UserProfileDto[]>('/api/users');
      return Array.isArray(data) && data.length > 0 ? data : FALLBACK_USERS;
    } catch {
      return FALLBACK_USERS;
    }
  },

  async assignUserRoles(userId: string, roleCodes: string[]): Promise<UserProfileDto> {
    const { data } = await httpClient.put<UserProfileDto>(`/api/users/${userId}/roles`, {
      roleCodes,
    });
    return data;
  },

  // Roles & Permissions
  async listRoles(): Promise<RoleDto[]> {
    try {
      const { data } = await httpClient.get<RoleDto[]>('/api/roles');
      return Array.isArray(data) && data.length > 0 ? data : FALLBACK_ROLES;
    } catch {
      return FALLBACK_ROLES;
    }
  },

  async listPermissions(): Promise<PermissionDto[]> {
    try {
      const { data } = await httpClient.get<PermissionDto[]>('/api/permissions');
      return Array.isArray(data) && data.length > 0 ? data : FALLBACK_PERMISSIONS;
    } catch {
      return FALLBACK_PERMISSIONS;
    }
  },

  async updateRolePermissions(roleId: string, permissionCodes: string[]): Promise<RoleDto> {
    const { data } = await httpClient.put<RoleDto>(`/api/roles/${roleId}/permissions`, {
      permissionCodes,
    });
    return data;
  },

  // Prompts (CEFR Levels & 19 Conversation Modes)
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

  // Teacher Voices
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

  // Topics Aggregate CRUD
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

  // Scoring Rubrics
  async updateScoringRubric(
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
    try {
      const { data } = await httpClient.put<ScoringRubricDto>(
        `/api/pronunciation/rubrics/${code}`,
        payload
      );
      return data;
    } catch {
      return DEFAULT_SCORING_RUBRIC;
    }
  },
};
