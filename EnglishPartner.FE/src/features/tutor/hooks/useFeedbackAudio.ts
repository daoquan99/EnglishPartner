import { useState, useRef, useEffect, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useTutorSessionStore } from '../store/tutorSessionStore';
import { vocabularyApi } from '@/features/vocabulary/api/vocabularyApi';
import type { FeedbackAudioResponseDto } from '@/shared/api/openapi.types';
import { toast } from '@/shared/lib/toast';

interface UseFeedbackAudioOptions {
  onRequestFeedbackAudio?: (
    correctionId: string,
    variant: 'corrected' | 'improved'
  ) => Promise<FeedbackAudioResponseDto | null>;
}

const MAX_CACHE_ENTRIES = 30;
const moduleAudioCache = new Map<string, string>();
let activeGlobalAudio: HTMLAudioElement | null = null;

function getCachedAudio(key: string): string | undefined {
  const cached = moduleAudioCache.get(key);
  if (cached) {
    moduleAudioCache.delete(key);
    moduleAudioCache.set(key, cached);
  }
  return cached;
}

function setCachedAudio(key: string, dataUri: string): void {
  if (moduleAudioCache.has(key)) {
    moduleAudioCache.delete(key);
  } else if (moduleAudioCache.size >= MAX_CACHE_ENTRIES) {
    const oldestKey = moduleAudioCache.keys().next().value;
    if (oldestKey) {
      moduleAudioCache.delete(oldestKey);
    }
  }
  moduleAudioCache.set(key, dataUri);
}

export function useFeedbackAudio({ onRequestFeedbackAudio }: UseFeedbackAudioOptions = {}) {
  const { t } = useTranslation();
  const apiKey = useTutorSessionStore((s) => s.apiKey);
  const voiceName = useTutorSessionStore((s) => s.voiceName);
  const speechSpeed = useTutorSessionStore((s) => s.speechSpeed);
  const ttsAccent = useTutorSessionStore((s) => s.ttsAccent);
  const ttsModelCode = useTutorSessionStore((s) => s.ttsModelCode);

  const [loadingKey, setLoadingKey] = useState<string | null>(null);
  const currentAudioRef = useRef<HTMLAudioElement | null>(null);

  useEffect(() => {
    return () => {
      if (currentAudioRef.current) {
        currentAudioRef.current.pause();
        currentAudioRef.current.currentTime = 0;
        if (activeGlobalAudio === currentAudioRef.current) {
          activeGlobalAudio = null;
        }
        currentAudioRef.current = null;
      }
    };
  }, []);

  const playFeedbackAudio = useCallback(
    async (
      correctionId: string | undefined,
      variant: 'corrected' | 'improved',
      fallbackText: string
    ) => {
      const trimmedText = fallbackText.trim();
      if (!trimmedText) return;

      const cacheKey = `${correctionId || 'no_id'}_${variant}_${trimmedText}`;
      const cached = getCachedAudio(cacheKey);

      const playDataUri = (dataUri: string) => {
        if (activeGlobalAudio) {
          activeGlobalAudio.pause();
          activeGlobalAudio.currentTime = 0;
          activeGlobalAudio = null;
        }
        const audio = new Audio(dataUri);
        activeGlobalAudio = audio;
        currentAudioRef.current = audio;
        return audio.play();
      };

      if (cached) {
        try {
          await playDataUri(cached);
          return;
        } catch {
          // If playback of cached audio failed, retry synthesis
        }
      }

      const activeKey = `${correctionId || trimmedText}_${variant}`;
      setLoadingKey(activeKey);

      try {
        let audioBase64: string | undefined;
        let mimeType: string | undefined;

        if (correctionId && onRequestFeedbackAudio) {
          try {
            const res = await onRequestFeedbackAudio(correctionId, variant);
            if (res) {
              audioBase64 = res.audioBase64;
              mimeType = res.mimeType;
            }
          } catch (err: any) {
            const msg = String(err?.message || '').toLowerCase();
            const isFallbackEligible =
              msg.includes('feedback_audio_session_ended') ||
              msg.includes('404') ||
              msg.includes('not found') ||
              msg.includes('không tìm thấy') ||
              err?.statusCode === 404;

            if (!isFallbackEligible) {
              throw err;
            }
            // Fall through to vocabularyApi fallback when session ended or 404
          }
        }

        if (!audioBase64) {
          const res = await vocabularyApi.synthesizePronunciationAudio({
            apiKey: apiKey.trim(),
            text: trimmedText,
            targetType: 'sentence',
            accent: (ttsAccent as 'US' | 'UK') || 'US',
            voiceName: (voiceName as any) || 'Aoede',
            speechSpeed: speechSpeed || 1.0,
            modelCode: ttsModelCode || 'gemini-3.8-live',
          });
          audioBase64 = res.audioBase64;
          mimeType = res.mimeType;
        }

        if (audioBase64) {
          const dataUri = `data:${mimeType || 'audio/wav'};base64,${audioBase64}`;
          setCachedAudio(cacheKey, dataUri);
          await playDataUri(dataUri);
        }
      } catch {
        toast.error(t('tutor.audioError', 'Không thể phát âm thanh'));
      } finally {
        setLoadingKey(null);
      }
    },
    [apiKey, onRequestFeedbackAudio, speechSpeed, t, ttsAccent, ttsModelCode, voiceName]
  );

  return {
    loadingKey,
    playFeedbackAudio,
  };
}
