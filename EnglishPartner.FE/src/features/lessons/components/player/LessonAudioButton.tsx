import React, { createContext, useContext, useEffect, useRef, useState } from 'react';
import { Volume2, Square, Loader2 } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { useTutorSessionStore } from '@/features/tutor/store/tutorSessionStore';
import { lessonApi } from '../../api/lessonApi';

export const LessonAudioContext = createContext<{ attemptId: string; exerciseId: string } | null>(null);

export function LessonAudioButton({ label = 'Nghe đọc mẫu', cardId, disabled = false }: { label?: string; cardId?: string; disabled?: boolean }) {
  const target = useContext(LessonAudioContext);
  const apiKey = useTutorSessionStore((s) => s.apiKey);
  const voiceName = useTutorSessionStore((s) => s.voiceName);
  const speechSpeed = useTutorSessionStore((s) => s.speechSpeed);
  const focusKey = useTutorSessionStore((s) => s.requestFocusLeftMenuApiKey);
  const [busy, setBusy] = useState(false);
  const [playing, setPlaying] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const audio = useRef<HTMLAudioElement | null>(null);
  const request = useRef<AbortController | null>(null);
  const session = useRef(0);

  useEffect(() => {
    setBusy(false); setPlaying(false); setError(null);
    const stop = () => {
      session.current++;
      request.current?.abort();
      audio.current?.pause();
      setBusy(false); setPlaying(false);
    };
    window.addEventListener('lesson-recording-start', stop);
    return () => {
      session.current++;
      request.current?.abort();
      audio.current?.pause();
      window.removeEventListener('lesson-recording-start', stop);
    };
  }, [target?.attemptId, target?.exerciseId, cardId, voiceName, speechSpeed]);

  const play = async () => {
    if (!target || busy) return;
    if (playing) { audio.current?.pause(); setPlaying(false); return; }
    const token = ++session.current;
    request.current?.abort();
    const controller = new AbortController();
    request.current = controller;
    setBusy(true); setError(null);
    try {
      const result = await lessonApi.synthesizeExerciseAudio(target.attemptId, target.exerciseId,
        { apiKey, voiceName, speechSpeed, cardId }, controller.signal);
      if (token !== session.current) return;
      audio.current?.pause();
      const player = new Audio(`data:${result.mimeType};base64,${result.audioBase64}`);
      audio.current = player;
      player.onended = () => { if (token === session.current) setPlaying(false); };
      player.onerror = () => { if (token === session.current) { setPlaying(false); setError('Không phát được âm thanh. Vui lòng thử lại.'); } };
      await player.play();
      if (token === session.current) setPlaying(true);
    } catch (err: any) {
      if (token !== session.current || controller.signal.aborted) return;
      const message = err?.response?.data?.error || err?.response?.data?.detail || 'Không tạo hoặc phát được âm thanh. Vui lòng thử lại.';
      setError(message);
      if (!apiKey && /API Key/i.test(message)) focusKey();
    } finally {
      if (token === session.current) setBusy(false);
    }
  };

  return <div className="space-y-2">
    <Button type="button" variant="outline" disabled={disabled || !target || busy} onClick={() => void play()} className="gap-2">
      {busy ? <Loader2 className="w-4 h-4 animate-spin" /> : playing ? <Square className="w-4 h-4" /> : <Volume2 className="w-4 h-4" />}
      {busy ? 'Đang tạo âm thanh...' : playing ? 'Dừng nghe' : label}
    </Button>
    {error && <p role="alert" className="text-sm text-rose-600">{error}</p>}
  </div>;
}
