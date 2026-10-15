import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { adminApi } from '../api/adminApi';
import { CATALOG_QUERY_KEYS } from '@/features/catalog';
import type { TeacherVoiceDto, UpsertTopicRequestDto } from '@/shared/api/openapi.types';

export const ADMIN_QUERY_KEYS = {
  users: ['admin', 'users'] as const,
  roles: ['admin', 'roles'] as const,
  permissions: ['admin', 'permissions'] as const,
  allVoices: ['admin', 'voices', 'all'] as const,
  allTopics: ['admin', 'topics', 'all'] as const,
};

export function useAdminUsersQuery() {
  return useQuery({
    queryKey: ADMIN_QUERY_KEYS.users,
    queryFn: () => adminApi.listUsers(),
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

export function useAdminRolesQuery() {
  return useQuery({
    queryKey: ADMIN_QUERY_KEYS.roles,
    queryFn: () => adminApi.listRoles(),
  });
}

export function useAdminPermissionsQuery() {
  return useQuery({
    queryKey: ADMIN_QUERY_KEYS.permissions,
    queryFn: () => adminApi.listPermissions(),
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

export function useUpsertModePromptMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: adminApi.upsertConversationModePrompt,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['catalog', 'modes'] });
    },
  });
}

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
