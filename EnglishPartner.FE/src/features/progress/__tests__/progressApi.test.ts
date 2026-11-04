import { describe, it, expect, vi, beforeEach } from 'vitest';
import { progressApi } from '../api/progressApi';
import { httpClient } from '@/shared/api/httpClient';

vi.mock('@/shared/api/httpClient', () => ({
  httpClient: {
    get: vi.fn(),
  },
}));

describe('progressApi Client Contracts', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('calls GET /api/progress/overview without params when days is undefined', async () => {
    const mockOverview = { days: 30, currentStreakDays: 3 };
    (httpClient.get as any).mockResolvedValueOnce({ data: mockOverview });

    const result = await progressApi.getOverview();

    expect(httpClient.get).toHaveBeenCalledWith('/api/progress/overview', {
      params: undefined,
    });
    expect(result).toEqual(mockOverview);
  });

  it('calls GET /api/progress/overview with days=7 param', async () => {
    const mockOverview = { days: 7, currentStreakDays: 1 };
    (httpClient.get as any).mockResolvedValueOnce({ data: mockOverview });

    const result = await progressApi.getOverview(7);

    expect(httpClient.get).toHaveBeenCalledWith('/api/progress/overview', {
      params: { days: 7 },
    });
    expect(result).toEqual(mockOverview);
  });

  it('calls GET /api/progress/overview with days=30 param', async () => {
    const mockOverview = { days: 30, currentStreakDays: 5 };
    (httpClient.get as any).mockResolvedValueOnce({ data: mockOverview });

    const result = await progressApi.getOverview(30);

    expect(httpClient.get).toHaveBeenCalledWith('/api/progress/overview', {
      params: { days: 30 },
    });
    expect(result).toEqual(mockOverview);
  });

  it('calls GET /api/progress/overview with days=90 param', async () => {
    const mockOverview = { days: 90, currentStreakDays: 14 };
    (httpClient.get as any).mockResolvedValueOnce({ data: mockOverview });

    const result = await progressApi.getOverview(90);

    expect(httpClient.get).toHaveBeenCalledWith('/api/progress/overview', {
      params: { days: 90 },
    });
    expect(result).toEqual(mockOverview);
  });
});
