import React, { useState, useRef, useEffect } from 'react';
import { Mic, Square, Volume2, Check, Sparkles } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { LessonAudioButton } from './LessonAudioButton';
import { toast } from '@/shared/lib/toast';

interface SpeakingPracticePlayerProps {
  content: {
    prompt?: string;
    topic?: string;
    instructions?: string;
    suggestedPrompts?: string[];
    taskInstructions?: string;
    targetDurationSeconds?: number;
    cueQuestions?: string[];
  };
  onSubmit: (payload: { audioAssetId?: string; spokenText?: string }) => void;
  isSubmitting?: boolean;
  isSubmitted?: boolean;
}

export const SpeakingPracticePlayer: React.FC<SpeakingPracticePlayerProps> = ({
  content,
  onSubmit,
  isSubmitting = false,
  isSubmitted = false,
}) => {
  const [isRecording, setIsRecording] = useState(false);
  const [recordedAudioUrl, setRecordedAudioUrl] = useState<string | null>(null);
  const [isPlayingAudio, setIsPlayingAudio] = useState(false);
  const [spokenNotes, setSpokenNotes] = useState('');

  const audioRef = useRef<HTMLAudioElement | null>(null);
  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const audioChunksRef = useRef<Blob[]>([]);
  const streamRef = useRef<MediaStream | null>(null);
  const activeRef = useRef(true);
  const recordedUrlRef = useRef<string | null>(null);

  useEffect(() => {
    activeRef.current = true;
    return () => {
      activeRef.current = false;
      const recorder = mediaRecorderRef.current;
      if (recorder) {
        recorder.onstop = null;
        recorder.ondataavailable = null;
        if (recorder.state !== 'inactive') recorder.stop();
      }
      streamRef.current?.getTracks().forEach((track) => track.stop());
      audioRef.current?.pause();
      if (recordedUrlRef.current) URL.revokeObjectURL(recordedUrlRef.current);
    };
  }, []);

  const startRecording = async () => {
    window.dispatchEvent(new Event('lesson-recording-start'));
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      if (!activeRef.current) { stream.getTracks().forEach((track) => track.stop()); return; }
      streamRef.current = stream;
      audioChunksRef.current = [];
      const mr = new MediaRecorder(stream);

      mr.ondataavailable = (e) => {
        if (e.data.size > 0) audioChunksRef.current.push(e.data);
      };

      mr.onstop = () => {
        if (!activeRef.current) { stream.getTracks().forEach((track) => track.stop()); return; }
        const audioBlob = new Blob(audioChunksRef.current, { type: 'audio/webm' });
        if (recordedUrlRef.current) URL.revokeObjectURL(recordedUrlRef.current);
        const url = URL.createObjectURL(audioBlob);
        recordedUrlRef.current = url;
        setRecordedAudioUrl(url);
        stream.getTracks().forEach((track) => track.stop());
      };

      mediaRecorderRef.current = mr;
      mr.start();
      setIsRecording(true);
    } catch {
      if (!activeRef.current) return;
      setIsRecording(false);
      toast.error('Không truy cập được microphone. Vui lòng cấp quyền thu âm.');
    }
  };

  const stopRecording = () => {
    if (mediaRecorderRef.current && isRecording) {
      mediaRecorderRef.current.stop();
      setIsRecording(false);
    } else {
      setIsRecording(false);
    }
  };

  const togglePlayback = () => {
    if (!audioRef.current) return;
    if (isPlayingAudio) {
      audioRef.current.pause();
      setIsPlayingAudio(false);
    } else {
      void audioRef.current.play();
      setIsPlayingAudio(true);
    }
  };

  return (
    <div className="space-y-6 max-w-xl mx-auto">
      {/* Prompt Instructions */}
      <div className="bg-slate-50 p-6 rounded-3xl border border-slate-100 space-y-3 shadow-xs">
        <span className="text-xs uppercase font-extrabold tracking-wider text-emerald-600 flex items-center gap-1.5">
          <Sparkles className="w-4 h-4" /> Đề bài luyện nói (Speaking Practice)
        </span>

        <div className="text-base sm:text-lg font-bold text-slate-900 leading-snug">
          {content.topic ?? content.prompt}
        </div>
        <LessonAudioButton label="Nghe đề bài luyện nói" disabled={isRecording} />

        {(content.instructions || content.taskInstructions) && (
          <p className="text-xs text-slate-600 leading-relaxed">{content.instructions ?? content.taskInstructions}</p>
        )}

        {(content.suggestedPrompts ?? content.cueQuestions)?.length ? (
          <div className="pt-2 border-t border-slate-200 space-y-1">
            <span className="text-xs font-bold text-slate-700 block">Các gợi ý trả lời:</span>
            <ul className="list-disc list-inside text-xs text-slate-600 space-y-1">
              {(content.suggestedPrompts ?? content.cueQuestions ?? []).map((cq, idx) => (
                <li key={idx}>{cq}</li>
              ))}
            </ul>
          </div>
        ) : null}
      </div>

      {/* Recording Stage */}
      <div className="p-6 rounded-3xl border border-slate-200 bg-white text-center space-y-4 shadow-xs">
        <div className="flex items-center justify-center gap-4">
          {!isRecording ? (
            <Button
              type="button"
              variant="primary"
              size="lg"
              onClick={() => void startRecording()}
              disabled={isSubmitted}
              className="rounded-full gap-2 px-6 bg-rose-600 hover:bg-rose-500 text-white"
            >
              <Mic className="w-5 h-5" /> Bắt đầu ghi âm bài nói
            </Button>
          ) : (
            <Button
              type="button"
              variant="danger"
              size="lg"
              onClick={stopRecording}
              className="rounded-full gap-2 px-6 animate-pulse"
            >
              <Square className="w-5 h-5" /> Dừng ghi âm
            </Button>
          )}

          {recordedAudioUrl && !isRecording && (
            <>
              {recordedAudioUrl !== 'simulated-speaking-asset' && (
                <audio
                  ref={audioRef}
                  src={recordedAudioUrl}
                  onEnded={() => setIsPlayingAudio(false)}
                  className="hidden"
                />
              )}
              <Button
                type="button"
                variant="outline"
                size="md"
                onClick={togglePlayback}
                className="rounded-full gap-2 text-emerald-700 border-emerald-200"
              >
                <Volume2 className="w-4 h-4" />
                {isPlayingAudio ? 'Dừng' : 'Nghe lại'}
              </Button>
            </>
          )}
        </div>

        {isRecording && (
          <p className="text-xs text-rose-600 font-bold animate-pulse">
            Đang ghi âm... Hãy nói rõ ràng, tự nhiên và bám sát các tiêu chí đề bài.
          </p>
        )}

        {/* Optional text notes */}
        <div className="pt-2 text-left space-y-1">
          <label className="text-xs font-medium text-slate-600">
            Nội dung tóm tắt hoặc văn bản bổ sung (tùy chọn):
          </label>
          <input
            type="text"
            placeholder="Ghi chú thêm nội dung bài nói nếu cần..."
            value={spokenNotes}
            onChange={(e) => setSpokenNotes(e.target.value)}
            disabled={isSubmitted}
            className="w-full text-xs rounded-xl border border-slate-200 bg-slate-50/50 p-2.5 focus:outline-none focus:ring-2 focus:ring-emerald-500"
          />
        </div>
      </div>

      {/* Submit Action */}
      {!isSubmitted && (
        <div className="pt-2">
          <Button
            type="button"
            variant="primary"
            onClick={() =>
              onSubmit({
                audioAssetId: recordedAudioUrl ? 'speaking-submission-asset-01' : undefined,
                spokenText: spokenNotes.trim() || undefined,
              })
            }
            disabled={(!recordedAudioUrl && !spokenNotes.trim()) || isRecording || isSubmitting}
            className="w-full gap-2 py-3 rounded-2xl"
          >
            <Check className="w-4 h-4" />
            {isSubmitting ? 'AI đang chấm rubric kỹ năng nói...' : 'Xác nhận nộp bài nói'}
          </Button>
        </div>
      )}
    </div>
  );
};
