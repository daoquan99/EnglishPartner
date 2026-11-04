import { useQuery } from '@tanstack/react-query';
import { progressApi } from '../api/progressApi';
import type { ProgressOverviewDto } from '@/shared/api/openapi.types';

export const progressKeys = {
  all: ['progress'] as const,
  overview: (days?: number) => ['progress', 'overview', days] as const,
};

export function useProgressOverviewQuery(days: number = 30) {
  return useQuery<ProgressOverviewDto, Error>({
    queryKey: progressKeys.overview(days),
    queryFn: () => progressApi.getOverview(days),
    staleTime: 5 * 60 * 1000,
  });
}
