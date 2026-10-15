import React from 'react';
import {
  Mic,
  Volume2,
  Gauge,
  Brain,
  Sparkles,
  MessageSquareCode,
  FileText,
  Star,
} from 'lucide-react';
import {
  useConversationModesQuery,
  useSpeechSpeedsQuery,
  useTeacherVoicesQuery,
  useTopicsQuery,
} from '@/features/catalog';
import { TopicCreatablePicker } from '@/shared/components/ui/TopicCreatablePicker';
import { Badge } from '@/shared/components/ui/Badge';
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

interface TutorSetupSidebarProps {
  isSessionActive: boolean;
}

/**
 * Database-Driven Session Configuration Sidebar for Student Live AI Tutor:
 * - Chế độ luyện nói (19 Modes from /api/prompts/modes)
 * - Giọng nói của Teacher (30 Google Prebuilt Voices from /api/voices)
 * - Tốc độ nói (Speech Speeds from /api/prompts/speech-speeds)
 * - Chủ đề cuộc trò chuyện (DB Topic suggestions from /api/topics + free custom input)
 */
export const TutorSetupSidebar: React.FC<TutorSetupSidebarProps> = ({ isSessionActive }) => {
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

  const displayedVoices = showOnlyFeaturedVoices
    ? allVoices.filter((v) => v.isFeatured)
    : allVoices;

  const interviewModes = modes.filter((m) => m.category === 'Interview & Coaching');
  const workplaceModes = modes.filter((m) => m.category !== 'Interview & Coaching');
  const currentModeObj = modes.find((m) => m.modeCode === conversationMode);
  const currentVoiceObj = allVoices.find((v) => v.id === voiceName);

  return (
    <aside className="bg-white rounded-3xl border border-slate-200/90 shadow-xs p-5 space-y-5 h-full overflow-y-auto">
      <div className="flex items-center justify-between border-b border-slate-100 pb-3">
        <div>
          <h2 className="text-base font-extrabold text-slate-900 flex items-center gap-2">
            <Sparkles className="w-4 h-4 text-emerald-600" />
            Thiết lập Phiên Luyện Nói
          </h2>
          <p className="text-xs text-slate-500">
            Đồng bộ trực tiếp từ Database (Modes, Voices, Speeds & Topics)
          </p>
        </div>
        {isSessionActive && <Badge variant="amber">Đang trong phiên Live</Badge>}
      </div>

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
                disabled={isSessionActive}
                onClick={() => setCefrLevel(lvl.id)}
                className={cn(
                  'py-2 px-1 rounded-xl border text-center transition-all cursor-pointer disabled:opacity-60',
                  active
                    ? 'bg-emerald-600 text-white border-emerald-600 shadow-xs'
                    : 'bg-slate-50 hover:bg-slate-100 text-slate-700 border-slate-200'
                )}
              >
                <div className="text-xs font-extrabold">{lvl.id}</div>
                <div className={cn('text-[9px] truncate', active ? 'text-emerald-100' : 'text-slate-400')}>
                  {lvl.badge}
                </div>
              </button>
            );
          })}
        </div>
      </div>

      {/* 2. Conversation Mode (Fetched from Database /api/prompts/modes) */}
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
          disabled={isSessionActive}
          onChange={(e) => setConversationMode(e.target.value)}
          className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 bg-white text-sm font-semibold text-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-500/30 focus:border-emerald-500 disabled:bg-slate-100"
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

      {/* 3. Teacher Voice (Fetched from Database /api/voices — 30 Google Voices) */}
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
            {showOnlyFeaturedVoices ? `Hiện tất cả ${allVoices.length} giọng` : 'Chỉ hiện 6 giọng nổi bật'}
          </button>
        </div>

        <div className="grid grid-cols-2 gap-2 max-h-48 overflow-y-auto pr-1">
          {displayedVoices.map((v) => {
            const active = voiceName === v.id;
            return (
              <button
                key={v.id}
                type="button"
                disabled={isSessionActive}
                onClick={() => setVoiceName(v.id)}
                className={cn(
                  'p-2.5 rounded-xl border text-left transition-all cursor-pointer disabled:opacity-60',
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
              Đang chọn: <strong className="text-slate-800">{currentVoiceObj.label}</strong> ({currentVoiceObj.persona})
            </span>
            <span>{currentVoiceObj.gender}</span>
          </div>
        )}
      </div>

      {/* 4. Speech Speed (Fetched from Database /api/prompts/speech-speeds) */}
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <label className="text-xs font-bold uppercase tracking-wider text-slate-600 flex items-center gap-1.5">
            <Gauge className="w-3.5 h-3.5 text-emerald-600" />
            4. Tốc độ nói của Teacher
          </label>
          <span className="text-xs font-bold text-emerald-700">{speechSpeed.toFixed(2)}x</span>
        </div>

        <select
          value={speechSpeed}
          disabled={isSessionActive}
          onChange={(e) => setSpeechSpeed( parseFloat(e.target.value) )}
          className="w-full px-3.5 py-2 rounded-xl border border-slate-200 bg-white text-xs font-semibold text-slate-800"
        >
          {speechSpeeds.map((sp) => (
            <option key={sp.value} value={sp.value}>
              {sp.label} ({sp.recommendedCefrRange})
            </option>
          ))}
        </select>
      </div>

      {/* 5. Conversation Topic (DB Suggestions + Free Custom Input) */}
      <TopicCreatablePicker
        label="5. Chủ đề cuộc trò chuyện (Topic)"
        value={topic}
        onChange={setTopic}
        topics={conversationTopics}
        disabled={isSessionActive}
        activeModeCode={conversationMode}
        helperText="Chọn nhanh chủ đề gợi ý từ Database (tự động ưu tiên theo Chế độ luyện nói) hoặc tự gõ chủ đề bất kỳ."
      />

      {/* 6. Custom System Prompt & Extended Thinking */}
      <div className="space-y-2 pt-2 border-t border-slate-100">
        <label className="text-xs font-bold uppercase tracking-wider text-slate-600 flex items-center gap-1.5">
          <FileText className="w-3.5 h-3.5 text-emerald-600" />
          6. Custom System Prompt / JD / CV (Tùy chọn)
        </label>
        <textarea
          rows={2}
          disabled={isSessionActive}
          value={customSystemPrompt}
          onChange={(e) => setCustomSystemPrompt(e.target.value)}
          placeholder="Dán JD tuyển dụng, tóm tắt CV hoặc yêu cầu đóng vai cụ thể..."
          className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-white text-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-500/30"
        />

        <label className="flex items-center justify-between p-2.5 rounded-xl bg-slate-50 border border-slate-200/80 cursor-pointer">
          <span className="flex items-center gap-2 text-xs font-semibold text-slate-700">
            <Brain className="w-4 h-4 text-violet-600" />
            Extended Thinking (Suy luận sâu cho C#, System Design)
          </span>
          <input
            type="checkbox"
            disabled={isSessionActive}
            checked={extendedThinking}
            onChange={(e) => setExtendedThinking(e.target.checked)}
            className="rounded text-emerald-600 focus:ring-emerald-500"
          />
        </label>
      </div>
    </aside>
  );
};
