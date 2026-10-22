import React, { useState, useRef, useEffect } from 'react';
import { Play, Pause, Mic, Square, Check, Volume2, Loader2 } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { LessonAudioButton } from './LessonAudioButton';
import { toast } from '@/shared/lib/toast';
import { lessonApi } from '../../api/lessonApi';

interface ShadowingPlayerProps {
  content: {
    audioUrl?: string;
    referenceText?: string;
  };
  onSubmit: (payload: { audioAssetId?: string; spokenText?: string }) => void;
  isSubmitting?: boolean;
  isSubmitted?: boolean;
  initialAudioAssetId?: string;
  initialSpokenText?: string;
}

export const ShadowingPlayer: React.FC<ShadowingPlayerProps> = ({
  content,
  onSubmit,
  isSubmitting = false,
  isSubmitted = false,
  initialAudioAssetId,
  initialSpokenText,
}) => {
  const [isPlayingModel, setIsPlayingModel] = useState(false);
  const [isRecording, setIsRecording] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [recordedAudioUrl, setRecordedAudioUrl] = useState<string | null>(
    initialAudioAssetId ? `/api/lesson-media/${initialAudioAssetId}/stream` : null
  );
  const [recordedBlob, setRecordedBlob] = useState<Blob | null>(null);
  const [uploadedAssetId, setUploadedAssetId] = useState<string | null>(initialAudioAssetId ?? null);
  const [isPlayingUserAudio, setIsPlayingUserAudio] = useState(false);

  useEffect(() => {
    if (initialAudioAssetId) {
      setUploadedAssetId(initialAudioAssetId);
      setRecordedAudioUrl(`/api/lesson-media/${initialAudioAssetId}/stream`);
    }
  }, [initialAudioAssetId]);

  const modelAudioRef = useRef<HTMLAudioElement | null>(null);
  const userAudioRef = useRef<HTMLAudioElement | null>(null);
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
      modelAudioRef.current?.pause();
      userAudioRef.current?.pause();
      if (recordedUrlRef.current) URL.revokeObjectURL(recordedUrlRef.current);
    };
  }, []);

  const toggleModelAudio = () => {
    if (!modelAudioRef.current) return;
    if (isPlayingModel) {
      modelAudioRef.current.pause();
      setIsPlayingModel(false);
    } else {
      void modelAudioRef.current.play();
      setIsPlayingModel(true);
    }
  };

  const startRecording = async () => {
    window.dispatchEvent(new Event('lesson-recording-start'));
    if (isPlayingModel && modelAudioRef.current) {
      modelAudioRef.current.pause();
      setIsPlayingModel(false);
    }

    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      if (!activeRef.current) {
        stream.getTracks().forEach((track) => track.stop());
        return;
      }
      streamRef.current = stream;
      audioChunksRef.current = [];

      const mimeType = MediaRecorder.isTypeSupported('audio/webm;codecs=opus')
        ? 'audio/webm;codecs=opus'
        : MediaRecorder.isTypeSupported('audio/mp4')
        ? 'audio/mp4'
        : '';

      const mr = mimeType ? new MediaRecorder(stream, { mimeType }) : new MediaRecorder(stream);

      mr.ondataavailable = (e) => {
        if (e.data.size > 0) audioChunksRef.current.push(e.data);
      };

      mr.onstop = () => {
        if (!activeRef.current) {
          stream.getTracks().forEach((track) => track.stop());
          return;
        }
        const effectiveType = mr.mimeType || 'audio/webm';
        const audioBlob = new Blob(audioChunksRef.current, { type: effectiveType });
        if (recordedUrlRef.current) URL.revokeObjectURL(recordedUrlRef.current);
        const url = URL.createObjectURL(audioBlob);
        recordedUrlRef.current = url;
        setRecordedBlob(audioBlob);
        setUploadedAssetId(null);
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

  const toggleUserAudio = () => {
    if (!userAudioRef.current) return;
    if (isPlayingUserAudio) {
      userAudioRef.current.pause();
      setIsPlayingUserAudio(false);
    } else {
      void userAudioRef.current.play();
      setIsPlayingUserAudio(true);
    }
  };

  const handleSubmit = async () => {
    let assetId = uploadedAssetId;

    if (!assetId && recordedBlob) {
      setIsUploading(true);
      try {
        const ext = recordedBlob.type.includes('mp4') ? 'mp4' : recordedBlob.type.includes('ogg') ? 'ogg' : 'webm';
        const up = await lessonApi.uploadLessonMedia(recordedBlob, `shadowing-${Date.now()}.${ext}`);
        assetId = up.assetId;
        setUploadedAssetId(assetId);
      } catch (err: any) {
        setIsUploading(false);
        toast.error(err?.response?.data?.error || err?.response?.data?.detail || 'Không tải lên được bản ghi âm. Vui lòng thử lại.');
        return;
      } finally {
        setIsUploading(false);
      }
    }

    if (!assetId) {
      toast.error('Vui lòng ghi âm câu nhại trước khi nộp.');
      return;
    }

    // Explicit constraint: Never copy referenceText into spokenText.
    onSubmit({
      audioAssetId: assetId,
    });
  };

  const busy = isSubmitting || isUploading;

  return (
    <div className="space-y-6 max-w-xl mx-auto">
      {/* Reference Model Audio */}
      <div className="bg-slate-50 p-6 rounded-3xl border border-slate-100 text-center space-y-4 shadow-xs">
        <span className="text-xs uppercase font-extrabold tracking-wider text-emerald-600">
          Luyện nói nhại (Shadowing) theo giọng mẫu
        </span>

        {content.audioUrl ? (
          <audio
            ref={modelAudioRef}
            src={content.audioUrl}
            onEnded={() => setIsPlayingModel(false)}
            className="hidden"
          />
        ) : null}

        {!content.audioUrl && <LessonAudioButton label="Nghe câu mẫu bản ngữ" disabled={isRecording} />}
        {content.audioUrl && (
          <div>
            <Button
              type="button"
              variant="outline"
              size="md"
              onClick={toggleModelAudio}
              disabled={!content.audioUrl || isRecording}
              className="rounded-full gap-2 text-emerald-700 border-emerald-200 hover:bg-emerald-50/70"
            >
              {isPlayingModel ? <Pause className="w-4 h-4" /> : <Play className="w-4 h-4" />}
              {isPlayingModel ? 'Đang phát âm mẫu...' : '1. Nghe câu mẫu bản ngữ'}
            </Button>
          </div>
        )}

        {content.referenceText && (
          <div className="p-4 bg-white rounded-2xl border border-slate-200 text-base sm:text-lg font-bold text-slate-900 leading-relaxed">
            &ldquo;{content.referenceText}&rdquo;
          </div>
        )}
      </div>

      {/* Recording Stage */}
      <div className="p-6 rounded-3xl border border-slate-200 bg-white text-center space-y-4 shadow-xs">
        <span className="text-xs font-bold text-slate-700 uppercase tracking-wider block">
          2. Ghi âm giọng của bạn:
        </span>

        <div className="flex items-center justify-center gap-4">
          {!isRecording ? (
            <Button
              type="button"
              variant="primary"
              size="lg"
              onClick={() => void startRecording()}
              disabled={isSubmitted || busy}
              className="rounded-full gap-2 px-6 bg-rose-600 hover:bg-rose-500 text-white"
            >
              <Mic className="w-5 h-5" /> Bắt đầu ghi âm
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
              <audio
                ref={userAudioRef}
                src={recordedAudioUrl}
                onEnded={() => setIsPlayingUserAudio(false)}
                className="hidden"
              />
              <Button
                type="button"
                variant="outline"
                size="md"
                onClick={toggleUserAudio}
                className="rounded-full gap-2 text-emerald-700 border-emerald-200"
              >
                <Volume2 className="w-4 h-4" />
                {isPlayingUserAudio ? 'Dừng' : 'Nghe lại'}
              </Button>
            </>
          )}
        </div>

        {isRecording && (
          <p className="text-xs text-rose-600 font-bold animate-pulse">
            Đang thu âm... Nói rõ ràng từng âm theo ngữ điệu mẫu.
          </p>
        )}
      </div>

      {/* Submit Action */}
      {!isSubmitted && (
        <div className="pt-2">
          <Button
            type="button"
            variant="primary"
            onClick={handleSubmit}
            disabled={(!recordedBlob && !uploadedAssetId) || isRecording || busy}
            className="w-full gap-2 py-3 rounded-2xl"
          >
            {busy ? <Loader2 className="w-4 h-4 animate-spin" /> : <Check className="w-4 h-4" />}
            {isUploading ? 'Đang tải lên bản thu âm...' : isSubmitting ? 'AI đang chấm rubric phát âm...' : 'Xác nhận nộp bài nói nhại'}
          </Button>
        </div>
      )}
    </div>
  );
};
