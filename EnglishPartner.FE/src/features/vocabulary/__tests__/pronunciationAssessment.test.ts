import { beforeEach, describe, expect, it, vi } from 'vitest';
import { httpClient } from '@/shared/api/httpClient';
import { vocabularyApi } from '../api/vocabularyApi';
import type { PronunciationAssessmentApiResultDto } from '../types/vocabulary.types';

vi.mock('@/shared/api/httpClient', () => ({ httpClient: { post: vi.fn() } }));

const input = {
  apiKey: 'test-key',
  vocabularyId: 'e012ee30-03d5-4142-95f6-07e61b40d6ae',
  senseId: '028e2097-7376-446e-b888-01e785a28e00',
  referenceText: 'episode',
  audioBase64: 'AQIDBA==',
  mimeType: 'audio/webm;codecs=opus',
  durationMs: 4490,
  modelCode: 'gemini-3.5-flash-lite',
};

const response: PronunciationAssessmentApiResultDto = {
  assessmentId: 'assessment-1',
  overallScore: 86,
  expectedText: 'episode',
  recognizedText: 'episode',
  modelCode: 'gemini-3.5-flash-lite',
  rubricCode: 'ENGLISH_PRONUNCIATION_V1',
  isAssessable: true,
  status: 'Completed',
  criteria: [{ code: 'Accuracy', name: 'Accuracy', weight: 100, score: 86, weightedScore: 86, feedback: 'Âm cuối cần rõ hơn.' }],
  strengths: ['Trọng âm đúng.'],
  improvements: ['Luyện âm cuối.'],
  suggestedPractice: ['Đọc chậm từng âm tiết.'],
  createdAt: '2026-10-15T10:00:00Z',
};

describe('Pronunciation assessment API contract', () => {
  beforeEach(() => vi.resetAllMocks());

  it('maps recording fields to the BE request instead of leaving Base64Audio empty', async () => {
    vi.mocked(httpClient.post).mockResolvedValueOnce({ data: response });
    await vocabularyApi.assessPronunciation(input);
    expect(httpClient.post).toHaveBeenCalledWith('/api/pronunciation/assessments', {
      apiKey: 'test-key',
      vocabularyId: input.vocabularyId,
      senseId: input.senseId,
      targetType: 'Word',
      targetId: undefined,
      referenceText: 'episode',
      accent: 'US',
      assessmentModelCode: 'gemini-3.5-flash-lite',
      base64Audio: 'AQIDBA==',
      contentType: 'audio/webm;codecs=opus',
      durationMilliseconds: 4490,
    });
  });

  it('maps an example to Sentence and TargetId while preserving the chosen accent', async () => {
    vi.mocked(httpClient.post).mockResolvedValueOnce({ data: response });
    await vocabularyApi.assessPronunciation({ ...input, exampleId: 'example-1', accent: 'UK' });
    expect(httpClient.post).toHaveBeenCalledWith('/api/pronunciation/assessments', expect.objectContaining({
      targetType: 'Sentence', targetId: 'example-1', accent: 'UK',
    }));
  });

  it('maps BE scores, criteria and feedback to the fields rendered by the page', async () => {
    vi.mocked(httpClient.post).mockResolvedValueOnce({ data: response });
    const result = await vocabularyApi.assessPronunciation(input);
    expect(result).toEqual({
      assessmentId: 'assessment-1',
      vocabularyId: input.vocabularyId,
      senseId: input.senseId,
      targetType: 'Word',
      targetId: undefined,
      accent: 'US',
      overallScore: 86,
      isAssessable: true,
      status: 'Completed',
      recognizedTranscript: 'episode',
      referenceText: 'episode',
      modelCodeUsed: 'gemini-3.5-flash-lite',
      rubricCode: 'ENGLISH_PRONUNCIATION_V1',
      phonemeAndStressFeedbackVi: 'Trọng âm đúng.\nLuyện âm cuối.',
      coachingSuggestionVi: 'Đọc chậm từng âm tiết.',
      criterionScores: [{ criterionCode: 'Accuracy', criterionName: 'Accuracy', weight: 100,
        score: 86, weightedScore: 86, feedbackVi: 'Âm cuối cần rõ hơn.' }],
      assessedAtUtc: response.createdAt,
    });
  });

  it('preserves an unassessable recording without inventing a score or transcript', async () => {
    vi.mocked(httpClient.post).mockResolvedValueOnce({ data: {
      ...response, overallScore: null, recognizedText: null, isAssessable: false, status: 'NotAssessable',
    } });
    const result = await vocabularyApi.assessPronunciation(input);
    expect(result.overallScore).toBeNull();
    expect(result.isAssessable).toBe(false);
    expect(result.recognizedTranscript).toBe('');
  });

  it('propagates API errors so the page can display them', async () => {
    const error = { response: { status: 400, data: { error: 'Bản ghi âm không được để trống.' } } };
    vi.mocked(httpClient.post).mockRejectedValueOnce(error);
    await expect(vocabularyApi.assessPronunciation(input)).rejects.toBe(error);
  });
});
