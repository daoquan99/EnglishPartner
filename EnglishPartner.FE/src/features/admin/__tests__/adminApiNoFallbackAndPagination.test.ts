import { describe, it, expect, vi, beforeEach } from 'vitest';
import { adminApi } from '../api/adminApi';
import { httpClient } from '@/shared/api/httpClient';

vi.mock('@/shared/api/httpClient', () => ({
  httpClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe('adminApi — No Fake Fallback Data & Server Pagination Contracts', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('listUsers', () => {
    it('rejects with error when httpClient.get fails (does NOT return fallback users)', async () => {
      vi.mocked(httpClient.get).mockRejectedValueOnce(new Error('Network error 500'));

      await expect(adminApi.listUsers()).rejects.toThrow('Network error 500');
    });

    it('sends page, pageSize, search, and paged=true parameters', async () => {
      const mockPagedResult = {
        items: [
          {
            id: 'u-1',
            username: 'alice',
            email: 'alice@lingomint.local',
            fullName: 'Alice Smith',
            preferredCefrLevel: 'B2',
            preferredVoiceName: 'Aoede',
            timeZoneId: 'Asia/Ho_Chi_Minh',
            isActive: true,
            roles: ['LEARNER'],
            permissions: [],
          },
        ],
        totalCount: 42,
        page: 2,
        pageSize: 20,
        totalPages: 3,
      };

      vi.mocked(httpClient.get).mockResolvedValueOnce({ data: mockPagedResult });

      const result = await adminApi.listUsers({
        page: 2,
        pageSize: 20,
        search: 'alice',
        paged: true,
      });

      expect(httpClient.get).toHaveBeenCalledWith('/api/users', {
        params: {
          page: 2,
          pageSize: 20,
          search: 'alice',
          paged: true,
        },
      });

      expect(result.items).toHaveLength(1);
      expect(result.totalCount).toBe(42);
      expect(result.page).toBe(2);
      expect(result.pageSize).toBe(20);
    });
  });

  describe('listVocabularies', () => {
    it('rejects with error when httpClient.get fails (does NOT return fallback vocabulary cards)', async () => {
      vi.mocked(httpClient.get).mockRejectedValueOnce(new Error('Database unavailable'));

      await expect(adminApi.listVocabularies()).rejects.toThrow('Database unavailable');
    });

    it('sends page, pageSize, search, cefrLevel and paged=true parameters', async () => {
      const mockPagedVocab = {
        items: [
          {
            vocabularyId: 'v-1',
            senseId: 's-1',
            word: 'resilient',
            cefrLevel: 'B2',
            definitionEn: 'Able to recover quickly from difficult conditions.',
            meaningVi: 'Kiên cường, có khả năng phục hồi nhanh',
            partOfSpeech: 'adjective',
            pronunciations: [],
            synonyms: ['tough', 'robust'],
            antonyms: ['fragile'],
            wordFamilies: [],
            collocations: [],
            examples: [],
            grammarPatterns: [],
            learningStatus: 'New',
            repetitionCount: 0,
            intervalDays: 0,
            easeFactor: 2.5,
            isCompletedToday: false,
          },
        ],
        totalCount: 150,
        page: 1,
        pageSize: 20,
        totalPages: 8,
      };

      vi.mocked(httpClient.get).mockResolvedValueOnce({ data: mockPagedVocab });

      const result = await adminApi.listVocabularies({
        page: 1,
        pageSize: 20,
        search: 'resilient',
        cefrLevel: 'B2',
        paged: true,
      });

      expect(httpClient.get).toHaveBeenCalledWith('/api/vocabularies', {
        params: {
          page: 1,
          pageSize: 20,
          search: 'resilient',
          cefrLevel: 'B2',
          paged: true,
        },
      });

      expect(result.items).toHaveLength(1);
      expect(result.totalCount).toBe(150);
      expect(result.items[0].word).toBe('resilient');
    });
  });

  describe('listRoles and listPermissions', () => {
    it('rejects when listRoles fails without returning fallback roles', async () => {
      vi.mocked(httpClient.get).mockRejectedValueOnce(new Error('Roles fetch failed'));
      await expect(adminApi.listRoles()).rejects.toThrow('Roles fetch failed');
    });

    it('rejects when listPermissions fails without returning fallback permissions', async () => {
      vi.mocked(httpClient.get).mockRejectedValueOnce(new Error('Permissions fetch failed'));
      await expect(adminApi.listPermissions()).rejects.toThrow('Permissions fetch failed');
    });
  });
});
