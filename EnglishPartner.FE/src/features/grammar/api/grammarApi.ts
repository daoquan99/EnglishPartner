import { httpClient } from '@/shared/api/httpClient';
import type {
  GrammarRoadmapDto,
  GrammarStepDetailDto,
  GrammarExerciseLearnerDto,
  StartGrammarAttemptRequestDto,
  SubmitGrammarAnswerRequestDto,
  GrammarAttemptDto,
  GrammarAnswerResultDto,
  GrammarAttemptResultDto,
  GenerateGrammarExercisesRequestDto,
  BuildGrammarExercisePromptRequestDto,
  BuildGrammarExercisePromptResponseDto,
  ImportGrammarExercisesJsonRequestDto,
  GrammarMyExerciseDto,
  GenerateGrammarExercisesResponseDto,
  UpdateMyGrammarExerciseRequestDto,
  ChangeMyGrammarExerciseVisibilityRequestDto,
  PagedResultDto,
  ListGrammarExercisesParams,
  ListMyGrammarExercisesParams,
} from '../types/grammar.types';

// AI generation/grading can run up to the backend provider timeout (120s); wait a little longer
// so the backend's own timeout error reaches the user instead of a client-side abort.
export const GRAMMAR_AI_TIMEOUT_MS = 150_000;

export const grammarApi = {
  getRoadmap: async (): Promise<GrammarRoadmapDto> => {
    const { data } = await httpClient.get<GrammarRoadmapDto>('/api/grammar/roadmap');
    return data;
  },

  getStep: async (code: string): Promise<GrammarStepDetailDto> => {
    const { data } = await httpClient.get<GrammarStepDetailDto>(
      `/api/grammar/steps/${encodeURIComponent(code)}`
    );
    return data;
  },

  listStepExercises: async (
    code: string,
    params?: ListGrammarExercisesParams
  ): Promise<PagedResultDto<GrammarExerciseLearnerDto>> => {
    const response = await httpClient.get<PagedResultDto<GrammarExerciseLearnerDto> | GrammarExerciseLearnerDto[]>(
      `/api/grammar/steps/${encodeURIComponent(code)}/exercises`,
      { params }
    );
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

  startAttempt: async (payload: StartGrammarAttemptRequestDto): Promise<GrammarAttemptDto> => {
    const { data } = await httpClient.post<GrammarAttemptDto>('/api/grammar/attempts', payload);
    return data;
  },

  getAttempt: async (id: string): Promise<GrammarAttemptDto> => {
    const { data } = await httpClient.get<GrammarAttemptDto>(
      `/api/grammar/attempts/${encodeURIComponent(id)}`
    );
    return data;
  },

  submitAnswer: async (
    attemptId: string,
    payload: SubmitGrammarAnswerRequestDto
  ): Promise<GrammarAnswerResultDto> => {
    const { data } = await httpClient.post<GrammarAnswerResultDto>(
      `/api/grammar/attempts/${encodeURIComponent(attemptId)}/answers`,
      payload,
      { timeout: GRAMMAR_AI_TIMEOUT_MS }
    );
    return data;
  },

  completeAttempt: async (attemptId: string): Promise<GrammarAttemptResultDto> => {
    const { data } = await httpClient.post<GrammarAttemptResultDto>(
      `/api/grammar/attempts/${encodeURIComponent(attemptId)}/complete`
    );
    return data;
  },

  generateExercises: async (
    stepCode: string,
    payload: GenerateGrammarExercisesRequestDto
  ): Promise<GenerateGrammarExercisesResponseDto> => {
    const { data } = await httpClient.post<GenerateGrammarExercisesResponseDto>(
      `/api/grammar/steps/${encodeURIComponent(stepCode)}/exercises/generate`,
      payload,
      { timeout: GRAMMAR_AI_TIMEOUT_MS }
    );
    return data;
  },

  buildPrompt: async (
    stepCode: string,
    payload: BuildGrammarExercisePromptRequestDto
  ): Promise<BuildGrammarExercisePromptResponseDto> => {
    const { data } = await httpClient.post<BuildGrammarExercisePromptResponseDto>(
      `/api/grammar/steps/${encodeURIComponent(stepCode)}/exercises/prompt`,
      payload
    );
    return data;
  },

  importExercises: async (
    stepCode: string,
    payload: ImportGrammarExercisesJsonRequestDto
  ): Promise<GenerateGrammarExercisesResponseDto> => {
    const { data } = await httpClient.post<GenerateGrammarExercisesResponseDto>(
      `/api/grammar/steps/${encodeURIComponent(stepCode)}/exercises/import`,
      payload
    );
    return data;
  },

  listMyExercises: async (
    params?: ListMyGrammarExercisesParams
  ): Promise<PagedResultDto<GrammarMyExerciseDto>> => {
    const response = await httpClient.get<PagedResultDto<GrammarMyExerciseDto> | GrammarMyExerciseDto[]>(
      '/api/grammar/my-exercises',
      { params }
    );
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

  updateMyExercise: async (
    id: string,
    payload: UpdateMyGrammarExerciseRequestDto
  ): Promise<GrammarMyExerciseDto> => {
    const { data } = await httpClient.put<GrammarMyExerciseDto>(
      `/api/grammar/my-exercises/${encodeURIComponent(id)}`,
      payload
    );
    return data;
  },

  changeMyExerciseVisibility: async (
    id: string,
    payload: ChangeMyGrammarExerciseVisibilityRequestDto
  ): Promise<GrammarMyExerciseDto> => {
    const { data } = await httpClient.post<GrammarMyExerciseDto>(
      `/api/grammar/my-exercises/${encodeURIComponent(id)}/visibility`,
      payload
    );
    return data;
  },

  deleteMyExercise: async (id: string): Promise<void> => {
    await httpClient.delete<void>(`/api/grammar/my-exercises/${encodeURIComponent(id)}`);
  },
};
