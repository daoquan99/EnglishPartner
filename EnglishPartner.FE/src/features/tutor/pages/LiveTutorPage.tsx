import React from 'react';
import {
  Mic,
  PhoneOff,
  History,
  Settings,
  Wifi,
  AlertCircle,
  MessageSquarePlus,
} from 'lucide-react';
import { TutorSetupSidebar as TutorSetupModal } from '../components/TutorSetupSidebar';
import { ConversationHistoryModal } from '../components/ConversationHistoryModal';
import { LiveMessengerStream } from '../components/LiveMessengerStream';
import { GrammarCorrectionPanel } from '../components/GrammarCorrectionPanel';
import { useTutorSessionStore } from '../store/tutorSessionStore';
import { useThreeHubLiveSession } from '../hooks/useThreeHubLiveSession';
import {
  useConversationsListQuery,
  useCreateConversationMutation,
  useDeleteConversationMutation,
  useUpdateConversationSettingsMutation,
} from '../hooks/useConversations';
import {
  normalizeGrammarCorrection,
  type ConversationDetailDto,
  type GrammarCorrectionItem,
} from '../types/tutor.types';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { cn } from '@/shared/lib/cn';

export const LiveTutorPage: React.FC = () => {
  const {
    apiKey,
    cefrLevel,
    conversationMode,
    voiceName,
    speechSpeed,
    topic,
    customSystemPrompt,
    extendedThinking,
    activeConversationId,
    isSetupModalOpen,
    isHistoryDrawerOpen,
    setActiveConversationId,
    setSetupModalOpen,
    setHistoryDrawerOpen,
    requestFocusLeftMenuApiKey,
    applyConversationSettings,
  } = useTutorSessionStore();

  const {
    phase,
    hubStates,
    turns,
    corrections,
    isAnalyzingGrammar,
    errorBanner,
    micLevel,
    aiAudioLevel,
    startSession,
    stopSession,
    analyzeTurnGrammar,
    loadPersistedSessionData,
    resetConversationWorkspace,
    clearError,
  } = useThreeHubLiveSession();

  const { data: historyList = [] } = useConversationsListQuery();
  const createConvMutation = useCreateConversationMutation();
  const updateSettingsMutation = useUpdateConversationSettingsMutation();
  const deleteConvMutation = useDeleteConversationMutation();

  const isSessionActive = phase !== 'idle' && phase !== 'error';

  const latestUserTurn = [...turns]
    .reverse()
    .find((t) => t.speaker === 'user' && t.englishLine1.trim().length > 0);

  /**
   * Switches to a brand-new conversation workspace:
   * - Stops any active live microphone/audio session
   * - Clears turns & grammar corrections from the screen
   * - Resets activeConversationId to null so the next session creates a fresh conversation in DB
   */
  const handleSwitchNewConversation = async () => {
    await resetConversationWorkspace();
    setActiveConversationId(null);
  };

  const handleSaveSettingsOnly = async () => {
    if (activeConversationId) {
      try {
        await updateSettingsMutation.mutateAsync({
          id: activeConversationId,
          payload: {
            title: `${conversationMode} (${cefrLevel}) — ${topic || 'Free Talk'}`,
            cefrLevel,
            voiceName,
            speechSpeed,
            conversationMode,
            topic,
            customSystemPrompt,
            extendedThinking,
          },
        });
      } catch {
        // ignore offline
      }
    }
    setSetupModalOpen(false);
  };

  const handleStartLive = async () => {
    setSetupModalOpen(false);

    if (!apiKey.trim()) {
      requestFocusLeftMenuApiKey();
    }

    let convId = activeConversationId;
    if (!convId) {
      try {
        const created = await createConvMutation.mutateAsync({
          title: `${conversationMode} (${cefrLevel}) — ${topic || 'Free Talk'}`,
          cefrLevel,
          voiceName,
          speechSpeed,
          conversationMode,
          topic,
          customSystemPrompt,
          extendedThinking,
        });
        convId = created.id;
        setActiveConversationId(convId);
      } catch {
        // Proceed with local UUID if BE DB is offline
      }
    } else {
      try {
        await updateSettingsMutation.mutateAsync({
          id: convId,
          payload: {
            title: `${conversationMode} (${cefrLevel}) — ${topic || 'Free Talk'}`,
            cefrLevel,
            voiceName,
            speechSpeed,
            conversationMode,
            topic,
            customSystemPrompt,
            extendedThinking,
          },
        });
      } catch {
        // ignore offline
      }
    }

    await startSession({
      apiKey,
      cefrLevel,
      extendedThinking,
      voiceName,
      speechSpeed,
      conversationMode,
      topic,
      customSystemPrompt,
      conversationId: convId,
    });
  };

  const handleResumeFromModal = (detail: ConversationDetailDto) => {
    applyConversationSettings({
      id: detail.id,
      cefrLevel: detail.cefrLevel,
      conversationMode: detail.conversationMode,
      voiceName: detail.voiceName,
      speechSpeed: detail.speechSpeed,
      topic: detail.topic,
      customSystemPrompt: detail.customSystemPrompt,
      extendedThinking: detail.extendedThinking,
    });

    const normalizedCorrections = (detail.corrections || [])
      .map((c) => normalizeGrammarCorrection(c))
      .filter((c): c is GrammarCorrectionItem => c !== null);

    loadPersistedSessionData(
      (detail.messages || []).map((m) => ({
        turnId: m.turnId,
        speaker: m.speaker,
        englishLine1: m.transcriptLine1,
        vietnameseLine2: m.translationLine2,
        isFinal: true,
        timestamp: new Date(m.createdAtUtc).getTime(),
      })),
      normalizedCorrections
    );
  };

  return (
    <div className="space-y-4">
      {/* Top Action & Configuration Bar */}
      <div className="bg-white rounded-3xl border border-slate-200/90 px-4 sm:px-5 py-3.5 flex flex-wrap items-center justify-between gap-3 shadow-xs">
        <div className="flex items-center gap-2.5 flex-wrap">
          <Button
            variant="outline"
            size="sm"
            onClick={() => setSetupModalOpen(true)}
            className="border-emerald-300 bg-emerald-50/70 hover:bg-emerald-100/80 text-emerald-900 font-bold"
          >
            <Settings className="w-4 h-4 text-emerald-700" />
            Thiết lập phiên luyện nói
          </Button>

          <Button
            variant="outline"
            size="sm"
            onClick={() => void handleSwitchNewConversation()}
            title="Tạo cuộc hội thoại mới và làm mới khung chat"
            className="border-slate-300 bg-white hover:bg-emerald-50 hover:border-emerald-300 text-slate-800 hover:text-emerald-900 font-semibold"
          >
            <MessageSquarePlus className="w-4 h-4 text-emerald-600" />
            Hội thoại mới
          </Button>

          <Badge variant="emerald">CEFR {cefrLevel}</Badge>
          <Badge variant="sky">{conversationMode}</Badge>
          <Badge variant="violet">Voice: {voiceName}</Badge>
          <Badge variant="amber">Speed: {speechSpeed.toFixed(2)}x</Badge>
          {topic && (
            <span className="hidden md:inline-block text-xs font-semibold text-slate-700 bg-slate-100 px-3 py-1 rounded-full truncate max-w-xs">
              Chủ đề: {topic}
            </span>
          )}
        </div>

        <div className="flex items-center gap-2">
          <div className="hidden xl:flex items-center gap-1.5 text-[11px] font-semibold text-slate-600 bg-slate-50 px-3 py-1.5 rounded-xl border border-slate-200">
            <Wifi
              className={cn(
                'w-3.5 h-3.5',
                hubStates.tutorHub === 'connected' ? 'text-emerald-600' : 'text-slate-400'
              )}
            />
            <span>Hub 1: {hubStates.tutorHub}</span>
            <span>·</span>
            <span>Hub 2: {hubStates.transcribeHub}</span>
            <span>·</span>
            <span>Hub 3: {hubStates.translateHub}</span>
          </div>

          <Button
            variant="outline"
            size="sm"
            onClick={() => setHistoryDrawerOpen(true)}
          >
            <History className="w-3.5 h-3.5" />
            Lịch sử ({historyList.length})
          </Button>
        </div>
      </div>

      {errorBanner && (
        <div className="bg-rose-50 border border-rose-200 rounded-2xl p-4 flex items-center justify-between gap-3 text-xs text-rose-800">
          <div className="flex items-center gap-2">
            <AlertCircle className="w-4 h-4 text-rose-600 shrink-0" />
            <span>{errorBanner}</span>
          </div>
          <button
            type="button"
            onClick={clearError}
            className="font-bold text-rose-700 hover:underline cursor-pointer"
          >
            Đóng
          </button>
        </div>
      )}

      {/* Expanded 2-Column Live Chat Workspace (8 cols Messenger + 4 cols Grammar Correction) */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-4 items-start">
        <div className="lg:col-span-8 bg-white rounded-3xl border border-slate-200/90 shadow-xs flex flex-col h-[calc(100vh-9.5rem)] min-h-[540px]">
          <div className="px-5 py-3.5 border-b border-slate-100 flex items-center justify-between gap-2 flex-wrap shrink-0">
            <div className="flex items-center gap-2">
              <div
                className={cn(
                  'w-2.5 h-2.5 rounded-full',
                  isSessionActive ? 'bg-emerald-500 animate-pulse' : 'bg-slate-300'
                )}
              />
              <span className="text-sm font-extrabold text-slate-800">
                Hội thoại Song ngữ Trực tiếp (EN / VI)
              </span>
              <Badge variant={activeConversationId ? 'sky' : 'emerald'}>
                {activeConversationId ? 'Đang mở phiên đã lưu' : 'Phiên mới'}
              </Badge>
            </div>
            <div className="flex items-center gap-3">
              <span className="text-xs text-slate-500">{turns.length} lượt thoại</span>
              {(turns.length > 0 || activeConversationId) && (
                <button
                  type="button"
                  onClick={() => void handleSwitchNewConversation()}
                  className="text-xs font-bold text-slate-600 hover:text-emerald-700 hover:underline flex items-center gap-1 cursor-pointer"
                >
                  <MessageSquarePlus className="w-3.5 h-3.5 text-emerald-600" />
                  Tạo mới
                </button>
              )}
              <button
                type="button"
                onClick={() => setSetupModalOpen(true)}
                className="text-xs font-bold text-emerald-700 hover:underline flex items-center gap-1 cursor-pointer"
              >
                <Settings className="w-3.5 h-3.5" />
                Đổi thiết lập
              </button>
            </div>
          </div>

          <LiveMessengerStream
            turns={turns}
            corrections={corrections}
            onAnalyzeTurn={(turnId, englishText) => {
              void analyzeTurnGrammar(turnId, englishText, true);
            }}
          />

          <div className="p-4 border-t border-slate-100 bg-slate-50/70 rounded-b-3xl flex flex-wrap items-center justify-between gap-4 shrink-0">
            <div className="flex items-center gap-4 text-xs text-slate-600">
              <div className="flex items-center gap-1.5">
                <span className="font-semibold">Mic:</span>
                <div className="w-20 h-2 rounded-full bg-slate-200 overflow-hidden">
                  <div
                    className="h-full bg-emerald-500 transition-all"
                    style={{ width: `${Math.round(micLevel * 100)}%` }}
                  />
                </div>
              </div>
              <div className="flex items-center gap-1.5">
                <span className="font-semibold">AI Voice:</span>
                <div className="w-20 h-2 rounded-full bg-slate-200 overflow-hidden">
                  <div
                    className="h-full bg-amber-500 transition-all"
                    style={{ width: `${Math.round(aiAudioLevel * 100)}%` }}
                  />
                </div>
              </div>
            </div>

            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="md"
                onClick={() => void handleSwitchNewConversation()}
                title="Chuyển sang cuộc hội thoại mới"
              >
                <MessageSquarePlus className="w-4 h-4" />
                Hội thoại mới
              </Button>

              <Button variant="outline" size="md" onClick={() => setSetupModalOpen(true)}>
                <Settings className="w-4 h-4" />
                Thiết lập
              </Button>

              {!isSessionActive ? (
                <Button
                  variant={apiKey.trim() ? 'primary' : 'secondary'}
                  size="md"
                  onClick={handleStartLive}
                >
                  <Mic className="w-4 h-4" />
                  Bắt đầu luyện nói
                </Button>
              ) : (
                <Button variant="danger" size="md" onClick={() => void stopSession()}>
                  <PhoneOff className="w-4 h-4" />
                  Kết thúc phiên ({phase})
                </Button>
              )}
            </div>
          </div>
        </div>

        <div className="lg:col-span-4">
          <GrammarCorrectionPanel
            corrections={corrections}
            isAnalyzing={isAnalyzingGrammar}
            canAnalyzeLatestTurn={Boolean(latestUserTurn)}
            onAnalyzeLatestTurn={() => {
              if (latestUserTurn) {
                void analyzeTurnGrammar(latestUserTurn.turnId, latestUserTurn.englishLine1, true);
              }
            }}
          />
        </div>
      </div>

      {/* Modal 1: Session Setup Dialog */}
      <TutorSetupModal
        isOpen={isSetupModalOpen}
        isSessionActive={isSessionActive}
        onClose={() => setSetupModalOpen(false)}
        onSaveSettings={() => void handleSaveSettingsOnly()}
        onStartSpeaking={() => void handleStartLive()}
      />

      {/* Modal 2: Conversation History Dialog */}
      <ConversationHistoryModal
        isOpen={isHistoryDrawerOpen}
        onClose={() => setHistoryDrawerOpen(false)}
        conversations={historyList}
        activeConversationId={activeConversationId}
        onResumeConversation={handleResumeFromModal}
        onStartNewConversation={() => void handleSwitchNewConversation()}
        onDeleteConversation={async (id) => {
          await deleteConvMutation.mutateAsync(id);
          if (activeConversationId === id) {
            await handleSwitchNewConversation();
          }
        }}
      />
    </div>
  );
};
