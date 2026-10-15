import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { adminApi, type UpsertVocabularyAdminPayload } from '../api/adminApi';
import { CATALOG_QUERY_KEYS } from '@/features/catalog';
import type {
  AdminCreateUserRequestDto,
  AdminUpdateUserRequestDto,
  TeacherVoiceDto,
  UpsertAiModelConfigRequestDto,
  UpsertSpeechSpeedRequestDto,
  UpsertTopicRequestDto,
} from '@/shared/api/openapi.types';

export const ADMIN_QUERY_KEYS = {
  vocabularies: ['admin', 'vocabularies'] as const,
  users: ['admin', 'users'] as const,
  roles: ['admin', 'roles'] as const,
  permissions: ['admin', 'permissions'] as const,
  allVoices: ['admin', 'voices', 'all'] as const,
  allTopics: ['admin', 'topics', 'all'] as const,
  allSpeechSpeeds: ['admin', 'speech-speeds', 'all'] as const,
  allRubrics: ['admin', 'rubrics', 'all'] as const,
  allAiModels: ['admin', 'ai-models', 'all'] as const,
};

// --- Vocabularies / Flashcards CRUD ---
export function useAdminVocabulariesQuery() {
  return useQuery({
    queryKey: ADMIN_QUERY_KEYS.vocabularies,
    queryFn: () => adminApi.listVocabularies(),
  });
}

export function useCreateVocabularyAdminMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpsertVocabularyAdminPayload) => adminApi.createVocabulary(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.vocabularies });
      void queryClient.invalidateQueries({ queryKey: ['vocabulary'] });
    },
  });
}

export function useUpdateVocabularyAdminMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpsertVocabularyAdminPayload }) =>
      adminApi.updateVocabulary(id, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.vocabularies });
      void queryClient.invalidateQueries({ queryKey: ['vocabulary'] });
    },
  });
}

export function useDeleteVocabularyAdminMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => adminApi.deleteVocabulary(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.vocabularies });
      void queryClient.invalidateQueries({ queryKey: ['vocabulary'] });
    },
  });
}

// --- Topics CRUD ---
export function useAdminTopicsQuery() {
  return useQuery({
    queryKey: ADMIN_QUERY_KEYS.allTopics,
    queryFn: () => adminApi.listAllTopics(),
  });
}

export function useCreateTopicMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpsertTopicRequestDto) => adminApi.createTopic(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.allTopics });
      void queryClient.invalidateQueries({ queryKey: ['catalog', 'topics'] });
    },
  });
}

export function useUpdateTopicMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpsertTopicRequestDto }) =>
      adminApi.updateTopic(id, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.allTopics });
      void queryClient.invalidateQueries({ queryKey: ['catalog', 'topics'] });
    },
  });
}

export function useDeleteTopicMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => adminApi.deleteTopic(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.allTopics });
      void queryClient.invalidateQueries({ queryKey: ['catalog', 'topics'] });
    },
  });
}

// --- Prompts (CEFR, Modes, Speech Speeds in DB) ---
export function useUpsertCefrPromptMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      cefrLevel,
      payload,
    }: {
      cefrLevel: string;
      payload: { displayName: string; levelGuidance: string; displayOrder: number; isActive: boolean };
    }) => adminApi.upsertCefrLevelPrompt(cefrLevel, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.cefrLevels });
    },
  });
}

export function useDeleteCefrPromptMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (cefrLevel: string) => adminApi.deleteCefrLevelPrompt(cefrLevel),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.cefrLevels });
    },
  });
}

export function useUpsertModePromptMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: adminApi.upsertConversationModePrompt,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['catalog', 'modes'] });
    },
  });
}

export function useDeleteModePromptMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (modeCode: string) => adminApi.deleteConversationModePrompt(modeCode),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['catalog', 'modes'] });
    },
  });
}

export function useAdminSpeechSpeedsQuery() {
  return useQuery({
    queryKey: ADMIN_QUERY_KEYS.allSpeechSpeeds,
    queryFn: () => adminApi.listAllSpeechSpeeds(),
  });
}

export function useCreateSpeechSpeedMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpsertSpeechSpeedRequestDto) => adminApi.createSpeechSpeed(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.allSpeechSpeeds });
      void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.speechSpeeds });
    },
  });
}

export function useUpdateSpeechSpeedMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpsertSpeechSpeedRequestDto }) =>
      adminApi.updateSpeechSpeed(id, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.allSpeechSpeeds });
      void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.speechSpeeds });
    },
  });
}

export function useDeleteSpeechSpeedMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => adminApi.deleteSpeechSpeed(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.allSpeechSpeeds });
      void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.speechSpeeds });
    },
  });
}

// --- Teacher Voices CRUD ---
export function useAdminVoicesQuery() {
  return useQuery({
    queryKey: ADMIN_QUERY_KEYS.allVoices,
    queryFn: () => adminApi.listAllTeacherVoices(),
  });
}

export function useUpsertTeacherVoiceMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: Omit<TeacherVoiceDto, 'id'> }) =>
      adminApi.upsertTeacherVoice(id, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.allVoices });
      void queryClient.invalidateQueries({ queryKey: ['catalog', 'voices'] });
    },
  });
}

export function useDeleteTeacherVoiceMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => adminApi.deleteTeacherVoice(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.allVoices });
      void queryClient.invalidateQueries({ queryKey: ['catalog', 'voices'] });
    },
  });
}

// --- Rubrics & AI Models CRUD ---
export function useAdminRubricsQuery() {
  return useQuery({
    queryKey: ADMIN_QUERY_KEYS.allRubrics,
    queryFn: () => adminApi.listScoringRubrics(),
  });
}

export function useUpsertRubricMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      code,
      payload,
    }: {
      code: string;
      payload: Parameters<typeof adminApi.upsertScoringRubric>[1];
    }) => adminApi.upsertScoringRubric(code, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.allRubrics });
      void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.activeRubric });
    },
  });
}

export function useDeleteRubricMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (code: string) => adminApi.deleteScoringRubric(code),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.allRubrics });
      void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.activeRubric });
    },
  });
}

export function useAdminAiModelsQuery() {
  return useQuery({
    queryKey: ADMIN_QUERY_KEYS.allAiModels,
    queryFn: () => adminApi.listAllAiModels(),
  });
}

export function useCreateAiModelMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpsertAiModelConfigRequestDto) => adminApi.createAiModel(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.allAiModels });
      void queryClient.invalidateQueries({ queryKey: ['catalog', 'models'] });
    },
  });
}

export function useUpdateAiModelMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpsertAiModelConfigRequestDto }) =>
      adminApi.updateAiModel(id, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.allAiModels });
      void queryClient.invalidateQueries({ queryKey: ['catalog', 'models'] });
    },
  });
}

export function useDeleteAiModelMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => adminApi.deleteAiModel(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.allAiModels });
      void queryClient.invalidateQueries({ queryKey: ['catalog', 'models'] });
    },
  });
}

// --- Users, Roles & Permissions CRUD ---
export function useAdminUsersQuery() {
  return useQuery({
    queryKey: ADMIN_QUERY_KEYS.users,
    queryFn: () => adminApi.listUsers(),
  });
}

export function useCreateUserAdminMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: AdminCreateUserRequestDto) => adminApi.createUser(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.users });
    },
  });
}

export function useUpdateUserAdminMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: AdminUpdateUserRequestDto }) =>
      adminApi.updateUser(id, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.users });
    },
  });
}

export function useAssignUserRolesMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ userId, roleCodes }: { userId: string; roleCodes: string[] }) =>
      adminApi.assignUserRoles(userId, roleCodes),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.users });
    },
  });
}

export function useDeleteUserAdminMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => adminApi.deleteUser(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.users });
    },
  });
}

export function useAdminRolesQuery() {
  return useQuery({
    queryKey: ADMIN_QUERY_KEYS.roles,
    queryFn: () => adminApi.listRoles(),
  });
}

export function useCreateRoleMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: adminApi.createRole,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.roles });
    },
  });
}

export function useUpdateRolePermissionsMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ roleId, permissionCodes }: { roleId: string; permissionCodes: string[] }) =>
      adminApi.updateRolePermissions(roleId, permissionCodes),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.roles });
    },
  });
}

export function useDeleteRoleMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => adminApi.deleteRole(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.roles });
    },
  });
}

export function useAdminPermissionsQuery() {
  return useQuery({
    queryKey: ADMIN_QUERY_KEYS.permissions,
    queryFn: () => adminApi.listPermissions(),
  });
}

export function useCreatePermissionMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: adminApi.createPermission,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.permissions });
    },
  });
}

export function useUpdatePermissionMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      payload,
    }: {
      id: string;
      payload: { name: string; module: string; description: string };
    }) => adminApi.updatePermission(id, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.permissions });
    },
  });
}

export function useDeletePermissionMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => adminApi.deletePermission(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.permissions });
    },
  });
}
