import { beforeEach, describe, expect, it, vi } from 'vitest';
import { httpClient } from '@/shared/api/httpClient';
import { vocabularyApi } from '../api/vocabularyApi';
import type {
  SynthesizePronunciationAudioRequestDto,
  SynthesizePronunciationAudioResponseDto,
} from '@/shared/api/openapi.types';

vi.mock('@/shared/api/httpClient', () => ({
  httpClient: {
    post: vi.fn(),
    get: vi.fn(),
  },
}));

const mockResponse: SynthesizePronunciationAudioResponseDto = {
  text: 'resilient',
  targetType: 'word',
  accent: 'US',
  voiceName: 'Aoede',
  speechSpeed: 1.0,
  modelCodeUsed: 'gemini-3.8-live',
  systemPromptUsed: 'System prompt',
  mimeType: 'audio/wav',
  sampleRate: 24000,
  durationMs: 950,
  audioBase64: 'UklGRi4AAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQAAAAA=',
};

describe('Pronunciation synthesis audio cache client contracts', () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it('posts synthesis request with targetId and vocabulary context', async () => {
    vi.mocked(httpClient.post).mockResolvedValueOnce({ data: mockResponse });

    const payload: SynthesizePronunciationAudioRequestDto = {
      apiKey: '',
      text: 'resilient',
      targetType: 'word',
      accent: 'US',
      voiceName: 'Aoede',
      speechSpeed: 1.0,
      modelCode: 'gemini-3.8-live',
      targetId: '30000000-0000-0000-0000-000000000006',
      vocabularyId: '30000000-0000-0000-0000-000000000006',
    };

    const res = await vocabularyApi.synthesizePronunciationAudio(payload);

    expect(httpClient.post).toHaveBeenCalledWith('/api/pronunciation/synthesize', payload);
    expect(res).toEqual(mockResponse);
  });

  it('posts synthesis request for example sentence with example targetId and sense context', async () => {
    vi.mocked(httpClient.post).mockResolvedValueOnce({
      data: { ...mockResponse, targetType: 'sentence', text: 'She is resilient.' },
    });

    const payload: SynthesizePronunciationAudioRequestDto = {
      apiKey: 'key-123',
      text: 'She is resilient.',
      targetType: 'sentence',
      accent: 'UK',
      voiceName: 'Puck',
      speechSpeed: 0.8,
      modelCode: 'gemini-3.8-live-extended-thinking',
      targetId: '40000000-0000-0000-0000-000000000001',
      vocabularyId: '30000000-0000-0000-0000-000000000006',
      senseId: '50000000-0000-0000-0000-000000000001',
    };

    await vocabularyApi.synthesizePronunciationAudio(payload);

    expect(httpClient.post).toHaveBeenCalledWith('/api/pronunciation/synthesize', payload);
  });

  it('queries GET /api/pronunciation/audio for cache-only lookup', async () => {
    vi.mocked(httpClient.get).mockResolvedValueOnce({ data: mockResponse });

    const params = {
      targetType: 'word' as const,
      targetId: '30000000-0000-0000-0000-000000000006',
      voiceName: 'Aoede',
      speechSpeed: 1.0,
      accent: 'US' as const,
      modelCode: 'gemini-3.8-live',
      vocabularyId: '30000000-0000-0000-0000-000000000006',
    };

    const res = await vocabularyApi.getPronunciationAudio(params);

    expect(httpClient.get).toHaveBeenCalledWith('/api/pronunciation/audio', { params });
    expect(res).toEqual(mockResponse);
  });
});
