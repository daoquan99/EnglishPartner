import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Mic,
  PhoneOff,
  History,
  Settings,
  Wifi,
  AlertCircle,
  MessageSquarePlus,
  Sparkles,
  ArrowRight,
  Lightbulb,
  Volume2,
  Square,
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
import { Dialog } from '@/shared/components/ui/Dialog';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { cn } from '@/shared/lib/cn';

export const LiveTutorPage: React.FC = () => {
  const { t, i18n } = useTranslation();
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
    replayingTurnId,
    startSession,
    stopSession,
    replayTurnAudio,
    analyzeTurnGrammar,
    loadPersistedSessionData,
    resetConversationWorkspace,
    clearError,
  } = useThreeHubLiveSession();

  const [selectedCorrectionModal, setSelectedCorrectionModal] =
    useState<GrammarCorrectionItem | null>(null);

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

  const speakCorrectedSample = (text: string) => {
    if (!('speechSynthesis' in window) || !text) return;
    window.speechSynthesis.cancel();
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.lang = 'en-US';
    utterance.rate = 0.95;
    window.speechSynthesis.speak(utterance);
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
            <img src="/assets/icons/navigation/nav-settings.svg" alt="" className="w-4 h-4 object-contain" />
            {t('tutor.setupModal')}
          </Button>

          <Button
            variant="outline"
            size="sm"
            onClick={() => void handleSwitchNewConversation()}
            title={t('tutor.newConversation')}
            className="border-slate-300 bg-white hover:bg-emerald-50 hover:border-emerald-300 text-slate-800 hover:text-emerald-900 font-semibold"
          >
            <img src="/assets/icons/navigation/nav-conversations.svg" alt="" className="w-4 h-4 object-contain" />
            {t('tutor.newConversation')}
          </Button>

          <Badge variant="emerald">CEFR {cefrLevel}</Badge>
          <Badge variant="sky">{conversationMode}</Badge>
          <Badge variant="violet">Voice: {voiceName}</Badge>
          <Badge variant="amber">Speed: {speechSpeed.toFixed(2)}x</Badge>
          {topic && (
            <span className="hidden md:inline-block text-xs font-semibold text-slate-700 bg-slate-100 px-3 py-1 rounded-full truncate max-w-xs">
              {t('common.conversationTopic')}: {topic}
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
            <img src="/assets/icons/navigation/nav-history.svg" alt="" className="w-3.5 h-3.5 object-contain" />
            {t('tutor.historyModal')} ({historyList.length})
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
            {t('common.close')}
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
                {t('tutor.bilingualLiveStream')}
              </span>
              <Badge variant={activeConversationId ? 'sky' : 'emerald'}>
                {activeConversationId ? t('tutor.activeSavedSession') : t('tutor.freshSession')}
              </Badge>
            </div>
            <div className="flex items-center gap-3">
              <span className="text-xs text-slate-500">
                {turns.length} {t('tutor.turnsCount')}
              </span>
              {(turns.length > 0 || activeConversationId) && (
                <button
                  type="button"
                  onClick={() => void handleSwitchNewConversation()}
                  className="text-xs font-bold text-slate-600 hover:text-emerald-700 hover:underline flex items-center gap-1 cursor-pointer"
                >
                  <img src="/assets/icons/navigation/nav-conversations.svg" alt="" className="w-3.5 h-3.5 object-contain" />
                  {t('tutor.newConversation')}
                </button>
              )}
              <button
                type="button"
                onClick={() => setSetupModalOpen(true)}
                className="text-xs font-bold text-emerald-700 hover:underline flex items-center gap-1 cursor-pointer"
              >
                <img src="/assets/icons/navigation/nav-settings.svg" alt="" className="w-3.5 h-3.5 object-contain" />
                {t('tutor.changeSettings')}
              </button>
            </div>
          </div>

          <LiveMessengerStream
            turns={turns}
            corrections={corrections}
            replayingTurnId={replayingTurnId}
            onReplayTurnAudio={(turn) => {
              void replayTurnAudio(turn);
            }}
            onSelectCorrection={(corr) => {
              setSelectedCorrectionModal(corr);
            }}
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
                title={t('tutor.newConversation')}
              >
                <MessageSquarePlus className="w-4 h-4" />
                {t('tutor.newConversation')}
              </Button>

              <Button variant="outline" size="md" onClick={() => setSetupModalOpen(true)}>
                <Settings className="w-4 h-4" />
                {t('tutor.setupModal')}
              </Button>

              {!isSessionActive ? (
                <Button
                  variant={apiKey.trim() ? 'primary' : 'secondary'}
                  size="md"
                  onClick={handleStartLive}
                >
                  <Mic className="w-4 h-4" />
                  {t('tutor.startSpeaking')}
                </Button>
              ) : (
                <Button variant="danger" size="md" onClick={() => void stopSession()}>
                  <PhoneOff className="w-4 h-4" />
                  {t('tutor.stopSpeaking')} ({phase})
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
            replayingTurnId={replayingTurnId}
            onReplayUserTurnAudio={(turnId, fallbackText) => {
              void replayTurnAudio({
                turnId,
                speaker: 'user',
                englishLine1: fallbackText,
              });
            }}
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
        open={isSetupModalOpen}
        onClose={() => setSetupModalOpen(false)}
        onStartSpeaking={() => void handleStartLive()}
        onApplyAndStart={() => void handleStartLive()}
        onSaveSettings={() => void handleSaveSettingsOnly()}
        onSaveOnly={() => void handleSaveSettingsOnly()}
        isSessionActive={isSessionActive}
      />

      {/* Modal 2: Persisted Conversation History Dialog */}
      <ConversationHistoryModal
        isOpen={isHistoryDrawerOpen}
        open={isHistoryDrawerOpen}
        onClose={() => setHistoryDrawerOpen(false)}
        conversations={historyList}
        activeConversationId={activeConversationId}
        onResumeConversation={handleResumeFromModal}
        onSelectAndResume={handleResumeFromModal}
        onStartNewConversation={() => void handleSwitchNewConversation()}
        onDeleteConversation={(id) => deleteConvMutation.mutate(id)}
      />

      {/* Modal 3: Grammar Correction Detail Dialog when clicking inline badge under User message */}
      <Dialog
        isOpen={Boolean(selectedCorrectionModal)}
        onClose={() => setSelectedCorrectionModal(null)}
        maxWidth="lg"
        icon={
          <img
            src="/assets/icons/navigation/nav-rubrics.svg"
            alt=""
            className="w-5 h-5 object-contain"
          />
        }
        title={t('tutor.grammarModalTitle')}
        description={selectedCorrectionModal?.errorCategory || 'Grammar & Vocabulary'}
        footer={
          <div className="flex items-center justify-end gap-2 w-full">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setSelectedCorrectionModal(null)}
            >
              {t('common.close')}
            </Button>
          </div>
        }
      >
        {selectedCorrectionModal && (
          <div className="space-y-3 text-xs">
            <div className="flex items-start justify-between gap-2 bg-rose-50/70 border border-rose-200 rounded-xl p-3">
              <div className="space-y-1">
                <span className="text-[10px] font-bold uppercase tracking-wider text-rose-600 block">
                  {t('tutor.originalSentence')}
                </span>
                <p className="text-rose-800 font-semibold text-sm leading-relaxed">
                  {selectedCorrectionModal.originalSentence}
                </p>
              </div>
              <button
                type="button"
                onClick={() =>
                  void replayTurnAudio({
                    turnId: selectedCorrectionModal.turnId,
                    speaker: 'user',
                    englishLine1: selectedCorrectionModal.originalSentence,
                  })
                }
                title="Replay user audio"
                className="p-1.5 rounded-lg text-rose-600 hover:bg-rose-100 shrink-0 cursor-pointer"
              >
                {replayingTurnId === selectedCorrectionModal.turnId ? (
                  <Square className="w-4 h-4 fill-current animate-pulse" />
                ) : (
                  <Volume2 className="w-4 h-4" />
                )}
              </button>
            </div>

            <div className="flex items-start justify-between gap-2 bg-emerald-50/80 border border-emerald-200 rounded-xl p-3">
              <div className="space-y-1">
                <span className="text-[10px] font-bold uppercase tracking-wider text-emerald-700 flex items-center gap-1">
                  <ArrowRight className="w-3 h-3" />
                  {t('tutor.correctedSentence')}
                </span>
                <p className="text-emerald-900 font-bold text-sm leading-relaxed">
                  {selectedCorrectionModal.correctedSentence}
                </p>
              </div>
              <button
                type="button"
                onClick={() => speakCorrectedSample(selectedCorrectionModal.correctedSentence)}
                title="Speak corrected sentence"
                className="p-1.5 rounded-lg text-emerald-700 hover:bg-emerald-100 shrink-0 cursor-pointer"
              >
                <Volume2 className="w-4 h-4" />
              </button>
            </div>

            {selectedCorrectionModal.naturalAlternative && (
              <div className="bg-slate-50 border border-slate-200 rounded-xl p-3 leading-relaxed text-slate-700">
                <strong className="text-slate-900">{t('tutor.naturalAlternative')}</strong>{' '}
                {selectedCorrectionModal.naturalAlternative}
              </div>
            )}

            {(selectedCorrectionModal.explanationVi || selectedCorrectionModal.overallTipVi) && (
              <div className="bg-white border border-slate-200 rounded-xl p-3 leading-relaxed text-slate-700">
                <strong className="text-slate-900">{t('tutor.grammarExplanation')}</strong>{' '}
                {selectedCorrectionModal.explanationVi || selectedCorrectionModal.overallTipVi}
              </div>
            )}

            {selectedCorrectionModal.cefrTip && (
              <div className="flex items-start gap-2 bg-amber-50 border border-amber-200 rounded-xl p-3 text-amber-900">
                <Lightbulb className="w-4 h-4 text-amber-600 shrink-0 mt-0.5" />
                <span className="font-medium leading-relaxed">{selectedCorrectionModal.cefrTip}</span>
              </div>
            )}
          </div>
        )}
      </Dialog>
    </div>
  );
};
