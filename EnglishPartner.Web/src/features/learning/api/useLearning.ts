import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import * as api from './learning.api'
import type {
  SubmitReviewRequest,
  StartSessionRequest,
  SubmitAnswerRequest,
  CompleteSessionRequest,
  AdjustXpRequest,
  SubmitAudioAnswerRequest,
  EvaluateConversationLineRequest,
  PronunciationEvaluationRequest,
} from '../types/learning.types'

// ── Dashboard & Leaderboard ──────────────────────────────────────────────────

export const useDashboard = () => {
  return useQuery({
    queryKey: ['learning', 'dashboard'],
    queryFn: api.getDashboard,
    staleTime: 5 * 60 * 1000,
  })
}

export const useLeaderboard = (count = 50) => {
  return useQuery({
    queryKey: ['learning', 'leaderboard', count],
    queryFn: () => api.getLeaderboard(count),
    staleTime: 5 * 60 * 1000,
  })
}

// ── Reviews ──────────────────────────────────────────────────────────────────

export const useReviewsDue = () => {
  return useQuery({
    queryKey: ['learning', 'reviews', 'due'],
    queryFn: api.getReviewsDue,
    staleTime: 2 * 60 * 1000,
  })
}

export const useSubmitReview = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: SubmitReviewRequest) => api.submitReview(body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['learning', 'reviews'] })
      queryClient.invalidateQueries({ queryKey: ['learning', 'dashboard'] })
    },
    onError: () => toast.error('Failed to submit review'),
  })
}

// ── Study Sessions ───────────────────────────────────────────────────────────

export const useStartSession = () => {
  return useMutation({
    mutationFn: (body: StartSessionRequest) => api.startSession(body),
    onError: () => toast.error('Failed to start session'),
  })
}

export const useSubmitAnswer = () => {
  return useMutation({
    mutationFn: ({ sessionId, ...body }: { sessionId: string } & SubmitAnswerRequest) =>
      api.submitAnswer(sessionId, body),
    onError: () => toast.error('Failed to submit answer'),
  })
}

export const useSubmitAudioAnswer = () => {
  return useMutation({
    mutationFn: ({ sessionId, ...body }: { sessionId: string } & SubmitAudioAnswerRequest) =>
      api.submitAudioAnswer(sessionId, body),
    onError: () => toast.error('Failed to submit audio answer'),
  })
}

export const usePingSession = () => {
  return useMutation({
    mutationFn: (sessionId: string) => api.pingSession(sessionId),
  })
}

export const useCompleteSession = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ sessionId, ...body }: { sessionId: string } & CompleteSessionRequest) =>
      api.completeSession(sessionId, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['learning', 'dashboard'] })
      toast.success('Session completed!')
    },
    onError: () => toast.error('Failed to complete session'),
  })
}

// ── Conversations ────────────────────────────────────────────────────────────

export const useEvaluateConversationLine = () => {
  return useMutation({
    mutationFn: (body: EvaluateConversationLineRequest) => api.evaluateConversationLine(body),
    retry: 2,
    onError: () => toast.error('Failed to evaluate conversation audio'),
  })
}

// ── Pronunciation ────────────────────────────────────────────────────────────

export const useEvaluatePronunciation = () => {
  return useMutation({
    mutationFn: (body: PronunciationEvaluationRequest) => api.evaluatePronunciation(body),
    retry: 1,
    onError: () => toast.error('Failed to evaluate pronunciation. Please try again.'),
  })
}

// ── Admin ────────────────────────────────────────────────────────────────────

export const useAdjustXp = () => {
  return useMutation({
    mutationFn: ({ userId, ...body }: { userId: string } & AdjustXpRequest) =>
      api.adjustXp(userId, body),
    onSuccess: () => toast.success('XP adjusted successfully'),
    onError: () => toast.error('Failed to adjust XP'),
  })
}

export const useResetXp = () => {
  return useMutation({
    mutationFn: (userId: string) => api.resetXp(userId),
    onSuccess: () => toast.success('XP reset successfully'),
    onError: () => toast.error('Failed to reset XP'),
  })
}

export const useDeleteItemMastery = () => {
  return useMutation({
    mutationFn: ({ userId, itemId }: { userId: string; itemId: string }) =>
      api.deleteItemMastery(userId, itemId),
    onSuccess: () => toast.success('Item mastery deleted'),
    onError: () => toast.error('Failed to delete item mastery'),
  })
}

export const useResetAllMasteries = () => {
  return useMutation({
    mutationFn: (userId: string) => api.resetAllMasteries(userId),
    onSuccess: () => toast.success('All masteries reset'),
    onError: () => toast.error('Failed to reset masteries'),
  })
}

export const useForceAbandonSession = () => {
  return useMutation({
    mutationFn: (sessionId: string) => api.forceAbandonSession(sessionId),
    onSuccess: () => toast.success('Session abandoned'),
    onError: () => toast.error('Failed to abandon session'),
  })
}
