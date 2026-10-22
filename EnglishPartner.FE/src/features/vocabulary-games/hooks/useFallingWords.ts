import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { fallingWordsApi } from '../api/fallingWordsApi';
import type {
  CreateFallingWordSessionRequest,
  FallingWordGameSource,
  FallingWordPlayFilter,
} from '../types/fallingWords.types';

export const FALLING_WORDS_QUERY_KEYS = {
  pool: (source: string, cefrLevel: string | null | undefined, playFilter: string) =>
    ['fallingWords', 'pool', source, cefrLevel || 'all', playFilter] as const,
  session: (sessionId: string) => ['fallingWords', 'session', sessionId] as const,
  sessionsList: (params: { page?: number; pageSize?: number; source?: string; status?: string }) =>
    ['fallingWords', 'sessionsList', params] as const,
  sessionRounds: (sessionId: string, page?: number) =>
    ['fallingWords', 'sessionRounds', sessionId, page ?? 1] as const,
  history: (senseId?: string, page?: number) =>
    ['fallingWords', 'history', senseId ?? 'all', page ?? 1] as const,
};

export function useFallingWordPool(params: {
  source?: FallingWordGameSource;
  cefrLevel?: string | null;
  playFilter?: FallingWordPlayFilter;
  timeZoneId?: string | null;
  enabled?: boolean;
}) {
  return useQuery({
    queryKey: FALLING_WORDS_QUERY_KEYS.pool(
      params.source ?? 'VocabularyCatalog',
      params.cefrLevel,
      params.playFilter ?? 'All'
    ),
    queryFn: () => fallingWordsApi.getPoolCount(params),
    enabled: params.enabled !== false,
    staleTime: 10_000,
  });
}

export function useFallingWordSession(sessionId: string | undefined, enabled = true) {
  return useQuery({
    queryKey: FALLING_WORDS_QUERY_KEYS.session(sessionId || ''),
    queryFn: () => fallingWordsApi.getSession(sessionId!),
    enabled: !!sessionId && enabled,
    refetchOnWindowFocus: false,
  });
}

export function useCreateFallingWordSession() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (variables: { request: CreateFallingWordSessionRequest; operationKey?: string }) =>
      fallingWordsApi.createSession(variables.request, variables.operationKey),
    onSuccess: (session) => {
      queryClient.setQueryData(FALLING_WORDS_QUERY_KEYS.session(session.id), session);
      queryClient.invalidateQueries({ queryKey: ['fallingWords', 'sessionsList'] });
    },
  });
}

export function useStartNextRound() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (variables: { sessionId: string; operationKey?: string }) =>
      fallingWordsApi.startNextRound(variables.sessionId, variables.operationKey),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: FALLING_WORDS_QUERY_KEYS.session(variables.sessionId),
      });
    },
  });
}

export function useSubmitFallingWordAnswer() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (variables: {
      sessionId: string;
      roundId: string;
      answerText: string;
      operationKey?: string;
    }) =>
      fallingWordsApi.submitAnswer(
        variables.sessionId,
        variables.roundId,
        variables.answerText,
        variables.operationKey
      ),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: FALLING_WORDS_QUERY_KEYS.session(variables.sessionId),
      });
      queryClient.invalidateQueries({ queryKey: ['fallingWords', 'history'] });
    },
  });
}

export function useTimeoutFallingWordRound() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (variables: { sessionId: string; roundId: string; operationKey?: string }) =>
      fallingWordsApi.timeoutRound(variables.sessionId, variables.roundId, variables.operationKey),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: FALLING_WORDS_QUERY_KEYS.session(variables.sessionId),
      });
      queryClient.invalidateQueries({ queryKey: ['fallingWords', 'history'] });
    },
  });
}

export function useSkipFallingWordRound() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (variables: { sessionId: string; roundId: string; operationKey?: string }) =>
      fallingWordsApi.skipRound(variables.sessionId, variables.roundId, variables.operationKey),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: FALLING_WORDS_QUERY_KEYS.session(variables.sessionId),
      });
      queryClient.invalidateQueries({ queryKey: ['fallingWords', 'history'] });
    },
  });
}

export function useSynthesizeFallingWordAudio() {
  return useMutation({
    mutationFn: (variables: {
      sessionId: string;
      roundId: string;
      payload: {
        apiKey?: string;
        voiceName?: string;
        speechSpeed?: number;
        accent?: string;
        modelCode?: string;
      };
      signal?: AbortSignal;
    }) =>
      fallingWordsApi.synthesizeRoundAudio(
        variables.sessionId,
        variables.roundId,
        variables.payload,
        variables.signal
      ),
  });
}

export function useAbandonFallingWordSession() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (variables: { sessionId: string; operationKey?: string }) =>
      fallingWordsApi.abandonSession(variables.sessionId, variables.operationKey),
    onSuccess: (session) => {
      queryClient.setQueryData(FALLING_WORDS_QUERY_KEYS.session(session.id), session);
      queryClient.invalidateQueries({ queryKey: ['fallingWords', 'sessionsList'] });
    },
  });
}

export function useFallingWordSessionsList(params: {
  page?: number;
  pageSize?: number;
  source?: string;
  status?: string;
}) {
  return useQuery({
    queryKey: FALLING_WORDS_QUERY_KEYS.sessionsList(params),
    queryFn: () => fallingWordsApi.listSessions(params),
  });
}

export function useFallingWordHistory(params: {
  senseId?: string;
  page?: number;
  pageSize?: number;
}) {
  return useQuery({
    queryKey: FALLING_WORDS_QUERY_KEYS.history(params.senseId, params.page),
    queryFn: () => fallingWordsApi.getWordHistory(params),
  });
}
