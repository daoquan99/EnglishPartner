import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  conversationsApi,
  type CreateOrUpdateConversationPayload,
} from '../api/conversationsApi';

export const CONVERSATION_QUERY_KEYS = {
  list: ['tutor', 'conversations'] as const,
  detail: (id: string | null) => ['tutor', 'conversations', id] as const,
};

export function useConversationsListQuery() {
  return useQuery({
    queryKey: CONVERSATION_QUERY_KEYS.list,
    queryFn: () => conversationsApi.listConversations(),
    staleTime: 30_000,
  });
}

export function useConversationDetailQuery(conversationId: string | null) {
  return useQuery({
    queryKey: CONVERSATION_QUERY_KEYS.detail(conversationId),
    queryFn: () => (conversationId ? conversationsApi.getConversationDetail(conversationId) : null),
    enabled: Boolean(conversationId),
  });
}

export function useCreateConversationMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateOrUpdateConversationPayload) =>
      conversationsApi.createConversation(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: CONVERSATION_QUERY_KEYS.list });
    },
  });
}

export function useUpdateConversationSettingsMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      payload,
    }: {
      id: string;
      payload: CreateOrUpdateConversationPayload;
    }) => conversationsApi.updateConversationSettings(id, payload),
    onSuccess: (_, variables) => {
      void queryClient.invalidateQueries({ queryKey: CONVERSATION_QUERY_KEYS.list });
      void queryClient.invalidateQueries({
        queryKey: CONVERSATION_QUERY_KEYS.detail(variables.id),
      });
    },
  });
}

export function useDeleteConversationMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => conversationsApi.deleteConversation(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: CONVERSATION_QUERY_KEYS.list });
    },
  });
}
