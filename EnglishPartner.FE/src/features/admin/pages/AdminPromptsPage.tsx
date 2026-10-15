import React, { useState } from 'react';
import { MessageSquareCode, Eye, Sparkles } from 'lucide-react';
import {
  useCefrLevelPromptsQuery,
  useConversationModesQuery,
  useTeacherVoicesQuery,
} from '@/features/catalog';
import { adminApi } from '../api/adminApi';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';

export const AdminPromptsPage: React.FC = () => {
  const { data: cefrPrompts = [] } = useCefrLevelPromptsQuery();
  const { data: modePrompts = [] } = useConversationModesQuery();
  const { data: voices = [] } = useTeacherVoicesQuery();

  const [previewCefr, setPreviewCefr] = useState('B2');
  const [previewMode, setPreviewMode] = useState('technical_interview');
  const [previewVoice, setPreviewVoice] = useState('Aoede');
  const [previewTopic, setPreviewTopic] = useState('.NET 10, Clean Architecture, DDD & CQRS');
  const [previewOutput, setPreviewOutput] = useState<string>('');

  const handleRunPreview = async () => {
    const res = await adminApi.previewPrompt({
      cefrLevel: previewCefr,
      conversationMode: previewMode,
      voiceName: previewVoice,
      speechSpeed: 1.0,
      topic: previewTopic,
      extendedThinking: true,
    });
    setPreviewOutput(res);
  };

  return (
    <div className="space-y-6">
      <div className="bg-white rounded-3xl border border-slate-200 p-6 flex items-center justify-between">
        <div>
          <h1 className="text-lg font-extrabold text-slate-900 flex items-center gap-2">
            <MessageSquareCode className="w-5 h-5 text-emerald-600" />
            Quản lý CefrPromptBuilder trong Database ({cefrPrompts.length} CEFR · {modePrompts.length} Modes)
          </h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Cấu hình hướng dẫn sư phạm theo trình độ CEFR (A1-C2) và 19 chế độ luyện nói / phỏng vấn.
          </p>
        </div>
        <Badge variant="emerald">Permission: prompts.manage</Badge>
      </div>

      {/* Live System Instruction Previewer */}
      <div className="bg-white rounded-3xl border border-slate-200 p-6 space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-sm font-extrabold text-slate-900 flex items-center gap-2">
            <Sparkles className="w-4 h-4 text-amber-500" />
            Trình tạo thử System Instruction (POST /api/prompts/preview)
          </h2>
          <Button size="sm" onClick={handleRunPreview}>
            <Eye className="w-3.5 h-3.5" />
            Kết xuất System Prompt từ DB
          </Button>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
          <select
            value={previewCefr}
            onChange={(e) => setPreviewCefr(e.target.value)}
            className="px-3 py-2 rounded-xl border border-slate-200 text-xs bg-white font-semibold"
          >
            {cefrPrompts.map((c) => (
              <option key={c.cefrLevel} value={c.cefrLevel}>
                {c.displayName}
              </option>
            ))}
          </select>

          <select
            value={previewMode}
            onChange={(e) => setPreviewMode(e.target.value)}
            className="px-3 py-2 rounded-xl border border-slate-200 text-xs bg-white font-semibold"
          >
            {modePrompts.map((m) => (
              <option key={m.modeCode} value={m.modeCode}>
                {m.displayName}
              </option>
            ))}
          </select>

          <select
            value={previewVoice}
            onChange={(e) => setPreviewVoice(e.target.value)}
            className="px-3 py-2 rounded-xl border border-slate-200 text-xs bg-white font-semibold"
          >
            {voices.map((v) => (
              <option key={v.id} value={v.id}>
                {v.label} ({v.persona})
              </option>
            ))}
          </select>

          <input
            type="text"
            value={previewTopic}
            onChange={(e) => setPreviewTopic(e.target.value)}
            className="px-3 py-2 rounded-xl border border-slate-200 text-xs"
            placeholder="Topic..."
          />
        </div>

        {previewOutput && (
          <pre className="p-4 rounded-2xl bg-slate-900 text-emerald-300 text-xs font-mono whitespace-pre-wrap max-h-64 overflow-y-auto">
            {previewOutput}
          </pre>
        )}
      </div>

      {/* 19 Conversation Modes Table */}
      <div className="bg-white rounded-3xl border border-slate-200 overflow-hidden">
        <div className="px-6 py-4 border-b border-slate-100 font-extrabold text-sm text-slate-800">
          Danh sách 19 Chế độ Luyện nói (`ConversationModePrompts`)
        </div>
        <div className="divide-y divide-slate-100 max-h-[460px] overflow-y-auto">
          {modePrompts.map((m) => (
            <div key={m.modeCode} className="p-4 hover:bg-slate-50 space-y-1 text-xs">
              <div className="flex items-center justify-between">
                <div className="font-bold text-slate-900">
                  {m.displayName}{' '}
                  <span className="font-mono text-slate-400 ml-1">({m.modeCode})</span>
                </div>
                <Badge variant={m.category === 'Interview & Coaching' ? 'amber' : 'sky'}>
                  {m.category}
                </Badge>
              </div>
              <p className="text-slate-600 font-mono text-[11px]">{m.directiveTemplate}</p>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
