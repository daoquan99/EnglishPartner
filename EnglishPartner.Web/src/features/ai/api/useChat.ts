import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import * as api from './chat.api'
import type { CreateSessionRequest, SendMessageRequest } from '../types/ai.types'
import { toast } from 'sonner'

export const useChatSessions = () => {
  return useQuery({
    queryKey: ['ai', 'chat', 'sessions'],
    queryFn: api.getSessions,
  })
}

export const useChatMessages = (sessionId: string | null) => {
  return useQuery({
    queryKey: ['ai', 'chat', 'sessions', sessionId, 'messages'],
    queryFn: () => api.getSessionMessages(sessionId!),
    enabled: !!sessionId,
  })
}

export const useCreateChatSession = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateSessionRequest) => api.createSession(body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['ai', 'chat', 'sessions'] })
    },
    onError: () => toast.error('Failed to create chat session')
  })
}

export const useSendChatMessage = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ sessionId, ...body }: { sessionId: string } & SendMessageRequest) =>
      api.sendMessage(sessionId, body),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['ai', 'chat', 'sessions', variables.sessionId, 'messages'] })
    },
    onError: () => toast.error('Failed to send message')
  })
}

export const useUpdateMessageTranslation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ sessionId, messageId, translation }: { sessionId: string; messageId: string; translation: string }) =>
      api.updateMessageTranslation(sessionId, messageId, { translation }),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['ai', 'chat', 'sessions', variables.sessionId, 'messages'] })
    },
    onError: () => toast.error('Failed to update translation')
  })
}

export const useUpdateMessageAudioUrl = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ sessionId, messageId, audioUrl }: { sessionId: string; messageId: string; audioUrl: string }) =>
      api.updateMessageAudioUrl(sessionId, messageId, { audioUrl }),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['ai', 'chat', 'sessions', variables.sessionId, 'messages'] })
    },
    onError: () => toast.error('Failed to update audio URL')
  })
}

export const useUploadMessageAudio = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ sessionId, messageId, audioFile }: { sessionId: string; messageId: string; audioFile: Blob }) =>
      api.uploadMessageAudio(sessionId, messageId, audioFile),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['ai', 'chat', 'sessions', variables.sessionId, 'messages'] })
    },
    onError: () => toast.error('Failed to upload audio file')
  })
}
