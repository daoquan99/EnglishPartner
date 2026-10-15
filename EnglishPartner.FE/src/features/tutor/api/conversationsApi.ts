import { httpClient } from '@/shared/api/httpClient';
import type { CefrLevel, ConversationModeCode, TeacherVoiceName } from '@/shared/api/openapi.types';
import type { ConversationDetailDto, ConversationSummaryDto } from '../types/tutor.types';

export interface CreateOrUpdateConversationPayload {
  title?: string;
  cefrLevel: CefrLevel;
  voiceName: TeacherVoiceName;
  speechSpeed: number;
  conversationMode: ConversationModeCode;
  topic: string;
  customSystemPrompt?: string | null;
  extendedThinking: boolean;
}

export const conversationsApi = {
  async listConversations(): Promise<ConversationSummaryDto[]> {
    try {
      const { data } = await httpClient.get<ConversationSummaryDto[]>('/api/conversations');
      return Array.isArray(data) ? data : [];
    } catch {
      return [];
    }
  },

  async getConversationDetail(id: string): Promise<ConversationDetailDto | null> {
    const { data } = await httpClient.get<ConversationDetailDto>(`/api/conversations/${id}`);
    return data ?? null;
  },

  async createConversation(payload: CreateOrUpdateConversationPayload): Promise<ConversationSummaryDto> {
    const { data } = await httpClient.post<ConversationSummaryDto>('/api/conversations', payload);
    return data;
  },

  async updateConversationSettings(
    id: string,
    payload: CreateOrUpdateConversationPayload
  ): Promise<ConversationSummaryDto> {
    const { data } = await httpClient.put<ConversationSummaryDto>(
      `/api/conversations/${id}/settings`,
      payload
    );
    return data;
  },

  async deleteConversation(id: string): Promise<void> {
    await httpClient.delete(`/api/conversations/${id}`);
  },

  async previewSystemPrompt(payload: {
    cefrLevel: CefrLevel;
    conversationMode: ConversationModeCode;
    voiceName: TeacherVoiceName;
    speechSpeed: number;
    topic: string;
    customSystemPrompt?: string;
    extendedThinking: boolean;
  }): Promise<{ systemInstruction: string }> {
    const { data } = await httpClient.post<{ systemInstruction: string }>(
      '/api/prompts/preview',
      payload
    );
    return data;
  },
};
