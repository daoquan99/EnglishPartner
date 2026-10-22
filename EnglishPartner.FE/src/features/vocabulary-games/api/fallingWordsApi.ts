import { httpClient } from '@/shared/api/httpClient';
import type {
  ActiveFallingWordRoundDto,
  CreateFallingWordSessionRequest,
  FallingWordAudioDto,
  FallingWordGameSource,
  FallingWordPlayFilter,
  FallingWordPoolCountDto,
  FallingWordSessionDto,
  FallingWordSessionSummaryDto,
  PagedResult,
  SubmitFallingWordAnswerResponse,
  SynthesizeFallingWordAudioRequest,
  TerminalFallingWordRoundDto,
  WordFallingHistoryItemDto,
} from '../types/fallingWords.types';

const BASE_URL = '/api/vocabulary-games/falling-words';

export const fallingWordsApi = {
  async getPoolCount(params: {
    source?: FallingWordGameSource;
    cefrLevel?: string | null;
    playFilter?: FallingWordPlayFilter;
    timeZoneId?: string | null;
  }): Promise<FallingWordPoolCountDto> {
    const { data } = await httpClient.get<FallingWordPoolCountDto>(`${BASE_URL}/pool`, {
      params: {
        source: params.source,
        cefrLevel: params.cefrLevel || undefined,
        playFilter: params.playFilter,
        timeZoneId: params.timeZoneId || Intl.DateTimeFormat().resolvedOptions().timeZone,
      },
    });
    return data;
  },

  async createSession(
    request: CreateFallingWordSessionRequest,
    operationKey?: string
  ): Promise<FallingWordSessionDto> {
    const headers: Record<string, string> = {};
    if (operationKey) {
      headers['X-Operation-Key'] = operationKey;
    }
    const { data } = await httpClient.post<FallingWordSessionDto>(`${BASE_URL}/sessions`, request, {
      headers,
    });
    return data;
  },

  async getSession(sessionId: string): Promise<FallingWordSessionDto> {
    const { data } = await httpClient.get<FallingWordSessionDto>(`${BASE_URL}/sessions/${sessionId}`);
    return data;
  },

  async startNextRound(
    sessionId: string,
    operationKey?: string
  ): Promise<ActiveFallingWordRoundDto | null> {
    const headers: Record<string, string> = {};
    if (operationKey) {
      headers['X-Operation-Key'] = operationKey;
    }
    const { data } = await httpClient.post<ActiveFallingWordRoundDto | { isCompleted?: boolean }>(
      `${BASE_URL}/sessions/${sessionId}/rounds/next`,
      {},
      { headers }
    );
    if ('isCompleted' in data && data.isCompleted) {
      return null;
    }
    return data as ActiveFallingWordRoundDto;
  },

  async submitAnswer(
    sessionId: string,
    roundId: string,
    answerText: string,
    operationKey?: string
  ): Promise<SubmitFallingWordAnswerResponse> {
    const headers: Record<string, string> = {};
    if (operationKey) {
      headers['X-Operation-Key'] = operationKey;
    }
    const { data } = await httpClient.post<SubmitFallingWordAnswerResponse>(
      `${BASE_URL}/sessions/${sessionId}/rounds/${roundId}/answer`,
      { answerText },
      { headers }
    );
    return data;
  },

  async timeoutRound(
    sessionId: string,
    roundId: string,
    operationKey?: string
  ): Promise<TerminalFallingWordRoundDto> {
    const headers: Record<string, string> = {};
    if (operationKey) {
      headers['X-Operation-Key'] = operationKey;
    }
    const { data } = await httpClient.post<TerminalFallingWordRoundDto>(
      `${BASE_URL}/sessions/${sessionId}/rounds/${roundId}/timeout`,
      {},
      { headers }
    );
    return data;
  },

  async skipRound(
    sessionId: string,
    roundId: string,
    operationKey?: string
  ): Promise<TerminalFallingWordRoundDto> {
    const headers: Record<string, string> = {};
    if (operationKey) {
      headers['X-Operation-Key'] = operationKey;
    }
    const { data } = await httpClient.post<TerminalFallingWordRoundDto>(
      `${BASE_URL}/sessions/${sessionId}/rounds/${roundId}/skip`,
      {},
      { headers }
    );
    return data;
  },

  async synthesizeRoundAudio(
    sessionId: string,
    roundId: string,
    payload: SynthesizeFallingWordAudioRequest,
    signal?: AbortSignal
  ): Promise<FallingWordAudioDto> {
    const { data } = await httpClient.post<FallingWordAudioDto>(
      `${BASE_URL}/sessions/${sessionId}/rounds/${roundId}/synthesize`,
      payload,
      { signal, timeout: 60_000 }
    );
    return data;
  },

  async abandonSession(sessionId: string, operationKey?: string): Promise<FallingWordSessionDto> {
    const headers: Record<string, string> = {};
    if (operationKey) {
      headers['X-Operation-Key'] = operationKey;
    }
    const { data } = await httpClient.post<FallingWordSessionDto>(
      `${BASE_URL}/sessions/${sessionId}/abandon`,
      {},
      { headers }
    );
    return data;
  },

  async listSessions(params: {
    page?: number;
    pageSize?: number;
    source?: string;
    status?: string;
  }): Promise<PagedResult<FallingWordSessionSummaryDto>> {
    const { data } = await httpClient.get<PagedResult<FallingWordSessionSummaryDto>>(
      `${BASE_URL}/sessions`,
      { params }
    );
    return data;
  },

  async listSessionRounds(
    sessionId: string,
    params?: { page?: number; pageSize?: number }
  ): Promise<PagedResult<TerminalFallingWordRoundDto>> {
    const { data } = await httpClient.get<PagedResult<TerminalFallingWordRoundDto>>(
      `${BASE_URL}/sessions/${sessionId}/rounds`,
      { params }
    );
    return data;
  },

  async getWordHistory(params?: {
    senseId?: string;
    page?: number;
    pageSize?: number;
  }): Promise<PagedResult<WordFallingHistoryItemDto>> {
    const { data } = await httpClient.get<PagedResult<WordFallingHistoryItemDto>>(
      `${BASE_URL}/words/history`,
      { params }
    );
    return data;
  },
};
