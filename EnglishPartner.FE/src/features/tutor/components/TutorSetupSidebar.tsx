import React, { useEffect } from 'react';
import { createPortal } from 'react-dom';
import {
  Volume2,
  Gauge,
  Brain,
  MessageSquareCode,
  FileText,
  Star,
  X,
  Save,
  Mic,
  Settings,
} from 'lucide-react';
import {
  useConversationModesQuery,
  useSpeechSpeedsQuery,
  useTeacherVoicesQuery,
  useTopicsQuery,
} from '@/features/catalog';
import { TopicCreatablePicker } from '@/shared/components/ui/TopicCreatablePicker';
import { Badge } from '@/shared/components/ui/Badge';
import { Button } from '@/shared/components/ui/Button';
import { useTutorSessionStore } from '../store/tutorSessionStore';
import type { CefrLevel } from '@/shared/api/openapi.types';
import { cn } from '@/shared/lib/cn';

const CEFR_LEVELS: { id: CefrLevel; label: string; badge: string }[] = [
  { id: 'A1', label: 'A1', badge: 'Beginner' },
  { id: 'A2', label: 'A2', badge: 'Elementary' },
  { id: 'B1', label: 'B1', badge: 'Intermediate' },
  { id: 'B2', label: 'B2', badge: 'Upper-Int' },
  { id: 'C1', label: 'C1', badge: 'Advanced' },
  { id: 'C2', label: 'C2', badge: 'Mastery' },
];

export interface TutorSetupModalProps {
  isOpen?: boolean;
  open?: boolean;
  isSessionActive: boolean;
  onClose: () => void;
  onSaveSettings?: () => void;
  onSaveOnly?: () => void;
  onStartSpeaking?: () => void;
  onApplyAndStart?: () => void;
}

/**
 * Modern, spacious Dialog/Modal for "Thiết lập Phiên Luyện Nói":
 * - Contains CEFR Level, Conversation Mode (19 DB Modes), Teacher Voice (30 Google Voices),
 *   Speech Speed, Topic (DB suggestions + custom input), Custom System Prompt, and Extended Thinking.
 * - Rendered via React Portal so it is never clipped by parent containers.
 * - Includes "Lưu thiết lập" and "Bắt đầu luyện nói" action buttons in the modal footer.
 */
export const TutorSetupSidebar: React.FC<TutorSetupModalProps> = ({
  isOpen,
  open,
  isSessionActive,
  onClose,
  onSaveSettings,
  onSaveOnly,
  onStartSpeaking,
  onApplyAndStart,
}) => {
  const visible = Boolean(isOpen ?? open);
  const handleSave = onSaveSettings ?? onSaveOnly ?? onClose;
  const handleStart = onStartSpeaking ?? onApplyAndStart ?? onClose;

  const {
    cefrLevel,
    conversationMode,
    voiceName,
    speechSpeed,
    topic,
    customSystemPrompt,
    extendedThinking,
    showOnlyFeaturedVoices,
    setCefrLevel,
    setConversationMode,
    setVoiceName,
    setSpeechSpeed,
    setTopic,
    setCustomSystemPrompt,
    setExtendedThinking,
    setShowOnlyFeaturedVoices,
  } = useTutorSessionStore();

  const { data: modes = [] } = useConversationModesQuery();
  const { data: allVoices = [] } = useTeacherVoicesQuery(true, false);
  const { data: speechSpeeds = [] } = useSpeechSpeedsQuery();
  const { data: conversationTopics = [] } = useTopicsQuery({
    onlyActive: true,
    supportsConversation: true,
  });

  useEffect(() => {
    if (!visible) return;
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [visible, onClose]);

  if (!visible) return null;

  const displayedVoices = showOnlyFeaturedVoices
    ? allVoices.filter((v) => v.isFeatured)
    : allVoices;

  const interviewModes = modes.filter((m) => m.category === 'Interview & Coaching');
  const workplaceModes = modes.filter((m) => m.category !== 'Interview & Coaching');
  const currentModeObj = modes.find((m) => m.modeCode === conversationMode);
  const currentVoiceObj = allVoices.find((v) => v.id === voiceName);

  const modalNode = (
    <div
      className="fixed inset-0 z-[100] bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-3 sm:p-6 animate-fadeIn"
      onClick={onClose}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="tutor-setup-modal-title"
        className="bg-white rounded-3xl border border-slate-200 shadow-2xl max-w-4xl w-full max-h-[90vh] flex flex-col overflow-hidden"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Modal Header (Fixed) */}
        <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between gap-4 bg-slate-50/70 shrink-0">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-2xl bg-emerald-600 text-white flex items-center justify-center shadow-xs shrink-0">
              <Settings className="w-5 h-5" />
            </div>
            <div>
              <h2
                id="tutor-setup-modal-title"
                className="text-base sm:text-lg font-extrabold text-slate-900 flex items-center gap-2"
              >
                Thiết lập Phiên Luyện Nói
                {isSessionActive && <Badge variant="amber">Đang trong phiên Live</Badge>}
              </h2>
              <p className="text-xs text-slate-500">
                Đồng bộ trực tiếp từ Database (19 Chế độ luyện nói · 30 Giọng Teacher · Tốc độ nói · 26+ Chủ đề gợi ý)
              </p>
            </div>
          </div>

          <button
            type="button"
            onClick={onClose}
            title="Đóng cửa sổ thiết lập (Esc)"
            className="p-2 rounded-xl text-slate-400 hover:text-slate-700 hover:bg-slate-200/70 transition-colors cursor-pointer"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Modal Scrollable Body */}
        <div className="flex-1 overflow-y-auto p-5 sm:p-6 grid grid-cols-1 lg:grid-cols-12 gap-6">
          {/* Left Column inside Modal: CEFR, Mode, Teacher Voice, Speech Speed */}
          <div className="lg:col-span-6 space-y-5">
            {/* 1. CEFR Level Selector */}
            <div className="space-y-2">
              <label className="text-xs font-bold uppercase tracking-wider text-slate-600 block">
                1. Trình độ CEFR mục tiêu
              </label>
              <div className="grid grid-cols-6 gap-1.5">
                {CEFR_LEVELS.map((lvl) => {
                  const active = cefrLevel === lvl.id;
                  return (
                    <button
                      key={lvl.id}
                      type="button"
                      onClick={() => setCefrLevel(lvl.id)}
                      className={cn(
                        'py-2 px-1 rounded-xl border text-center transition-all cursor-pointer',
                        active
                          ? 'bg-emerald-600 text-white border-emerald-600 shadow-xs'
                          : 'bg-slate-50 hover:bg-slate-100 text-slate-700 border-slate-200'
                      )}
                    >
                      <div className="text-xs font-extrabold">{lvl.id}</div>
                      <div
                        className={cn(
                          'text-[9px] truncate',
                          active ? 'text-emerald-100' : 'text-slate-400'
                        )}
                      >
                        {lvl.badge}
                      </div>
                    </button>
                  );
                })}
              </div>
            </div>

            {/* 2. Conversation Mode */}
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <label className="text-xs font-bold uppercase tracking-wider text-slate-600 flex items-center gap-1.5">
                  <MessageSquareCode className="w-3.5 h-3.5 text-emerald-600" />
                  2. Chế độ luyện nói ({modes.length} DB Modes)
                </label>
                <Badge variant="emerald">DB</Badge>
              </div>

              <select
                value={conversationMode}
                onChange={(e) => setConversationMode(e.target.value)}
                className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 bg-white text-sm font-semibold text-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-500/30 focus:border-emerald-500"
              >
                {workplaceModes.length > 0 && (
                  <optgroup label="Giao tiếp & Công sở (Communication & Workplace)">
                    {workplaceModes.map((m) => (
                      <option key={m.modeCode} value={m.modeCode}>
                        {m.displayName}
                      </option>
                    ))}
                  </optgroup>
                )}
                {interviewModes.length > 0 && (
                  <optgroup label="Phỏng vấn & Huấn luyện 1-1 (Interview & Coaching)">
                    {interviewModes.map((m) => (
                      <option key={m.modeCode} value={m.modeCode}>
                        {m.displayName}
                      </option>
                    ))}
                  </optgroup>
                )}
              </select>

              {currentModeObj && (
                <p className="text-[11px] text-slate-500 bg-slate-50 rounded-xl px-3 py-2 border border-slate-200/70">
                  <strong className="text-slate-700">{currentModeObj.category}:</strong>{' '}
                  {currentModeObj.displayName}
                </p>
              )}
            </div>

            {/* 3. Teacher Voice */}
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <label className="text-xs font-bold uppercase tracking-wider text-slate-600 flex items-center gap-1.5">
                  <Volume2 className="w-3.5 h-3.5 text-emerald-600" />
                  3. Giọng nói của Teacher ({allVoices.length} Google Voices)
                </label>
                <button
                  type="button"
                  onClick={() => setShowOnlyFeaturedVoices(!showOnlyFeaturedVoices)}
                  className="text-[11px] font-semibold text-emerald-700 hover:underline flex items-center gap-1 cursor-pointer"
                >
                  <Star className="w-3 h-3" />
                  {showOnlyFeaturedVoices
                    ? `Hiện tất cả ${allVoices.length} giọng`
                    : 'Chỉ hiện 6 giọng nổi bật'}
                </button>
              </div>

              <div className="grid grid-cols-2 gap-2 max-h-48 overflow-y-auto pr-1">
                {displayedVoices.map((v) => {
                  const active = voiceName === v.id;
                  return (
                    <button
                      key={v.id}
                      type="button"
                      onClick={() => setVoiceName(v.id)}
                      className={cn(
                        'p-2.5 rounded-xl border text-left transition-all cursor-pointer',
                        active
                          ? 'bg-emerald-50 border-emerald-500 ring-1 ring-emerald-500/30'
                          : 'bg-white hover:bg-slate-50 border-slate-200'
                      )}
                    >
                      <div className="flex items-center justify-between gap-1">
                        <span className="text-xs font-bold text-slate-800 truncate">{v.label}</span>
                        <span className="text-[10px] px-1.5 py-0.2 rounded bg-slate-100 text-slate-600 font-semibold">
                          {v.persona}
                        </span>
                      </div>
                      <p className="text-[11px] text-slate-500 truncate mt-0.5">{v.style}</p>
                    </button>
                  );
                })}
              </div>

              {currentVoiceObj && (
                <div className="text-[11px] text-slate-500 flex items-center justify-between px-1">
                  <span>
                    Đang chọn: <strong className="text-slate-800">{currentVoiceObj.label}</strong> (
                    {currentVoiceObj.persona})
                  </span>
                  <span>{currentVoiceObj.gender}</span>
                </div>
              )}
            </div>

            {/* 4. Speech Speed */}
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <label className="text-xs font-bold uppercase tracking-wider text-slate-600 flex items-center gap-1.5">
                  <Gauge className="w-3.5 h-3.5 text-emerald-600" />
                  4. Tốc độ nói của Teacher
                </label>
                <span className="text-xs font-bold text-emerald-700">
                  {speechSpeed.toFixed(2)}x
                </span>
              </div>

              <select
                value={speechSpeed}
                onChange={(e) => setSpeechSpeed(parseFloat(e.target.value))}
                className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 bg-white text-xs font-semibold text-slate-800"
              >
                {speechSpeeds.map((sp) => (
                  <option key={sp.value} value={sp.value}>
                    {sp.label} ({sp.recommendedCefrRange})
                  </option>
                ))}
              </select>
            </div>
          </div>

          {/* Right Column inside Modal: Topic + Custom System Prompt + Extended Thinking */}
          <div className="lg:col-span-6 space-y-5">
            {/* 5. Conversation Topic (DB Suggestions + Free Custom Input) */}
            <TopicCreatablePicker
              label="5. Chủ đề cuộc trò chuyện (Topic)"
              value={topic}
              onChange={setTopic}
              topics={conversationTopics}
              activeModeCode={conversationMode}
              helperText="Chọn nhanh chủ đề gợi ý từ Database (tự động ưu tiên theo Chế độ luyện nói) hoặc tự gõ chủ đề bất kỳ."
            />

            {/* 6. Custom System Prompt / JD / CV */}
            <div className="space-y-2 pt-2 border-t border-slate-100">
              <label className="text-xs font-bold uppercase tracking-wider text-slate-600 flex items-center gap-1.5">
                <FileText className="w-3.5 h-3.5 text-emerald-600" />
                6. Custom System Prompt / JD / CV (Tùy chọn)
              </label>
              <textarea
                rows={4}
                value={customSystemPrompt}
                onChange={(e) => setCustomSystemPrompt(e.target.value)}
                placeholder="Dán JD tuyển dụng, tóm tắt CV hoặc yêu cầu đóng vai cụ thể..."
                className="w-full px-3.5 py-2.5 text-xs rounded-xl border border-slate-200 bg-white text-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-500/30"
              />

              <label className="flex items-center justify-between p-3 rounded-xl bg-slate-50 border border-slate-200/80 cursor-pointer">
                <span className="flex items-center gap-2 text-xs font-semibold text-slate-700">
                  <Brain className="w-4 h-4 text-violet-600 shrink-0" />
                  Extended Thinking (Suy luận sâu cho C#, System Design)
                </span>
                <input
                  type="checkbox"
                  checked={extendedThinking}
                  onChange={(e) => setExtendedThinking(e.target.checked)}
                  className="rounded text-emerald-600 focus:ring-emerald-500"
                />
              </label>
            </div>
          </div>
        </div>

        {/* Modal Footer (Fixed) with "Lưu thiết lập" & "Bắt đầu luyện nói" */}
        <div className="px-6 py-4 bg-slate-50 border-t border-slate-200 flex flex-wrap items-center justify-between gap-3 shrink-0">
          <div className="flex items-center gap-1.5 flex-wrap text-xs">
            <Badge variant="emerald">CEFR {cefrLevel}</Badge>
            <Badge variant="sky">{conversationMode}</Badge>
            <Badge variant="violet">{voiceName}</Badge>
            <Badge variant="amber">{speechSpeed.toFixed(2)}x</Badge>
          </div>

          <div className="flex items-center gap-2.5 ml-auto">
            <Button variant="outline" size="md" onClick={handleSave}>
              <Save className="w-4 h-4" />
              Lưu thiết lập
            </Button>
            <Button variant="primary" size="md" onClick={handleStart}>
              <Mic className="w-4 h-4" />
              {isSessionActive ? 'Áp dụng & Khởi động lại phiên' : 'Bắt đầu luyện nói'}
            </Button>
          </div>
        </div>
      </div>
    </div>
  );

  if (typeof document !== 'undefined' && document.body) {
    return createPortal(modalNode, document.body);
  }
  return modalNode;
};
