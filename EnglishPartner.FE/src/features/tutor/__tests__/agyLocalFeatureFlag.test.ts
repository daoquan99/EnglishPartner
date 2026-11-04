import { afterEach, describe, expect, it, vi } from 'vitest';
import { useTutorSessionStore } from '../store/tutorSessionStore';
import { DEFAULT_AI_MODELS } from '@/features/catalog/api/catalogDefaults';
import { withoutDisabledAgyModels } from '@/features/catalog/api/catalogApi';

// Pin the flag so a developer .env with VITE_ENABLE_AGY_LOCAL=true cannot change the outcome.
vi.mock('@/shared/config/features', () => ({ isAgyLocalEnabled: false }));

class MockStorage implements Storage {
  private readonly values = new Map<string, string>();

  get length() {
    return this.values.size;
  }

  key(index: number) {
    return Array.from(this.values.keys())[index] ?? null;
  }

  getItem(key: string) {
    return this.values.get(key) ?? null;
  }

  setItem(key: string, value: string) {
    this.values.set(key, String(value));
  }

  removeItem(key: string) {
    this.values.delete(key);
  }

  clear() {
    this.values.clear();
  }
}

describe('AGY Local feature flag', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.resetModules();
  });

  it('ignores stale AGY provider selection while the feature is disabled', () => {
    useTutorSessionStore.setState({ aiProvider: 'gemini' });

    useTutorSessionStore.getState().setAiProvider('agy');

    expect(useTutorSessionStore.getState().aiProvider).toBe('gemini');
  });

  it('loads gemini when localStorage still contains AGY', async () => {
    const storage = new MockStorage();
    storage.setItem('lingomint_ai_provider', 'agy');
    vi.stubGlobal('localStorage', storage);
    vi.resetModules();

    const { useTutorSessionStore: freshStore } = await import('../store/tutorSessionStore');

    expect(freshStore.getState().aiProvider).toBe('gemini');
  });

  it('drops an AGY assessment model saved in localStorage', async () => {
    const storage = new MockStorage();
    storage.setItem('lingomint_assessment_model', 'gemini-3.8-flash-high');
    vi.stubGlobal('localStorage', storage);
    vi.resetModules();

    const { useTutorSessionStore: freshStore } = await import('../store/tutorSessionStore');

    expect(freshStore.getState().assessmentModelCode).toBe('gemini-3.8-flash');
  });

  it('removes AGY models from catalog lists, including the offline fallback', () => {
    const codes = withoutDisabledAgyModels(DEFAULT_AI_MODELS).map((m) => m.modelCode);

    expect(codes).toContain('gemini-3.8-flash');
    expect(codes).not.toContain('gemini-3.8-flash-medium');
    expect(codes).not.toContain('gemini-3.8-flash-low');
    expect(codes).not.toContain('gemini-3.8-flash-high');
  });
});
