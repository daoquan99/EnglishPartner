import React, { useState } from 'react';
import { Send, Bot, User, Check, MessageSquare } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { cn } from '@/shared/lib/cn';

interface ChatMessage {
  role: 'ai' | 'learner';
  text: string;
}

interface ConversationRolePlayPlayerProps {
  content: {
    scenario?: string;
    learnerRole?: string;
    aiRole?: string;
    initialAiMessage?: string;
    maxTurns?: number;
  };
  onSubmit: (payload: { turns: ChatMessage[]; endSession: boolean }) => void;
  isSubmitting?: boolean;
  isSubmitted?: boolean;
}

export const ConversationRolePlayPlayer: React.FC<ConversationRolePlayPlayerProps> = ({
  content,
  onSubmit,
  isSubmitting = false,
  isSubmitted = false,
}) => {
  const initialTurns: ChatMessage[] = content.initialAiMessage
    ? [{ role: 'ai', text: content.initialAiMessage }]
    : [];

  const [messages, setMessages] = useState<ChatMessage[]>(initialTurns);
  const [inputText, setInputText] = useState('');
  const maxTurns = content.maxTurns ?? 6;

  const handleSendTurn = () => {
    if (!inputText.trim() || isSubmitted) return;

    const userMsg: ChatMessage = { role: 'learner', text: inputText.trim() };
    const updated = [...messages, userMsg];

    // Simulate conversational response from AI persona
    const aiReply: ChatMessage = {
      role: 'ai',
      text: `Thank you for sharing that! As your ${content.aiRole ?? 'partner'}, I understand your point. Could you tell me more about your experience?`,
    };

    setMessages([...updated, aiReply]);
    setInputText('');
  };

  const handleCompleteSession = () => {
    onSubmit({ turns: messages, endSession: true });
  };

  const learnerTurnsCount = messages.filter((m) => m.role === 'learner').length;

  return (
    <div className="space-y-6 max-w-2xl mx-auto">
      {/* Scenario Header */}
      <div className="bg-slate-50 p-5 rounded-3xl border border-slate-100 space-y-3 shadow-xs">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <span className="text-xs uppercase font-extrabold tracking-wider text-emerald-600 flex items-center gap-1.5">
            <MessageSquare className="w-4 h-4" /> Đóng vai hội thoại tương tác (Role-play)
          </span>

          <div className="flex items-center gap-1.5">
            <Badge variant="outline" className="bg-white">
              Lượt nói: {learnerTurnsCount}/{maxTurns}
            </Badge>
          </div>
        </div>

        {content.scenario && (
          <p className="text-xs text-slate-700 leading-relaxed font-medium bg-white/80 p-3 rounded-xl border border-slate-200/80">
            <span className="font-bold text-slate-900">Tình huống:</span> {content.scenario}
          </p>
        )}

        <div className="flex flex-wrap items-center gap-4 text-xs font-semibold">
          <span className="text-emerald-700">
            Bạn đóng vai: <strong className="text-slate-900">{content.learnerRole ?? 'Học viên'}</strong>
          </span>
          <span className="text-sky-700">
            AI đóng vai: <strong className="text-slate-900">{content.aiRole ?? 'Đối tác hội thoại'}</strong>
          </span>
        </div>
      </div>

      {/* Chat Messages Window */}
      <div className="min-h-[280px] max-h-[420px] overflow-y-auto p-4 rounded-3xl border border-slate-200 bg-white shadow-2xs space-y-3">
        {messages.map((msg, idx) => {
          const isAi = msg.role === 'ai';
          return (
            <div
              key={idx}
              className={cn('flex items-start gap-2.5', isAi ? 'justify-start' : 'justify-end')}
            >
              {isAi && (
                <div className="w-7 h-7 rounded-full bg-sky-100 border border-sky-200 text-sky-700 flex items-center justify-center shrink-0 mt-0.5 shadow-2xs">
                  <Bot className="w-4 h-4" />
                </div>
              )}

              <div
                className={cn(
                  'max-w-[80%] p-3.5 rounded-2xl text-xs sm:text-sm font-medium leading-relaxed shadow-2xs',
                  isAi
                    ? 'bg-slate-50 text-slate-900 border border-slate-200/90 rounded-tl-sm'
                    : 'bg-emerald-600 text-white rounded-tr-sm'
                )}
              >
                {msg.text}
              </div>

              {!isAi && (
                <div className="w-7 h-7 rounded-full bg-emerald-100 border border-emerald-200 text-emerald-800 flex items-center justify-center shrink-0 mt-0.5 shadow-2xs">
                  <User className="w-4 h-4" />
                </div>
              )}
            </div>
          );
        })}
      </div>

      {/* Input box */}
      {!isSubmitted && (
        <div className="space-y-3">
          <div className="flex items-center gap-2">
            <input
              type="text"
              placeholder={`Trả lời với tư cách ${content.learnerRole ?? 'học viên'}...`}
              value={inputText}
              onChange={(e) => setInputText(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter') {
                  e.preventDefault();
                  handleSendTurn();
                }
              }}
              className="flex-1 text-xs sm:text-sm rounded-2xl border border-slate-200 bg-white p-3.5 focus:outline-none focus:ring-2 focus:ring-emerald-500 shadow-xs"
            />
            <Button
              type="button"
              variant="primary"
              size="md"
              onClick={handleSendTurn}
              disabled={!inputText.trim()}
              className="rounded-2xl px-4 py-3 shrink-0"
            >
              <Send className="w-4 h-4" />
            </Button>
          </div>

          <div className="pt-1">
            <Button
              type="button"
              variant="outline"
              onClick={handleCompleteSession}
              disabled={learnerTurnsCount === 0 || isSubmitting}
              className="w-full gap-2 py-3 rounded-2xl border-emerald-600/30 text-emerald-700 hover:bg-emerald-50/60 font-semibold"
            >
              <Check className="w-4 h-4" />
              {isSubmitting ? 'AI đang chấm điểm rubric hội thoại...' : 'Kết thúc hội thoại & Nộp bài chấm'}
            </Button>
          </div>
        </div>
      )}
    </div>
  );
};
