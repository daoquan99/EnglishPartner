import { create } from 'zustand';
import type { CefrLevel, ConversationModeCode, TeacherVoiceName } from '@/shared/api/openapi.types';

/**
 * Zustand Store strictly for Live Tutor UI & Session Setup State.
 * Server state (Modes, Voices, Speeds, Topics, Conversation History) is managed by TanStack Query.
 */
interface TutorSessionUiState {
  cefrLevel: CefrLevel;
  conversationMode: ConversationModeCode;
  voiceName: TeacherVoiceName;
  speechSpeed: number;
  topic: string;
  customSystemPrompt: string;
  extendedThinking: boolean;
  activeConversationId: string | null;
  isSetupPanelOpen: boolean;
  isHistoryDrawerOpen: boolean;
  isCorrectionDrawerOpen: boolean;
  showOnlyFeaturedVoices: boolean;

  setCefrLevel: (level: CefrLevel) => void;
  setConversationMode: (mode: ConversationModeCode) => void;
  setVoiceName: (voice: TeacherVoiceName) => void;
  setSpeechSpeed: (speed: number) => void;
  setTopic: (topic: string) => void;
  setCustomSystemPrompt: (prompt: string) => void;
  setExtendedThinking: (enabled: boolean) => void;
  setActiveConversationId: (id: string | null) => void;
  setSetupPanelOpen: (open: boolean) => void;
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
  cefrLevel: 'B1',
  conversationMode: 'daily_conversation',
  voiceName: 'Aoede',
  speechSpeed: 1.0,
  topic: '.NET 10, Clean Architecture, DDD & CQRS',
  customSystemPrompt: '',
  extendedThinking: false,
  activeConversationId: null,
  isSetupPanelOpen: true,
  isHistoryDrawerOpen: false,
  isCorrectionDrawerOpen: false,
  showOnlyFeaturedVoices: true,

  setCefrLevel: (cefrLevel) => set({ cefrLevel }),
  setConversationMode: (conversationMode) => set({ conversationMode }),
  setVoiceName: (voiceName) => set({ voiceName }),
  setSpeechSpeed: (speechSpeed) => set({ speechSpeed }),
  setTopic: (topic) => set({ topic }),
  setCustomSystemPrompt: (customSystemPrompt) => set({ customSystemPrompt }),
  setExtendedThinking: (extendedThinking) => set({ extendedThinking }),
  setActiveConversationId: (activeConversationId) => set({ activeConversationId }),
  setSetupPanelOpen: (isSetupPanelOpen) => set({ isSetupPanelOpen }),
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
