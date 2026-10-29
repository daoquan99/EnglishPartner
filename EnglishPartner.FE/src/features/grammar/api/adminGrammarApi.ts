import { httpClient } from '@/shared/api/httpClient';
import { GRAMMAR_AI_TIMEOUT_MS } from './grammarApi';
import type {
  GrammarStepAdminDto,
  GrammarStepAdminDetailDto,
  GrammarExerciseAdminDto,
  UpdateGrammarStepRequestDto,
  UpdateGrammarStepTheoryRequestDto,
  CreateOfficialGrammarExerciseRequestDto,
  UpdateOfficialGrammarExerciseRequestDto,
  ImportGrammarContentPackRequestDto,
  GrammarContentPackImportResultDto,
  GrammarModerationItemDto,
  RejectGrammarExerciseRequestDto,
  PromoteGrammarExerciseRequestDto,
  GenerateOfficialGrammarExercisesRequestDto,
  ImportOfficialGrammarExercisesJsonRequestDto,
  DraftGrammarTheoryWithAiRequestDto,
  DraftGrammarTheoryWithAiResponseDto,
  BuildGrammarExercisePromptRequestDto,
  BuildGrammarExercisePromptResponseDto,
  GenerateOfficialGrammarExercisesResponseDto,
  GenerateGrammarExercisesResponseDto,
  PagedResultDto,
  ListAdminGrammarStepsParams,
  ListAdminGrammarModerationParams,
} from '../types/grammar.types';

export const adminGrammarApi = {
  listSteps: async (
    params?: ListAdminGrammarStepsParams
  ): Promise<PagedResultDto<GrammarStepAdminDto>> => {
    const response = await httpClient.get<
      PagedResultDto<GrammarStepAdminDto> | GrammarStepAdminDto[]
    >('/api/admin/grammar/steps', { params });
    const { data, headers } = response;

    if (Array.isArray(data)) {
      const totalCount = parseInt(headers['x-total-count'] || String(data.length), 10);
      const page = parseInt(headers['x-page'] || '1', 10);
      const pageSize = parseInt(headers['x-page-size'] || String(data.length || 20), 10);
      const totalPages = parseInt(headers['x-total-pages'] || '1', 10);

      return {
        items: data,
        totalCount,
        page,
        pageSize,
        totalPages,
      };
    }

    return data;
  },

  getStep: async (code: string): Promise<GrammarStepAdminDetailDto> => {
    const { data } = await httpClient.get<GrammarStepAdminDetailDto>(
      `/api/admin/grammar/steps/${encodeURIComponent(code)}`
    );
    return data;
  },

  updateStep: async (
    code: string,
    payload: UpdateGrammarStepRequestDto
  ): Promise<GrammarStepAdminDto> => {
    const { data } = await httpClient.put<GrammarStepAdminDto>(
      `/api/admin/grammar/steps/${encodeURIComponent(code)}`,
      payload
    );
    return data;
  },

  updateTheory: async (
    code: string,
    payload: UpdateGrammarStepTheoryRequestDto
  ): Promise<void> => {
    await httpClient.put<void>(
      `/api/admin/grammar/steps/${encodeURIComponent(code)}/theory`,
      payload
    );
  },

  draftTheoryWithAi: async (
    code: string,
    payload: DraftGrammarTheoryWithAiRequestDto
  ): Promise<DraftGrammarTheoryWithAiResponseDto> => {
    const { data } = await httpClient.post<DraftGrammarTheoryWithAiResponseDto>(
      `/api/admin/grammar/steps/${encodeURIComponent(code)}/theory/ai-draft`,
      payload,
      { timeout: GRAMMAR_AI_TIMEOUT_MS }
    );
    return data;
  },

  createOfficialExercise: async (
    code: string,
    payload: CreateOfficialGrammarExerciseRequestDto
  ): Promise<GrammarExerciseAdminDto> => {
    const { data } = await httpClient.post<GrammarExerciseAdminDto>(
      `/api/admin/grammar/steps/${encodeURIComponent(code)}/exercises`,
      payload
    );
    return data;
  },

  updateOfficialExercise: async (
    id: string,
    payload: UpdateOfficialGrammarExerciseRequestDto
  ): Promise<GrammarExerciseAdminDto> => {
    const { data } = await httpClient.put<GrammarExerciseAdminDto>(
      `/api/admin/grammar/exercises/${encodeURIComponent(id)}`,
      payload
    );
    return data;
  },

  deleteOfficialExercise: async (id: string): Promise<void> => {
    await httpClient.delete<void>(`/api/admin/grammar/exercises/${encodeURIComponent(id)}`);
  },

  generateOfficialExercises: async (
    code: string,
    payload: GenerateOfficialGrammarExercisesRequestDto
  ): Promise<GenerateOfficialGrammarExercisesResponseDto> => {
    const { data } = await httpClient.post<GenerateOfficialGrammarExercisesResponseDto>(
      `/api/admin/grammar/steps/${encodeURIComponent(code)}/exercises/generate`,
      payload,
      { timeout: GRAMMAR_AI_TIMEOUT_MS }
    );
    return data;
  },

  buildOfficialPrompt: async (
    code: string,
    payload: BuildGrammarExercisePromptRequestDto
  ): Promise<BuildGrammarExercisePromptResponseDto> => {
    const { data } = await httpClient.post<BuildGrammarExercisePromptResponseDto>(
      `/api/admin/grammar/steps/${encodeURIComponent(code)}/exercises/prompt`,
      payload
    );
    return data;
  },

  importOfficialExercises: async (
    code: string,
    payload: ImportOfficialGrammarExercisesJsonRequestDto
  ): Promise<GenerateOfficialGrammarExercisesResponseDto> => {
    const { data } = await httpClient.post<GenerateOfficialGrammarExercisesResponseDto>(
      `/api/admin/grammar/steps/${encodeURIComponent(code)}/exercises/import`,
      payload
    );
    return data;
  },

  importContentPack: async (
    payload?: ImportGrammarContentPackRequestDto
  ): Promise<GrammarContentPackImportResultDto> => {
    const { data } = await httpClient.post<GrammarContentPackImportResultDto>(
      '/api/admin/grammar/content-pack/import',
      payload ?? {}
    );
    return data;
  },

  listModerationQueue: async (
    params?: ListAdminGrammarModerationParams
  ): Promise<PagedResultDto<GrammarModerationItemDto>> => {
    const response = await httpClient.get<
      PagedResultDto<GrammarModerationItemDto> | GrammarModerationItemDto[]
    >('/api/admin/grammar/moderation', { params });
    const { data, headers } = response;

    if (Array.isArray(data)) {
      const totalCount = parseInt(headers['x-total-count'] || String(data.length), 10);
      const page = parseInt(headers['x-page'] || '1', 10);
      const pageSize = parseInt(headers['x-page-size'] || String(data.length || 20), 10);
      const totalPages = parseInt(headers['x-total-pages'] || '1', 10);

      return {
        items: data,
        totalCount,
        page,
        pageSize,
        totalPages,
      };
    }

    return data;
  },

  approveExercise: async (id: string): Promise<void> => {
    await httpClient.post<void>(
      `/api/admin/grammar/exercises/${encodeURIComponent(id)}/approve`
    );
  },

  rejectExercise: async (
    id: string,
    payload: RejectGrammarExerciseRequestDto
  ): Promise<void> => {
    await httpClient.post<void>(
      `/api/admin/grammar/exercises/${encodeURIComponent(id)}/reject`,
      payload
    );
  },

  promoteExercise: async (
    id: string,
    payload: PromoteGrammarExerciseRequestDto
  ): Promise<GrammarExerciseAdminDto> => {
    const { data } = await httpClient.post<GrammarExerciseAdminDto>(
      `/api/admin/grammar/exercises/${encodeURIComponent(id)}/promote`,
      payload
    );
    return data;
  },
};
