import React, { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { Volume2, Code2, Key, Check, Sparkles } from 'lucide-react';
import { Dialog } from '@/shared/components/ui/Dialog';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { cn } from '@/shared/lib/cn';
import { useTeacherVoicesQuery, useSpeechSpeedsQuery, usePronunciationModelsQuery } from '@/features/catalog';
import { useTutorSessionStore } from '@/features/tutor';
import type { SynthesizePronunciationAudioResponseDto, TeacherVoiceName } from '@/shared/api/openapi.types';

export interface AiSpeechSettingsDialogProps {
  isOpen: boolean;
  onClose: () => void;
  title?: string;
  description?: string;
  icon?: React.ReactNode;
  lastSynthesizedAudio?: SynthesizePronunciationAudioResponseDto | null;
  showSystemPromptUsed?: boolean;
  onToggleSystemPromptUsed?: () => void;
}

export const AiSpeechSettingsDialog: React.FC<AiSpeechSettingsDialogProps> = ({
  isOpen,
  onClose,
  title,
  description,
  icon,
  lastSynthesizedAudio,
  showSystemPromptUsed = false,
  onToggleSystemPromptUsed,
}) => {
  const { t } = useTranslation();
  const audioPlayerRef = useRef<HTMLAudioElement | null>(null);

  const { data: allVoices = [] } = useTeacherVoicesQuery(true, false);
  const { data: speechSpeeds = [] } = useSpeechSpeedsQuery();
  const { data: assessmentModels = [] } = usePronunciationModelsQuery();

  const apiKey = useTutorSessionStore((s) => s.apiKey);
  const requestFocusLeftMenuApiKey = useTutorSessionStore((s) => s.requestFocusLeftMenuApiKey);

  const ttsModelCode = useTutorSessionStore((s) => s.ttsModelCode);
  const setTtsModelCode = useTutorSessionStore((s) => s.setTtsModelCode);

  const voiceName = useTutorSessionStore((s) => s.voiceName);
  const setVoiceName = useTutorSessionStore((s) => s.setVoiceName);

  const ttsAccent = useTutorSessionStore((s) => s.ttsAccent);
  const setTtsAccent = useTutorSessionStore((s) => s.setTtsAccent);

  const speechSpeed = useTutorSessionStore((s) => s.speechSpeed);
  const setSpeechSpeed = useTutorSessionStore((s) => s.setSpeechSpeed);

  const assessmentModelCode = useTutorSessionStore((s) => s.assessmentModelCode);
  const setAssessmentModelCode = useTutorSessionStore((s) => s.setAssessmentModelCode);

  const handleOpenApiKeyMenu = () => {
    onClose();
    requestFocusLeftMenuApiKey();
  };

  return (
    <Dialog
      isOpen={isOpen}
      onClose={onClose}
      maxWidth="2xl"
      icon={
        icon ?? (
          <div className="relative flex items-center justify-center">
            <Volume2 className="w-5 h-5 text-white" />
            <Sparkles className="w-2.5 h-2.5 text-amber-300 absolute -top-1 -right-1" />
          </div>
        )
      }
      title={title ?? t('vocab.ttsModalTitle', 'Cấu hình AI Speech & Luyện âm')}
      description={
        description ??
        t(
          'vocab.ttsModalDesc',
          'Tùy chỉnh giọng đọc mẫu bản ngữ, ngữ điệu, tốc độ và mô hình AI chấm điểm phát âm dùng chung toàn hệ thống.'
        )
      }
      footer={
        <div className="flex items-center justify-between w-full">
          <div className="flex items-center gap-1.5 text-xs text-slate-500">
            <Key className="w-3.5 h-3.5 text-amber-500" />
            {apiKey ? (
              <span className="text-emerald-700 font-semibold flex items-center gap-1">
                <Check className="w-3 h-3" /> Đã kết nối API Key
              </span>
            ) : (
              <button
                type="button"
                onClick={handleOpenApiKeyMenu}
                className="text-amber-700 font-bold hover:underline cursor-pointer"
              >
                Chưa có API Key — Nhấn để nhập ở Left Menu
              </button>
            )}
          </div>
          <Button size="sm" onClick={onClose}>
            {t('vocab.saveAndClose', 'Lưu & Đóng')}
          </Button>
        </div>
      }
    >
      <div className="space-y-4 text-xs">
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          {/* 1. TTS Model Selection */}
          <div>
            <label className="block font-bold text-slate-700 mb-1.5">
              {t('vocab.liveTtsModelLabel', 'Mô hình AI Đọc mẫu (Live TTS)')}
            </label>
            <select
              value={ttsModelCode}
              onChange={(e) =>
                setTtsModelCode(
                  e.target.value as 'gemini-3.8-live' | 'gemini-3.8-live-extended-thinking'
                )
              }
              className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 bg-white font-semibold text-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-500"
            >
              <option value="gemini-3.8-live">
                {t('vocab.liveSpeedInstant', 'Gemini 3.8 Live (Tốc độ cao / Tức thì)')}
              </option>
              <option value="gemini-3.8-live-extended-thinking">
                {t('vocab.liveExtendedThinking', 'Gemini 3.8 Live (Extended Thinking / Chuẩn sâu)')}
              </option>
            </select>
          </div>

          {/* 2. Teacher Voice */}
          <div>
            <label className="block font-bold text-slate-700 mb-1.5">
              {t('vocab.teacherVoiceCount', {
                count: allVoices.length,
                defaultValue: `Giọng giáo viên AI (${allVoices.length} giọng)`,
              })}
            </label>
            <select
              value={voiceName}
              onChange={(e) => setVoiceName(e.target.value as TeacherVoiceName)}
              className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 bg-white font-semibold text-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-500"
            >
              {allVoices.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.label} — {v.style} ({v.persona})
                </option>
              ))}
            </select>
          </div>

          {/* 3. Accent (US vs UK) */}
          <div>
            <label className="block font-bold text-slate-700 mb-1.5">
              {t('vocab.defaultAccentLabel', 'Ngữ điệu bản ngữ mặc định')}
            </label>
            <div className="grid grid-cols-2 gap-2">
              {(['US', 'UK'] as const).map((acc) => (
                <button
                  key={acc}
                  type="button"
                  onClick={() => setTtsAccent(acc)}
                  className={cn(
                    'py-2.5 rounded-xl font-extrabold border transition-all cursor-pointer',
                    ttsAccent === acc
                      ? 'bg-emerald-600 text-white border-emerald-600 shadow-xs'
                      : 'bg-slate-50 text-slate-700 border-slate-200 hover:bg-slate-100'
                  )}
                >
                  {acc === 'US' ? '🇺🇸 US (Mỹ)' : '🇬🇧 UK (Anh)'}
                </button>
              ))}
            </div>
          </div>

          {/* 4. Speech Speed */}
          <div>
            <label className="block font-bold text-slate-700 mb-1.5">
              {t('vocab.sampleSpeedLabel', 'Tốc độ phát âm mẫu')}
            </label>
            <select
              value={speechSpeed}
              onChange={(e) => setSpeechSpeed(parseFloat(e.target.value))}
              className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 bg-white font-semibold text-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-500"
            >
              {speechSpeeds.map((sp) => (
                <option key={sp.code || sp.value} value={sp.value}>
                  {sp.label}
                </option>
              ))}
            </select>
          </div>

          {/* 5. Assessment Model */}
          <div className="sm:col-span-2">
            <label className="block font-bold text-slate-700 mb-1.5 flex items-center gap-1.5">
              <Sparkles className="w-3.5 h-3.5 text-purple-600" />
              Mô hình AI Chấm điểm & Nhận xét bài tập
            </label>
            <select
              value={assessmentModelCode}
              onChange={(e) => setAssessmentModelCode(e.target.value)}
              className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 bg-white font-semibold text-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-500"
            >
              {assessmentModels.length > 0 ? (
                assessmentModels.map((m) => (
                  <option key={m.id || m.modelCode} value={m.modelCode}>
                    {m.displayName || m.modelCode}
                  </option>
                ))
              ) : (
                <>
                  <option value="gemini-3.8-flash">gemini-3.8-flash (Khuyên dùng / Nhanh & Chuẩn)</option>
                  <option value="gemini-2.5-flash">gemini-2.5-flash (Standard)</option>
                  <option value="gemini-2.5-pro">gemini-2.5-pro (Deep Analysis)</option>
                </>
              )}
            </select>
          </div>
        </div>

        {/* Audio Sample Preview (when available) */}
        {lastSynthesizedAudio && (
          <div className="p-4 rounded-2xl bg-emerald-50/70 border border-emerald-200 space-y-2.5">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <div className="flex items-center gap-1.5 flex-wrap">
                <Badge variant="emerald">{lastSynthesizedAudio.modelCodeUsed}</Badge>
                <Badge variant="violet">Voice: {lastSynthesizedAudio.voiceName}</Badge>
                <Badge variant="sky">Accent: {lastSynthesizedAudio.accent}</Badge>
              </div>
              {onToggleSystemPromptUsed && (
                <button
                  type="button"
                  onClick={onToggleSystemPromptUsed}
                  className="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-white border border-slate-200 text-[11px] font-bold text-slate-600 hover:text-slate-900 cursor-pointer"
                >
                  <Code2 className="w-3.5 h-3.5" />
                  {showSystemPromptUsed ? t('vocab.hideSystemPrompt') : t('vocab.viewSystemPrompt')}
                </button>
              )}
            </div>
            <audio
              ref={audioPlayerRef}
              controls
              src={`data:${lastSynthesizedAudio.mimeType};base64,${lastSynthesizedAudio.audioBase64}`}
              className="w-full h-9"
            />
            {showSystemPromptUsed && lastSynthesizedAudio.systemPromptUsed && (
              <pre className="p-3 rounded-xl bg-slate-900 text-emerald-300 text-[11px] font-mono whitespace-pre-wrap">
                {lastSynthesizedAudio.systemPromptUsed}
              </pre>
            )}
          </div>
        )}
      </div>
    </Dialog>
  );
};
