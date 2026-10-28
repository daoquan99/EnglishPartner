import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import {
  API_KEY_STORAGE_KEY,
  API_KEY_REMEMBER_STORAGE_KEY,
  initApiKeyStorage,
  useTutorSessionStore,
  setTestStorage,
} from '../store/tutorSessionStore';
import { useAuthStore } from '@/features/auth/store/authStore';

class MockStorage implements Storage {
  private store: Map<string, string> = new Map();

  get length(): number {
    return this.store.size;
  }

  key(index: number): string | null {
    return Array.from(this.store.keys())[index] ?? null;
  }

  getItem(key: string): string | null {
    return this.store.has(key) ? this.store.get(key)! : null;
  }

  setItem(key: string, value: string): void {
    this.store.set(key, String(value));
  }

  removeItem(key: string): void {
    this.store.delete(key);
  }

  clear(): void {
    this.store.clear();
  }
}

describe('BYO API Key Storage & Migration (ADR-0003, S9)', () => {
  let mockLocal: MockStorage;
  let mockSession: MockStorage;

  beforeEach(() => {
    mockLocal = new MockStorage();
    mockSession = new MockStorage();
    setTestStorage({ local: mockLocal, session: mockSession });
    useTutorSessionStore.setState({
      apiKey: '',
      rememberApiKey: false,
    });
  });

  afterEach(() => {
    setTestStorage(null);
  });

  it('saves API key in sessionStorage by default when remember is false', () => {
    const store = useTutorSessionStore.getState();
    expect(store.rememberApiKey).toBe(false);

    store.setApiKey('AIzaSyTestDefaultSessionKey123');

    expect(mockSession.getItem(API_KEY_STORAGE_KEY)).toBe('AIzaSyTestDefaultSessionKey123');
    expect(mockLocal.getItem(API_KEY_STORAGE_KEY)).toBeNull();
    expect(useTutorSessionStore.getState().apiKey).toBe('AIzaSyTestDefaultSessionKey123');
  });

  it('saves API key in localStorage when remember is enabled', () => {
    const store = useTutorSessionStore.getState();
    store.setRememberApiKey(true);

    expect(mockLocal.getItem(API_KEY_REMEMBER_STORAGE_KEY)).toBe('true');

    store.setApiKey('AIzaSyTestRememberedKey456');

    expect(mockLocal.getItem(API_KEY_STORAGE_KEY)).toBe('AIzaSyTestRememberedKey456');
    expect(mockSession.getItem(API_KEY_STORAGE_KEY)).toBeNull();
  });

  it('migrates key from localStorage to sessionStorage when remember is toggled OFF', () => {
    const store = useTutorSessionStore.getState();
    store.setRememberApiKey(true);
    store.setApiKey('AIzaSyActiveKey789');

    expect(mockLocal.getItem(API_KEY_STORAGE_KEY)).toBe('AIzaSyActiveKey789');

    // Toggle remember OFF
    store.setRememberApiKey(false);

    expect(mockLocal.getItem(API_KEY_REMEMBER_STORAGE_KEY)).toBeNull();
    expect(mockLocal.getItem(API_KEY_STORAGE_KEY)).toBeNull();
    expect(mockSession.getItem(API_KEY_STORAGE_KEY)).toBe('AIzaSyActiveKey789');
  });

  it('migrates legacy localStorage key to sessionStorage on first initialization without remember flag', () => {
    // Simulate old app version that stored key in localStorage
    mockLocal.setItem(API_KEY_STORAGE_KEY, 'AIzaSyLegacyOldKey999');
    expect(mockLocal.getItem(API_KEY_REMEMBER_STORAGE_KEY)).toBeNull();

    const result = initApiKeyStorage();

    // Result should load key with rememberApiKey = false
    expect(result.apiKey).toBe('AIzaSyLegacyOldKey999');
    expect(result.rememberApiKey).toBe(false);

    // Old key must be moved to sessionStorage and removed from localStorage
    expect(mockSession.getItem(API_KEY_STORAGE_KEY)).toBe('AIzaSyLegacyOldKey999');
    expect(mockLocal.getItem(API_KEY_STORAGE_KEY)).toBeNull();
  });

  it('retains localStorage key on initialization when remember flag was previously enabled', () => {
    mockLocal.setItem(API_KEY_STORAGE_KEY, 'AIzaSyPersistedKey888');
    mockLocal.setItem(API_KEY_REMEMBER_STORAGE_KEY, 'true');

    const result = initApiKeyStorage();

    expect(result.apiKey).toBe('AIzaSyPersistedKey888');
    expect(result.rememberApiKey).toBe(true);
    expect(mockLocal.getItem(API_KEY_STORAGE_KEY)).toBe('AIzaSyPersistedKey888');
  });

  it('clears the API key from both storages and the store when the auth session is cleared (logout)', () => {
    const store = useTutorSessionStore.getState();
    store.setRememberApiKey(true);
    store.setApiKey('AIzaSyRememberedKeyOfLearnerA');
    mockSession.setItem(API_KEY_STORAGE_KEY, 'AIzaSyStaleSessionKey');
    expect(mockLocal.getItem(API_KEY_STORAGE_KEY)).toBe('AIzaSyRememberedKeyOfLearnerA');

    useAuthStore.getState().clearSession();

    expect(mockLocal.getItem(API_KEY_STORAGE_KEY)).toBeNull();
    expect(mockLocal.getItem(API_KEY_REMEMBER_STORAGE_KEY)).toBeNull();
    expect(mockSession.getItem(API_KEY_STORAGE_KEY)).toBeNull();
    expect(useTutorSessionStore.getState().apiKey).toBe('');
    expect(useTutorSessionStore.getState().rememberApiKey).toBe(false);
  });
});
