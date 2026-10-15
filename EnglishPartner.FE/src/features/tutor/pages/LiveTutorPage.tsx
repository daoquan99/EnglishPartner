import React from 'react';
import {
  Mic,
  PhoneOff,
  History,
  SlidersHorizontal,
  Sparkles,
  Wifi,
  AlertCircle,
  Trash2,
  FolderOpen,
} from 'lucide-react';
import { TutorSetupSidebar } from '../components/TutorSetupSidebar';
import { LiveMessengerStream } from '../components/LiveMessengerStream';
import { GrammarCorrectionPanel } from '../components/GrammarCorrectionPanel';
import { useTutorSessionStore } from '../store/tutorSessionStore';
import { useThreeHubLiveSession } from '../hooks/useThreeHubLiveSession';
import {
  useConversationsListQuery,
  useCreateConversationMutation,
  useDeleteConversationMutation,
} from '../hooks/useConversations';
import { conversationsApi } from '../api/conversationsApi';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { cn } from '@/shared/lib/cn';

export const LiveTutorPage: React.FC = () => {
  const {
    cefrLevel,
    conversationMode,
    voiceName,
    speechSpeed,
    topic,
    customSystemPrompt,
    extendedThinking,
    activeConversationId,
    isSetupPanelOpen,
    isHistoryDrawerOpen,
    setSetupPanelOpen,
    setHistoryDrawerOpen,
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
  const deleteConvMutation = useDeleteConversationMutation();

  const isSessionActive = phase !== 'idle' && phase !== 'error';

  const handleStartLive = async () => {
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

  const handleLoadHistoryConversation = async (id: string) => {
    try {
      const detail = await conversationsApi.getConversationDetail(id);
      if (!detail) return;
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
      setHistoryDrawerOpen(false);
    } catch {
      // ignore
    }
  };

  return (
    <div className="space-y-4">
      {/* Top Session Status Bar */}
      <div className="bg-white rounded-3xl border border-slate-200/90 px-5 py-3.5 flex flex-wrap items-center justify-between gap-3 shadow-xs">
        <div className="flex items-center gap-2.5 flex-wrap">
          <Badge variant="emerald">CEFR {cefrLevel}</Badge>
          <Badge variant="sky">{conversationMode}</Badge>
          <Badge variant="violet">Voice: {voiceName}</Badge>
          <Badge variant="amber">Speed: {speechSpeed.toFixed(2)}x</Badge>
          {topic && (
            <span className="text-xs font-semibold text-slate-700 bg-slate-100 px-3 py-1 rounded-full truncate max-w-xs">
              Chủ đề: {topic}
            </span>
          )}
        </div>

        <div className="flex items-center gap-2">
          {/* 3 SignalR Hub Badges */}
          <div className="hidden md:flex items-center gap-1.5 text-[11px] font-semibold text-slate-600 bg-slate-50 px-3 py-1.5 rounded-xl border border-slate-200">
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
            onClick={() => setHistoryDrawerOpen(!isHistoryDrawerOpen)}
          >
            <History className="w-3.5 h-3.5" />
            Lịch sử ({historyList.length})
          </Button>

          <Button
            variant="outline"
            size="sm"
            onClick={() => setSetupPanelOpen(!isSetupPanelOpen)}
            className="lg:hidden"
          >
            <SlidersHorizontal className="w-3.5 h-3.5" />
            Cấu hình
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

      {/* History Drawer */}
      {isHistoryDrawerOpen && (
        <div className="bg-white rounded-3xl border border-slate-200 p-5 shadow-md space-y-3">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-extrabold text-slate-900 flex items-center gap-2">
              <FolderOpen className="w-4 h-4 text-emerald-600" />
              Lịch sử Cuộc trò chuyện đã lưu trong Database ({historyList.length})
            </h3>
            <button
              type="button"
              onClick={() => setHistoryDrawerOpen(false)}
              className="text-xs font-semibold text-slate-500 hover:text-slate-800 cursor-pointer"
            >
              Đóng
            </button>
          </div>
          {historyList.length === 0 ? (
            <p className="text-xs text-slate-500">Chưa có cuộc hội thoại nào được lưu.</p>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-2.5 max-h-60 overflow-y-auto">
              {historyList.map((conv) => (
                <div
                  key={conv.id}
                  className="p-3 rounded-2xl border border-slate-200 hover:border-emerald-400 bg-slate-50/60 flex items-start justify-between gap-2"
                >
                  <button
                    type="button"
                    onClick={() => handleLoadHistoryConversation(conv.id)}
                    className="text-left flex-1 min-w-0 cursor-pointer"
                  >
                    <div className="text-xs font-bold text-slate-800 truncate">{conv.title}</div>
                    <div className="text-[11px] text-slate-500 mt-0.5">
                      {conv.cefrLevel} · {conv.voiceName} · {conv.messageCount} câu
                    </div>
                  </button>
                  <button
                    type="button"
                    onClick={() => deleteConvMutation.mutate(conv.id)}
                    className="p-1.5 text-slate-400 hover:text-rose-600 rounded-lg cursor-pointer"
                    title="Xóa cuộc hội thoại"
                  >
                    <Trash2 className="w-3.5 h-3.5" />
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Main 3-Column Workspace Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-4 items-start">
        {/* Left Column: Setup Sidebar */}
        <div className={cn('lg:col-span-4', isSetupPanelOpen ? 'block' : 'hidden lg:block')}>
          <TutorSetupSidebar isSessionActive={isSessionActive} />
        </div>

        {/* Center Column: Bilingual Live Messenger & Call Controls */}
        <div className="lg:col-span-5 bg-white rounded-3xl border border-slate-200/90 shadow-xs flex flex-col h-[740px]">
          <div className="px-5 py-3.5 border-b border-slate-100 flex items-center justify-between">
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
            <span className="text-xs text-slate-500">{turns.length} lượt thoại</span>
          </div>

          <LiveMessengerStream turns={turns} corrections={corrections} />

          {/* Bottom Audio Visualizer & Start/Stop Call Bar */}
          <div className="p-4 border-t border-slate-100 bg-slate-50/70 rounded-b-3xl flex items-center justify-between gap-4">
            <div className="flex items-center gap-3 text-xs text-slate-600">
              <div className="flex items-center gap-1.5">
                <span className="font-semibold">Mic:</span>
                <div className="w-16 h-2 rounded-full bg-slate-200 overflow-hidden">
                  <div
                    className="h-full bg-emerald-500 transition-all"
                    style={{ width: `${Math.round(micLevel * 100)}%` }}
                  />
                </div>
              </div>
              <div className="flex items-center gap-1.5">
                <span className="font-semibold">AI Voice:</span>
                <div className="w-16 h-2 rounded-full bg-slate-200 overflow-hidden">
                  <div
                    className="h-full bg-amber-500 transition-all"
                    style={{ width: `${Math.round(aiAudioLevel * 100)}%` }}
                  />
                </div>
              </div>
            </div>

            {!isSessionActive ? (
              <Button variant="primary" size="md" onClick={handleStartLive}>
                <Mic className="w-4 h-4" />
                Bắt đầu Luyện Nói Live
              </Button>
            ) : (
              <Button variant="danger" size="md" onClick={() => void stopSession()}>
                <PhoneOff className="w-4 h-4" />
                Kết thúc phiên ({phase})
              </Button>
            )}
          </div>
        </div>

        {/* Right Column: Realtime Grammar & Vocabulary Corrections */}
        <div className="lg:col-span-3">
          <GrammarCorrectionPanel corrections={corrections} />
        </div>
      </div>
    </div>
  );
};
