import React, { useState, useRef } from 'react';
import { Volume2, Play, Pause, RotateCcw, Check } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { LessonAudioButton } from './LessonAudioButton';

interface DictationPlayerProps {
  content: {
    audioUrl?: string;
    speed?: number;
    hint?: string;
  };
  onSubmit: (payload: { transcribedText: string }) => void;
  isSubmitting?: boolean;
  isSubmitted?: boolean;
  initialTranscribedText?: string;
}

export const DictationPlayer: React.FC<DictationPlayerProps> = ({
  content,
  onSubmit,
  isSubmitting = false,
  isSubmitted = false,
  initialTranscribedText = '',
}) => {
  const [transcribedText, setTranscribedText] = useState(initialTranscribedText);
  const [isPlaying, setIsPlaying] = useState(false);
  const audioRef = useRef<HTMLAudioElement | null>(null);

  const togglePlay = () => {
    if (!audioRef.current) return;
    if (isPlaying) {
      audioRef.current.pause();
      setIsPlaying(false);
    } else {
      audioRef.current.playbackRate = content.speed ?? 1.0;
      void audioRef.current.play();
      setIsPlaying(true);
    }
  };

  const handleReplay = () => {
    if (!audioRef.current) return;
    audioRef.current.currentTime = 0;
    audioRef.current.playbackRate = content.speed ?? 1.0;
    void audioRef.current.play();
    setIsPlaying(true);
  };

  return (
    <div className="space-y-6 max-w-xl mx-auto">
      {/* Audio Playback Controls */}
      <div className="bg-slate-900 text-white p-6 rounded-3xl text-center space-y-4 shadow-md">
        <span className="text-xs uppercase font-extrabold tracking-wider text-emerald-400">
          Nghe chép chính tả (Dictation)
        </span>

        {content.audioUrl ? (
          <audio
            ref={audioRef}
            src={content.audioUrl}
            onEnded={() => setIsPlaying(false)}
            className="hidden"
          />
        ) : null}

        {!content.audioUrl && <LessonAudioButton label="Nghe câu cần chép" />}
        {content.audioUrl && <div className="flex items-center justify-center gap-4 py-1">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleReplay}
            className="rounded-full text-white border-slate-700 hover:bg-slate-800"
          >
            <RotateCcw className="w-4 h-4" />
          </Button>

          <button
            type="button"
            onClick={togglePlay}
            disabled={!content.audioUrl}
            className="w-14 h-14 rounded-full bg-emerald-500 hover:bg-emerald-400 text-slate-950 flex items-center justify-center shadow-lg transition-transform active:scale-95 cursor-pointer disabled:opacity-50"
          >
            {isPlaying ? <Pause className="w-6 h-6 fill-current" /> : <Play className="w-6 h-6 fill-current ml-0.5" />}
          </button>
        </div>}

        <p className="text-xs text-slate-400">
          Nghe kỹ từng từ và gõ lại chính xác những gì bạn nghe được vào ô bên dưới.
        </p>
      </div>

      {/* Transcription Input */}
      <div className="space-y-2">
        <label className="text-xs font-bold text-slate-700 block">
          Nội dung bạn nghe được:
        </label>
        <textarea
          rows={3}
          value={transcribedText}
          onChange={(e) => setTranscribedText(e.target.value)}
          disabled={isSubmitted}
          placeholder="Gõ văn bản nghe được..."
          className="w-full text-base font-medium rounded-2xl border border-slate-200 bg-white p-4 focus:outline-none focus:ring-2 focus:ring-emerald-500 shadow-xs"
        />
      </div>

      {/* Submit Action */}
      {!isSubmitted && (
        <div className="pt-2">
          <Button
            type="button"
            variant="primary"
            onClick={() => onSubmit({ transcribedText: transcribedText.trim() })}
            disabled={transcribedText.trim().length === 0 || isSubmitting}
            className="w-full gap-2 py-3 rounded-2xl"
          >
            <Check className="w-4 h-4" />
            {isSubmitting ? 'Đang chấm điểm Levenshtein...' : 'Xác nhận nộp bài chép'}
          </Button>
        </div>
      )}
    </div>
  );
};
