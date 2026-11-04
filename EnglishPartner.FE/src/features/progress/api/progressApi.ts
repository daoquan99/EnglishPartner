import { httpClient } from '@/shared/api/httpClient';
import type { ProgressOverviewDto } from '@/shared/api/openapi.types';

export const progressApi = {
  getOverview: async (days?: number): Promise<ProgressOverviewDto> => {
    const response = await httpClient.get<ProgressOverviewDto>('/api/progress/overview', {
      params: typeof days === 'number' ? { days } : undefined,
    });
    return response.data;
  },
};
