import React from 'react';
import {
  Mic,
  PhoneOff,
  History,
  Settings,
  Wifi,
  AlertCircle,
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
import type { ConversationDetailDto } from '../types/tutor.types';
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
    errorBanner,
    micLevel,
    aiAudioLevel,
    startSession,
    stopSession,
    loadPersistedSessionData,
    clearError,
  } = useThreeHubLiveSession();

  const { data: historyList = [] } = useConversationsListQuery();
  const createConvMutation = useCreateConversationMutation();
  const updateSettingsMutation = useUpdateConversationSettingsMutation();
  const deleteConvMutation = useDeleteConversationMutation();

  const isSessionActive = phase !== 'idle' && phase !== 'error';

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
    } catch {
      // Proceed with local UUID if BE DB is offline
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

    loadPersistedSessionData(
      detail.messages.map((m) => ({
        turnId: m.turnId,
        speaker: m.speaker,
        englishLine1: m.transcriptLine1,
        vietnameseLine2: m.translationLine2,
        isFinal: true,
        timestamp: new Date(m.createdAtUtc).getTime(),
      })),
      detail.corrections.map((c) => {
        let parsedErrors = [];
        try {
          parsedErrors = JSON.parse(c.errorsJson || '[]');
        } catch {
          parsedErrors = [];
        }
        return {
          turnId: c.turnId,
          hasErrors: true,
          originalSentence: c.originalSentence,
          correctedSentence: c.correctedSentence,
          naturalAlternative: c.naturalAlternative,
          overallTipVi: c.overallTipVi,
          errors: parsedErrors,
          timestamp: new Date(c.createdAtUtc).getTime(),
        };
      })
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

          {/* Button opens Centered Dialog/Modal instead of inline drawer */}
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
          <div className="px-5 py-3.5 border-b border-slate-100 flex items-center justify-between shrink-0">
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
            </div>
            <div className="flex items-center gap-3">
              <span className="text-xs text-slate-500">{turns.length} lượt thoại</span>
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

          <LiveMessengerStream turns={turns} corrections={corrections} />

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
          <GrammarCorrectionPanel corrections={corrections} />
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

      {/* Modal 2: Conversation History Dialog (with View Detail, Resume, and Delete ConfirmDialog) */}
      <ConversationHistoryModal
        isOpen={isHistoryDrawerOpen}
        onClose={() => setHistoryDrawerOpen(false)}
        conversations={historyList}
        onResumeConversation={handleResumeFromModal}
        onDeleteConversation={async (id) => {
          await deleteConvMutation.mutateAsync(id);
        }}
      />
    </div>
  );
};
