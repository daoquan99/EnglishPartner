import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Mic,
  PhoneOff,
  History,
  Settings,
  AlertCircle,
  MessageSquarePlus,
  Sparkles,
  ArrowRight,
  Lightbulb,
  Volume2,
  Square,
  Loader2,
  Wand2,
} from 'lucide-react';
import { TutorSetupSidebar as TutorSetupModal } from '../components/TutorSetupSidebar';
import { TutorSessionHeader } from '../components/TutorSessionHeader';
import { ConversationHistoryModal } from '../components/ConversationHistoryModal';
import { LiveMessengerStream } from '../components/LiveMessengerStream';
import {
  GrammarCorrectionPanel,
  normalizeSentence,
} from '../components/GrammarCorrectionPanel';
import { useFeedbackAudio } from '../hooks/useFeedbackAudio';
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
    requestFeedbackAudio,
    loadPersistedSessionData,
    resetConversationWorkspace,
    clearError,
  } = useThreeHubLiveSession();

  const {
    loadingKey: modalAudioLoadingKey,
    playFeedbackAudio: playModalFeedbackAudio,
  } = useFeedbackAudio({ onRequestFeedbackAudio: requestFeedbackAudio });

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

  return (
    <div className="space-y-4">
      {/* Top Action & Configuration Bar */}
      <TutorSessionHeader
        cefrLevel={cefrLevel}
        conversationMode={conversationMode}
        voiceName={voiceName}
        speechSpeed={speechSpeed}
        topic={topic}
        hubStates={hubStates}
        historyCount={historyList.length}
        onOpenSetup={() => setSetupModalOpen(true)}
        onNewConversation={() => void handleSwitchNewConversation()}
        onOpenHistory={() => setHistoryDrawerOpen(true)}
      />

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
          <div className="px-4 sm:px-5 py-3.5 border-b border-slate-100 flex flex-col sm:flex-row sm:flex-wrap sm:items-center justify-between gap-2.5 shrink-0">
            <div className="flex items-center justify-between sm:justify-start gap-2 w-full sm:w-auto min-w-0">
              <div className="flex items-center gap-2 min-w-0">
                <div
                  className={cn(
                    'w-2.5 h-2.5 rounded-full shrink-0',
                    isSessionActive ? 'bg-emerald-500 animate-pulse' : 'bg-slate-300'
                  )}
                />
                <span className="text-sm font-extrabold text-slate-800 min-w-0">
                  {t('tutor.bilingualLiveStream')}
                </span>
              </div>
              <Badge variant={activeConversationId ? 'sky' : 'emerald'} className="whitespace-nowrap shrink-0">
                {activeConversationId ? t('tutor.activeSavedSession') : t('tutor.freshSession')}
              </Badge>
            </div>
            <div className="flex items-center justify-between sm:justify-end gap-3 w-full sm:w-auto text-xs">
              <span className="text-xs text-slate-500 whitespace-nowrap">
                {turns.length} {t('tutor.turnsCount')}
              </span>
              <div className="flex items-center gap-3 shrink-0">
                {(turns.length > 0 || activeConversationId) && (
                  <button
                    type="button"
                    onClick={() => void handleSwitchNewConversation()}
                    className="text-xs font-bold text-slate-600 hover:text-emerald-700 hover:underline flex items-center gap-1 cursor-pointer whitespace-nowrap shrink-0"
                  >
                    <img src="/assets/icons/navigation/nav-conversations.svg" alt="" className="w-3.5 h-3.5 object-contain shrink-0" />
                    {t('tutor.newConversation')}
                  </button>
                )}
                <button
                  type="button"
                  onClick={() => setSetupModalOpen(true)}
                  className="text-xs font-bold text-emerald-700 hover:underline flex items-center gap-1 cursor-pointer whitespace-nowrap shrink-0"
                >
                  <img src="/assets/icons/navigation/nav-settings.svg" alt="" className="w-3.5 h-3.5 object-contain shrink-0" />
                  {t('tutor.changeSettings')}
                </button>
              </div>
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

          <div className="px-4 sm:px-5 py-3 sm:py-4 border-t border-slate-100 bg-slate-50/70 rounded-b-3xl flex flex-col xl:flex-row items-stretch xl:items-center justify-between gap-3 shrink-0">
            <div className="flex items-center gap-4 text-xs text-slate-600 flex-wrap">
              <div className="flex items-center gap-1.5">
                <span className="font-semibold whitespace-nowrap">Mic:</span>
                <div className="w-20 h-2 rounded-full bg-slate-200 overflow-hidden">
                  <div
                    className="h-full bg-emerald-500 transition-all"
                    style={{ width: `${Math.round(micLevel * 100)}%` }}
                  />
                </div>
              </div>
              <div className="flex items-center gap-1.5">
                <span className="font-semibold whitespace-nowrap">AI Voice:</span>
                <div className="w-20 h-2 rounded-full bg-slate-200 overflow-hidden">
                  <div
                    className="h-full bg-amber-500 transition-all"
                    style={{ width: `${Math.round(aiAudioLevel * 100)}%` }}
                  />
                </div>
              </div>
            </div>

            {/* Mobile: secondary actions collapse to icon buttons (also available in the header) so the bar stays one row. */}
            <div className="flex items-center gap-2 w-full xl:w-auto sm:flex-wrap sm:justify-end">
              <Button
                variant="outline"
                size="md"
                onClick={() => void handleSwitchNewConversation()}
                title={t('tutor.newConversation')}
                aria-label={t('tutor.newConversation')}
                className="shrink-0 justify-center whitespace-nowrap min-h-11 min-w-11 px-3 sm:px-4"
              >
                <MessageSquarePlus className="w-4 h-4 shrink-0" />
                <span className="hidden sm:inline">{t('tutor.newConversation')}</span>
              </Button>

              <Button
                variant="outline"
                size="md"
                onClick={() => setSetupModalOpen(true)}
                title={t('tutor.setupModal')}
                aria-label={t('tutor.setupModal')}
                className="shrink-0 justify-center whitespace-nowrap min-h-11 min-w-11 px-3 sm:px-4"
              >
                <Settings className="w-4 h-4 shrink-0" />
                <span className="hidden sm:inline">{t('tutor.setupModal')}</span>
              </Button>

              {!isSessionActive ? (
                <Button
                  variant={apiKey.trim() ? 'primary' : 'secondary'}
                  size="md"
                  onClick={handleStartLive}
                  className="flex-1 sm:flex-none min-w-0 min-h-11 justify-center whitespace-nowrap"
                >
                  <Mic className="w-4 h-4 shrink-0" />
                  <span className="truncate">{t('tutor.startSpeaking')}</span>
                </Button>
              ) : (
                <Button
                  variant="danger"
                  size="md"
                  onClick={() => void stopSession()}
                  className="flex-1 sm:flex-none min-w-0 min-h-11 justify-center whitespace-nowrap"
                >
                  <PhoneOff className="w-4 h-4 shrink-0" />
                  <span className="truncate">{t('tutor.stopSpeaking')} ({phase})</span>
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
            onRequestFeedbackAudio={requestFeedbackAudio}
            onReanalyzeTurn={(turnId, originalSentence) => {
              void analyzeTurnGrammar(turnId, originalSentence, true);
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
        {selectedCorrectionModal && (() => {
          const normOriginal = normalizeSentence(selectedCorrectionModal.originalSentence);
          const normCorrected = normalizeSentence(selectedCorrectionModal.correctedSentence);
          const improvedText = (
            selectedCorrectionModal.improvedText ||
            selectedCorrectionModal.naturalAlternative ||
            ''
          ).trim();
          const normImproved = normalizeSentence(improvedText);

          const hasCorrected =
            Boolean(selectedCorrectionModal.correctedSentence?.trim()) &&
            normCorrected !== normOriginal;
          const hasImproved =
            Boolean(improvedText) &&
            normImproved !== normOriginal &&
            normImproved !== normCorrected;

          return (
            <div className="space-y-3 text-xs">
              <div className="flex items-center justify-between gap-2">
                <div className="flex items-center gap-1.5 flex-wrap">
                  {selectedCorrectionModal.status === 'has_errors' && (
                    <Badge variant="rose">{t('tutor.statusHasErrors', 'Cần sửa')}</Badge>
                  )}
                  {selectedCorrectionModal.status === 'can_improve' && (
                    <Badge variant="sky">{t('tutor.statusCanImprove', 'Có thể cải thiện')}</Badge>
                  )}
                  {selectedCorrectionModal.status === 'already_natural' && (
                    <Badge variant="emerald">{t('tutor.statusAlreadyNatural', 'Đã tự nhiên')}</Badge>
                  )}
                  {selectedCorrectionModal.status === 'analysis_failed' && (
                    <Badge variant="amber">{t('tutor.statusAnalysisFailed', 'Phân tích lỗi')}</Badge>
                  )}
                  {!selectedCorrectionModal.status && (
                    <Badge variant="amber">{selectedCorrectionModal.errorCategory || 'Grammar & Vocabulary'}</Badge>
                  )}
                  {selectedCorrectionModal.errorCategory && selectedCorrectionModal.status && (
                    <span className="text-[10px] text-slate-500 font-medium">
                      ({selectedCorrectionModal.errorCategory})
                    </span>
                  )}
                </div>
              </div>

              {selectedCorrectionModal.status === 'analysis_failed' && (
                <div className="flex items-center justify-between gap-2 p-2.5 rounded-xl bg-amber-100/70 border border-amber-300 text-amber-900">
                  <div className="flex items-center gap-1.5 text-xs font-semibold">
                    <AlertCircle className="w-4 h-4 text-amber-700 shrink-0" />
                    <span>{t('tutor.statusAnalysisFailed', 'Phân tích lỗi')}</span>
                  </div>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => {
                      void analyzeTurnGrammar(
                        selectedCorrectionModal.turnId,
                        selectedCorrectionModal.originalSentence,
                        true
                      );
                      setSelectedCorrectionModal(null);
                    }}
                    className="text-[11px] font-bold text-amber-900 border-amber-300 bg-white hover:bg-amber-50 h-7 px-2.5"
                  >
                    <Wand2 className="w-3 h-3 text-amber-700" />
                    {t('tutor.reanalyzeAction', 'Phân tích lại')}
                  </Button>
                </div>
              )}

              {/* Original Utterance */}
              <div className="flex items-start justify-between gap-2 bg-rose-50/70 border border-rose-200 rounded-xl p-3">
                <div className="space-y-1">
                  <span className="text-[10px] font-bold uppercase tracking-wider text-rose-600 block">
                    {t('tutor.originalSentence')}
                  </span>
                  <p
                    className={
                      !hasCorrected
                        ? 'text-slate-800 font-semibold text-sm leading-relaxed'
                        : 'text-rose-800 line-through font-semibold text-sm leading-relaxed'
                    }
                  >
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

              {/* Corrected Sentence */}
              {hasCorrected && (
                <div className="flex flex-col gap-2 bg-emerald-50/80 border border-emerald-200 rounded-xl p-3">
                  <div className="flex items-start justify-between gap-2">
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
                      disabled={
                        modalAudioLoadingKey ===
                        `${selectedCorrectionModal.id || selectedCorrectionModal.correctedSentence}_corrected`
                      }
                      onClick={() =>
                        void playModalFeedbackAudio(
                          selectedCorrectionModal.id,
                          'corrected',
                          selectedCorrectionModal.correctedSentence
                        )
                      }
                      title="Speak corrected sentence"
                      className="p-1.5 rounded-lg text-emerald-700 hover:bg-emerald-100 shrink-0 cursor-pointer disabled:opacity-50"
                    >
                      {modalAudioLoadingKey ===
                      `${selectedCorrectionModal.id || selectedCorrectionModal.correctedSentence}_corrected` ? (
                        <Loader2 className="w-4 h-4 animate-spin" />
                      ) : (
                        <Volume2 className="w-4 h-4" />
                      )}
                    </button>
                  </div>

                  {selectedCorrectionModal.issues && selectedCorrectionModal.issues.length > 0 && (
                    <div className="mt-1 space-y-1.5 pt-2 border-t border-emerald-200/70">
                      <span className="text-[10px] font-bold text-emerald-800 uppercase tracking-wider block">
                        {t('tutor.issuesTitle', 'Chi tiết cần sửa:')}
                      </span>
                      <div className="space-y-1">
                        {selectedCorrectionModal.issues.map((issue, idx) => (
                          <div
                            key={idx}
                            className="flex flex-wrap items-center gap-1.5 text-[11px] bg-white/80 px-2 py-1 rounded-lg border border-emerald-100"
                          >
                            <span className="line-through text-rose-600 font-medium">
                              {issue.original}
                            </span>
                            <ArrowRight className="w-3 h-3 text-slate-400 shrink-0" />
                            <span className="text-emerald-700 font-bold">{issue.fix}</span>
                            <span className="text-slate-500 text-[10px]">
                              ({issue.category}
                              {issue.explanationVi ? `: ${issue.explanationVi}` : ''})
                            </span>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              )}

              {/* Improved Sentence */}
              {hasImproved && (
                <div className="flex items-start justify-between gap-2 bg-sky-50/80 border border-sky-200 rounded-xl p-3">
                  <div className="space-y-1">
                    <span className="text-[10px] font-bold uppercase tracking-wider text-sky-700 flex items-center gap-1">
                      <Sparkles className="w-3 h-3" />
                      {t('tutor.improvedSentence', 'Câu tự nhiên hơn')}
                    </span>
                    <p className="text-sky-950 font-bold text-sm leading-relaxed">{improvedText}</p>
                  </div>
                  <button
                    type="button"
                    disabled={
                      modalAudioLoadingKey ===
                      `${selectedCorrectionModal.id || improvedText}_improved`
                    }
                    onClick={() =>
                      void playModalFeedbackAudio(
                        selectedCorrectionModal.id,
                        'improved',
                        improvedText
                      )
                    }
                    title="Speak improved sentence"
                    className="p-1.5 rounded-lg text-sky-700 hover:bg-sky-100 shrink-0 cursor-pointer disabled:opacity-50"
                  >
                    {modalAudioLoadingKey ===
                    `${selectedCorrectionModal.id || improvedText}_improved` ? (
                      <Loader2 className="w-4 h-4 animate-spin" />
                    ) : (
                      <Volume2 className="w-4 h-4" />
                    )}
                  </button>
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
          );
        })()}
      </Dialog>
    </div>
  );
};
