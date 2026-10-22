import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { FastForward, Loader2, Settings, Square, Volume2 } from 'lucide-react';
import { FallingGameCanvas } from '../components/FallingGameCanvas';
import { GameInputBar } from '../components/GameInputBar';
import { RoundResultOverlay } from '../components/RoundResultOverlay';
import { SessionSummaryModal } from '../components/SessionSummaryModal';
import { AiSpeechSettingsDialog } from '@/features/speech';
import { useTutorSessionStore } from '@/features/tutor';
import {
  useAbandonFallingWordSession,
  useFallingWordSession,
  useSkipFallingWordRound,
  useStartNextRound,
  useSubmitFallingWordAnswer,
  useSynthesizeFallingWordAudio,
  useTimeoutFallingWordRound,
} from '../hooks/useFallingWords';
import type { TerminalFallingWordRoundDto } from '../types/fallingWords.types';

export const FallingWordsPlayPage: React.FC = () => {
  const { id: sessionId } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [inputValue, setInputValue] = useState('');
  const [activeResult, setActiveResult] = useState<TerminalFallingWordRoundDto | null>(null);
  const [isAbandonConfirmOpen, setIsAbandonConfirmOpen] = useState(false);
  const [isSpeechSettingsOpen, setIsSpeechSettingsOpen] = useState(false);
  const [isAudioBusy, setIsAudioBusy] = useState(false);
  const [isAudioPlaying, setIsAudioPlaying] = useState(false);
  const [audioError, setAudioError] = useState<string | null>(null);

  const audioPlayerRef = useRef<HTMLAudioElement | null>(null);
  const audioTokenRef = useRef(0);

  const apiKey = useTutorSessionStore((s) => s.apiKey);
  const voiceName = useTutorSessionStore((s) => s.voiceName);
  const speechSpeed = useTutorSessionStore((s) => s.speechSpeed);
  const accent = useTutorSessionStore((s) => s.ttsAccent);
  const modelCode = useTutorSessionStore((s) => s.ttsModelCode);

  const {
    data: session,
    isLoading: isLoadingSession,
    error: sessionError,
    refetch,
  } = useFallingWordSession(sessionId);

  const startNextRoundMutation = useStartNextRound();
  const submitAnswerMutation = useSubmitFallingWordAnswer();
  const timeoutRoundMutation = useTimeoutFallingWordRound();
  const skipRoundMutation = useSkipFallingWordRound();
  const synthesizeAudioMutation = useSynthesizeFallingWordAudio();
  const abandonSessionMutation = useAbandonFallingWordSession();

  useEffect(() => {
    return () => {
      audioTokenRef.current++;
      if (audioPlayerRef.current) {
        audioPlayerRef.current.pause();
      }
    };
  }, []);

  const isTerminalSession =
    session?.status === 'Completed' || session?.status === 'Abandoned';

  const handleStartNext = useCallback(async () => {
    if (!sessionId) return;
    try {
      if (audioPlayerRef.current) {
        audioPlayerRef.current.pause();
        setIsAudioPlaying(false);
      }
      setActiveResult(null);
      setInputValue('');
      setAudioError(null);
      await startNextRoundMutation.mutateAsync({ sessionId });
      await refetch();
    } catch (err) {
      console.error('Failed to start next round:', err);
    }
  }, [sessionId, startNextRoundMutation, refetch]);

  const handleSubmitAnswer = useCallback(async () => {
    if (!sessionId || !session?.activeRound || !inputValue.trim()) return;
    const currentRoundId = session.activeRound.roundId;

    try {
      if (audioPlayerRef.current) {
        audioPlayerRef.current.pause();
        setIsAudioPlaying(false);
      }
      const response = await submitAnswerMutation.mutateAsync({
        sessionId,
        roundId: currentRoundId,
        answerText: inputValue.trim(),
      });
      setActiveResult(response.roundResult);
      setInputValue('');
      setAudioError(null);
    } catch (err) {
      console.error('Failed to submit answer:', err);
    }
  }, [sessionId, session?.activeRound, inputValue, submitAnswerMutation]);

  const handleSkipRound = useCallback(async () => {
    if (!sessionId || !session?.activeRound) return;
    if (skipRoundMutation.isPending || submitAnswerMutation.isPending) return;

    const currentRoundId = session.activeRound.roundId;
    try {
      if (audioPlayerRef.current) {
        audioPlayerRef.current.pause();
        setIsAudioPlaying(false);
      }
      const terminalDto = await skipRoundMutation.mutateAsync({
        sessionId,
        roundId: currentRoundId,
      });
      setActiveResult(terminalDto);
      setInputValue('');
      setAudioError(null);
    } catch (err) {
      console.error('Failed to skip round:', err);
    }
  }, [sessionId, session?.activeRound, skipRoundMutation, submitAnswerMutation]);

  const handlePlayAudioHint = useCallback(async () => {
    if (!sessionId || !session?.activeRound) return;
    const currentRoundId = session.activeRound.roundId;

    if (isAudioPlaying && audioPlayerRef.current) {
      audioPlayerRef.current.pause();
      setIsAudioPlaying(false);
      return;
    }

    if (isAudioBusy) return;

    const token = ++audioTokenRef.current;
    setIsAudioBusy(true);
    setAudioError(null);

    try {
      const res = await synthesizeAudioMutation.mutateAsync({
        sessionId,
        roundId: currentRoundId,
        payload: {
          apiKey: apiKey?.trim() || undefined,
          voiceName,
          speechSpeed,
          accent,
          modelCode,
        },
      });

      if (token !== audioTokenRef.current) return;

      if (audioPlayerRef.current) {
        audioPlayerRef.current.pause();
      }

      const player = new Audio(`data:${res.mimeType || 'audio/wav'};base64,${res.audioBase64}`);
      audioPlayerRef.current = player;

      player.onended = () => {
        if (token === audioTokenRef.current) {
          setIsAudioPlaying(false);
        }
      };

      player.onerror = () => {
        if (token === audioTokenRef.current) {
          setIsAudioPlaying(false);
          setAudioError('Không phát được âm thanh gợi ý.');
        }
      };

      await player.play();
      if (token === audioTokenRef.current) {
        setIsAudioPlaying(true);
      }
    } catch (err: any) {
      if (token !== audioTokenRef.current) return;
      const msg =
        err?.response?.data?.error ||
        err?.response?.data?.detail ||
        err?.message ||
        'Không tạo được âm thanh phát âm.';
      setAudioError(msg);
      if (!apiKey && /API Key/i.test(msg)) {
        setIsSpeechSettingsOpen(true);
      }
    } finally {
      if (token === audioTokenRef.current) {
        setIsAudioBusy(false);
      }
    }
  }, [
    sessionId,
    session?.activeRound,
    isAudioPlaying,
    isAudioBusy,
    synthesizeAudioMutation,
    apiKey,
    voiceName,
    speechSpeed,
    accent,
    modelCode,
  ]);

  const handlePlayAudioResult = useCallback(async () => {
    if (!sessionId || !activeResult) return;
    const roundId = activeResult.roundId;

    if (isAudioPlaying && audioPlayerRef.current) {
      audioPlayerRef.current.pause();
      setIsAudioPlaying(false);
      return;
    }

    const token = ++audioTokenRef.current;
    setIsAudioBusy(true);

    try {
      const res = await synthesizeAudioMutation.mutateAsync({
        sessionId,
        roundId,
        payload: {
          apiKey: apiKey?.trim() || undefined,
          voiceName,
          speechSpeed,
          accent,
          modelCode,
        },
      });

      if (token !== audioTokenRef.current) return;

      const player = new Audio(`data:${res.mimeType || 'audio/wav'};base64,${res.audioBase64}`);
      audioPlayerRef.current = player;
      player.onended = () => {
        if (token === audioTokenRef.current) setIsAudioPlaying(false);
      };
      await player.play();
      if (token === audioTokenRef.current) setIsAudioPlaying(true);
    } catch (err) {
      console.error('Failed to play audio in result:', err);
    } finally {
      if (token === audioTokenRef.current) setIsAudioBusy(false);
    }
  }, [
    sessionId,
    activeResult,
    isAudioPlaying,
    synthesizeAudioMutation,
    apiKey,
    voiceName,
    speechSpeed,
    accent,
    modelCode,
  ]);

  const handleDeadlineReached = useCallback(async () => {
    if (!sessionId || !session?.activeRound) return;
    const currentRoundId = session.activeRound.roundId;

    try {
      if (audioPlayerRef.current) {
        audioPlayerRef.current.pause();
        setIsAudioPlaying(false);
      }
      const terminalDto = await timeoutRoundMutation.mutateAsync({
        sessionId,
        roundId: currentRoundId,
      });
      setActiveResult(terminalDto);
      setInputValue('');
      setAudioError(null);
    } catch (err) {
      console.error('Failed to timeout round:', err);
    }
  }, [sessionId, session?.activeRound?.roundId, timeoutRoundMutation]);

  const handleAbandon = useCallback(async () => {
    if (!sessionId) return;
    try {
      if (audioPlayerRef.current) {
        audioPlayerRef.current.pause();
        setIsAudioPlaying(false);
      }
      await abandonSessionMutation.mutateAsync({ sessionId });
      setIsAbandonConfirmOpen(false);
      await refetch();
    } catch (err) {
      console.error('Failed to abandon session:', err);
    }
  }, [sessionId, abandonSessionMutation, refetch]);

  // Global Hotkeys for Play Page:
  // Ctrl+Space -> Audio Hint
  // Ctrl+K -> Skip Word
  // Alt+S -> Speech Settings Dialog
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Speech settings dialog can always be opened
      if (e.altKey && (e.key === 's' || e.key === 'S')) {
        e.preventDefault();
        setIsSpeechSettingsOpen((prev) => !prev);
        return;
      }

      if (!session?.activeRound || activeResult || isTerminalSession || isAbandonConfirmOpen) {
        return;
      }

      // 1. Audio Hint Hotkey: Ctrl + Space (or Cmd + Space on macOS)
      if (
        ((e.ctrlKey || e.metaKey) && (e.code === 'Space' || e.key === ' ')) ||
        (e.altKey && (e.key === 'p' || e.key === 'P'))
      ) {
        e.preventDefault();
        e.stopPropagation();
        void handlePlayAudioHint();
        return;
      }

      // 2. Skip Word Hotkey: Ctrl + K (or Cmd + K on macOS)
      if (
        ((e.ctrlKey || e.metaKey) && (e.key === 'k' || e.key === 'K')) ||
        (e.altKey && (e.key === 'k' || e.key === 'K'))
      ) {
        e.preventDefault();
        e.stopPropagation();
        void handleSkipRound();
        return;
      }
    };

    // Use capture phase so Ctrl+K is caught before Chrome's default address bar focus
    window.addEventListener('keydown', handleKeyDown, true);
    return () => window.removeEventListener('keydown', handleKeyDown, true);
  }, [
    session?.activeRound,
    activeResult,
    isTerminalSession,
    isAbandonConfirmOpen,
    handlePlayAudioHint,
    handleSkipRound,
  ]);

  if (isLoadingSession) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[60vh] gap-3">
        <div className="w-10 h-10 border-4 border-emerald-600 border-t-transparent rounded-full animate-spin" />
        <span className="text-slate-500 font-medium text-sm">Đang tải phiên chơi...</span>
      </div>
    );
  }

  if (sessionError || !session) {
    return (
      <div className="max-w-md mx-auto my-12 p-6 bg-white rounded-3xl border border-slate-200 text-center flex flex-col gap-4 shadow-sm">
        <div className="text-3xl">⚠️</div>
        <h2 className="text-xl font-bold text-slate-800">Không tìm thấy ván chơi</h2>
        <p className="text-sm text-slate-500">
          Phiên chơi này có thể không tồn tại hoặc bạn không có quyền truy cập.
        </p>
        <button
          type="button"
          onClick={() => navigate('/student/vocabulary-games/falling-words')}
          className="py-3 px-5 rounded-xl bg-emerald-600 text-white font-bold text-sm shadow hover:bg-emerald-700 transition"
        >
          Về trang thiết lập
        </button>
      </div>
    );
  }

  return (
    <div className="max-w-3xl mx-auto px-4 py-6 flex flex-col gap-5 min-h-[calc(100vh-5rem)]">
      {/* Play Page Header Bar */}
      <div className="flex items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={() => setIsAbandonConfirmOpen(true)}
            className="text-xs font-semibold text-slate-500 hover:text-slate-800 transition py-1.5 px-3 rounded-lg border border-slate-200 bg-white"
          >
            ← Dừng ván
          </button>
          <span className="text-xs font-medium text-slate-400">
            {session.source === 'DailyVocabulary' ? 'Daily Vocabulary' : 'Kho từ vựng'}
            {session.cefrLevel ? ` • ${session.cefrLevel}` : ''}
          </span>
        </div>

        {/* Stats Summary Pill & Speech Settings Button */}
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => setIsSpeechSettingsOpen(true)}
            className="text-xs font-semibold text-slate-600 hover:text-emerald-700 bg-white px-2.5 py-1.5 rounded-xl border border-slate-200 flex items-center gap-1.5 shadow-sm transition"
            title="Cấu hình AI Speech Synthesis (Alt+S)"
          >
            <Settings className="w-3.5 h-3.5 text-slate-500" />
            <span className="hidden sm:inline">Phát âm</span>
            <kbd className="text-[10px] bg-slate-100 text-slate-500 px-1 py-0.5 rounded font-mono">Alt+S</kbd>
          </button>

          <div className="flex items-center gap-3 text-xs font-bold text-slate-600 bg-white px-3 py-1.5 rounded-xl border border-slate-200">
            <span className="text-emerald-700">✓ {session.correctCount}</span>
            <span className="text-rose-600">✕ {session.incorrectCount + session.timedOutCount}</span>
            {session.bestStreak > 1 && (
              <span className="text-amber-600">🔥 {session.bestStreak}</span>
            )}
          </div>
        </div>
      </div>

      {/* State 1: Ready to start round 1 */}
      {session.status === 'Ready' && !session.activeRound && (
        <div className="my-auto bg-white rounded-3xl p-8 border border-slate-200 text-center flex flex-col items-center gap-4 shadow-sm">
          <div className="text-5xl">🎯</div>
          <h2 className="text-2xl font-extrabold text-slate-800">Ván chơi đã sẵn sàng!</h2>
          <p className="text-sm text-slate-500 max-w-md">
            Gồm {session.actualCount} từ, mỗi từ có {session.timeLimitSeconds} giây. Nhấn nút bên dưới khi bạn đã sẵn sàng.
          </p>
          <button
            type="button"
            onClick={handleStartNext}
            disabled={startNextRoundMutation.isPending}
            className="mt-2 py-4 px-8 rounded-2xl bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white font-extrabold text-lg shadow-lg shadow-emerald-600/25 transition"
          >
            {startNextRoundMutation.isPending ? 'Đang chuẩn bị...' : 'Bắt đầu ngay 🚀'}
          </button>
        </div>
      )}

      {/* State 2: Active Round Playing */}
      {session.activeRound && !activeResult && (
        <div className="flex flex-col flex-1 gap-4">
          <FallingGameCanvas
            activeRound={session.activeRound}
            currentInput={inputValue}
            onDeadlineReached={handleDeadlineReached}
            streak={session.currentStreak}
          />

          {/* Action Helper Bar: Pronounce Hint & Skip Word */}
          <div className="flex items-center justify-between gap-3 px-1">
            <button
              type="button"
              onClick={handlePlayAudioHint}
              disabled={isAudioBusy}
              className="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl text-xs font-bold text-emerald-800 bg-emerald-50 hover:bg-emerald-100 border border-emerald-300 shadow-sm active:scale-95 transition"
              title="Nghe phát âm từ này làm gợi ý (Ctrl+Space)"
            >
              {isAudioBusy ? (
                <Loader2 className="w-4 h-4 animate-spin text-emerald-600" />
              ) : isAudioPlaying ? (
                <Square className="w-4 h-4 text-emerald-600" />
              ) : (
                <Volume2 className="w-4 h-4 text-emerald-600" />
              )}
              <span>{isAudioBusy ? 'Đang tạo âm...' : isAudioPlaying ? 'Dừng nghe' : 'Phát âm gợi ý'}</span>
              <kbd className="hidden sm:inline text-[10px] bg-emerald-100 text-emerald-700 px-1.5 py-0.5 rounded font-mono border border-emerald-200">
                Ctrl+Space
              </kbd>
            </button>

            <button
              type="button"
              onClick={handleSkipRound}
              disabled={skipRoundMutation.isPending || submitAnswerMutation.isPending}
              className="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl text-xs font-bold text-amber-800 bg-amber-50 hover:bg-amber-100 border border-amber-300 shadow-sm active:scale-95 transition"
              title="Bỏ qua từ hiện tại và xem đáp án (Ctrl+K)"
            >
              {skipRoundMutation.isPending ? (
                <Loader2 className="w-4 h-4 animate-spin text-amber-600" />
              ) : (
                <FastForward className="w-4 h-4 text-amber-600" />
              )}
              <span>{skipRoundMutation.isPending ? 'Đang bỏ qua...' : 'Bỏ qua từ này'}</span>
              <kbd className="hidden sm:inline text-[10px] bg-amber-100 text-amber-700 px-1.5 py-0.5 rounded font-mono border border-amber-200">
                Ctrl+K
              </kbd>
            </button>
          </div>

          {audioError && (
            <div className="text-xs text-rose-600 px-2 flex items-center justify-between bg-rose-50 p-2 rounded-xl border border-rose-200">
              <span>⚠️ {audioError}</span>
              {!apiKey && (
                <button
                  type="button"
                  onClick={() => setIsSpeechSettingsOpen(true)}
                  className="underline font-bold text-rose-700 hover:text-rose-900 ml-2"
                >
                  Nhập API Key
                </button>
              )}
            </div>
          )}

          <GameInputBar
            value={inputValue}
            onChange={setInputValue}
            onSubmit={handleSubmitAnswer}
            disabled={
              submitAnswerMutation.isPending ||
              timeoutRoundMutation.isPending ||
              skipRoundMutation.isPending
            }
            isSubmitting={submitAnswerMutation.isPending}
          />
        </div>
      )}

      {/* State 3: Round Terminal Result Overlay (between rounds) */}
      {activeResult && (
        <RoundResultOverlay
          result={activeResult}
          onNext={handleStartNext}
          isLastRound={session.completedRounds.length >= session.actualCount}
          isLoadingNext={startNextRoundMutation.isPending}
          onPlayAudio={handlePlayAudioResult}
          isAudioPlaying={isAudioPlaying}
        />
      )}

      {/* State 4: Session Completed or Abandoned Summary Modal */}
      {isTerminalSession && !activeResult && (
        <SessionSummaryModal
          session={session}
          onPlayAgain={() => navigate('/student/vocabulary-games/falling-words')}
          onGoToHistory={() => navigate('/student/vocabulary-games/falling-words/history')}
          onGoToSetup={() => navigate('/student/vocabulary-games/falling-words')}
        />
      )}

      {/* Abandon Confirm Dialog */}
      {isAbandonConfirmOpen && (
        <div
          className="fixed inset-0 bg-slate-900/50 backdrop-blur-sm flex items-center justify-center p-4 z-50"
          role="dialog"
          aria-modal="true"
        >
          <div className="bg-white rounded-3xl max-w-sm w-full p-6 shadow-xl border border-slate-200 text-center flex flex-col gap-4">
            <h3 className="text-lg font-bold text-slate-800">Dừng ván chơi hiện tại?</h3>
            <p className="text-xs text-slate-500">
              Các từ đã chơi sẽ được lưu lại lịch sử. Lượt đang chạy (nếu có) sẽ bị hủy hoặc tính hết giờ nếu đã quá hạn.
            </p>
            <div className="flex items-center gap-3 mt-2">
              <button
                type="button"
                onClick={() => setIsAbandonConfirmOpen(false)}
                className="flex-1 py-2.5 rounded-xl border border-slate-200 font-semibold text-slate-700 hover:bg-slate-50 transition text-sm"
              >
                Tiếp tục chơi
              </button>
              <button
                type="button"
                onClick={handleAbandon}
                disabled={abandonSessionMutation.isPending}
                className="flex-1 py-2.5 rounded-xl bg-rose-600 hover:bg-rose-700 text-white font-bold text-sm shadow transition"
              >
                Dừng ván
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Modal: Cấu hình AI Speech Synthesis */}
      <AiSpeechSettingsDialog
        isOpen={isSpeechSettingsOpen}
        onClose={() => setIsSpeechSettingsOpen(false)}
      />
    </div>
  );
};
