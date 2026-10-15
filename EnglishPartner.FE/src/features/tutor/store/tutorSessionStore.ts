import { create } from 'zustand';
import type { CefrLevel, ConversationModeCode, TeacherVoiceName } from '@/shared/api/openapi.types';

const API_KEY_STORAGE_KEY = 'lingomint_api_key';

function loadInitialApiKey(): string {
  try {
    return localStorage.getItem(API_KEY_STORAGE_KEY) || '';
  } catch {
    return '';
  }
}

/**
 * Zustand Store strictly for Client UI State:
 * - Google AI Studio API Key (exclusively rendered in the Left Menu, persisted in localStorage)
 * - Left Menu collapse toggle (Student Portal & Admin Portal)
 * - Live Tutor Setup Modal state & session setup parameters
 */
interface TutorSessionUiState {
  apiKey: string;
  showApiKey: boolean;
  apiKeyFocusTrigger: number;
  isStudentSidebarCollapsed: boolean;
  isAdminSidebarCollapsed: boolean;
  cefrLevel: CefrLevel;
  conversationMode: ConversationModeCode;
  voiceName: TeacherVoiceName;
  speechSpeed: number;
  topic: string;
  customSystemPrompt: string;
  extendedThinking: boolean;
  activeConversationId: string | null;
  isSetupModalOpen: boolean;
  isHistoryDrawerOpen: boolean;
  isCorrectionDrawerOpen: boolean;
  showOnlyFeaturedVoices: boolean;

  setApiKey: (key: string) => void;
  toggleShowApiKey: () => void;
  requestFocusLeftMenuApiKey: () => void;
  toggleStudentSidebar: () => void;
  setStudentSidebarCollapsed: (collapsed: boolean) => void;
  toggleAdminSidebar: () => void;
  setAdminSidebarCollapsed: (collapsed: boolean) => void;
  setCefrLevel: (level: CefrLevel) => void;
  setConversationMode: (mode: ConversationModeCode) => void;
  setVoiceName: (voice: TeacherVoiceName) => void;
  setSpeechSpeed: (speed: number) => void;
  setTopic: (topic: string) => void;
  setCustomSystemPrompt: (prompt: string) => void;
  setExtendedThinking: (enabled: boolean) => void;
  setActiveConversationId: (id: string | null) => void;
  setSetupModalOpen: (open: boolean) => void;
  setHistoryDrawerOpen: (open: boolean) => void;
  setCorrectionDrawerOpen: (open: boolean) => void;
  setShowOnlyFeaturedVoices: (onlyFeatured: boolean) => void;
  applyConversationSettings: (settings: {
    id: string;
    cefrLevel: CefrLevel;
    conversationMode: ConversationModeCode;
    voiceName: TeacherVoiceName;
    speechSpeed: number;
    topic: string;
    customSystemPrompt?: string | null;
    extendedThinking: boolean;
  }) => void;
}

export const useTutorSessionStore = create<TutorSessionUiState>((set) => ({
  apiKey: loadInitialApiKey(),
  showApiKey: false,
  apiKeyFocusTrigger: 0,
  isStudentSidebarCollapsed: false,
  isAdminSidebarCollapsed: false,
  cefrLevel: 'B1',
  conversationMode: 'daily_conversation',
  voiceName: 'Aoede',
  speechSpeed: 1.0,
  topic: '.NET 10, Clean Architecture, DDD & CQRS',
  customSystemPrompt: '',
  extendedThinking: false,
  activeConversationId: null,
  isSetupModalOpen: false,
  isHistoryDrawerOpen: false,
  isCorrectionDrawerOpen: false,
  showOnlyFeaturedVoices: true,

  setApiKey: (apiKey) => {
    try {
      localStorage.setItem(API_KEY_STORAGE_KEY, apiKey);
    } catch {
      // ignore
    }
    set({ apiKey });
  },
  toggleShowApiKey: () => set((s) => ({ showApiKey: !s.showApiKey })),
  requestFocusLeftMenuApiKey: () =>
    set((s) => ({
      isStudentSidebarCollapsed: false,
      apiKeyFocusTrigger: s.apiKeyFocusTrigger + 1,
    })),
  toggleStudentSidebar: () =>
    set((s) => ({ isStudentSidebarCollapsed: !s.isStudentSidebarCollapsed })),
  setStudentSidebarCollapsed: (isStudentSidebarCollapsed) => set({ isStudentSidebarCollapsed }),
  toggleAdminSidebar: () =>
    set((s) => ({ isAdminSidebarCollapsed: !s.isAdminSidebarCollapsed })),
  setAdminSidebarCollapsed: (isAdminSidebarCollapsed) => set({ isAdminSidebarCollapsed }),
  setCefrLevel: (cefrLevel) => set({ cefrLevel }),
  setConversationMode: (conversationMode) => set({ conversationMode }),
  setVoiceName: (voiceName) => set({ voiceName }),
  setSpeechSpeed: (speechSpeed) => set({ speechSpeed }),
  setTopic: (topic) => set({ topic }),
  setCustomSystemPrompt: (customSystemPrompt) => set({ customSystemPrompt }),
  setExtendedThinking: (extendedThinking) => set({ extendedThinking }),
  setActiveConversationId: (activeConversationId) => set({ activeConversationId }),
  setSetupModalOpen: (isSetupModalOpen) => set({ isSetupModalOpen }),
  setHistoryDrawerOpen: (isHistoryDrawerOpen) => set({ isHistoryDrawerOpen }),
  setCorrectionDrawerOpen: (isCorrectionDrawerOpen) => set({ isCorrectionDrawerOpen }),
  setShowOnlyFeaturedVoices: (showOnlyFeaturedVoices) => set({ showOnlyFeaturedVoices }),
  applyConversationSettings: (s) =>
    set({
      activeConversationId: s.id,
      cefrLevel: s.cefrLevel || 'B1',
      conversationMode: s.conversationMode || 'daily_conversation',
      voiceName: s.voiceName || 'Aoede',
      speechSpeed: s.speechSpeed || 1.0,
      topic: s.topic || '',
      customSystemPrompt: s.customSystemPrompt ?? '',
      extendedThinking: Boolean(s.extendedThinking),
    }),
}));
