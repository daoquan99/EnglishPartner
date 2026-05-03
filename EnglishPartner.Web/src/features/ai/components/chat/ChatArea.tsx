'use client'

import { useState, useRef, useEffect } from 'react'
import { Bot, Volume2, Plus } from 'lucide-react'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/ui/select'
import { useChatMessages, useSendChatMessage } from '../../api/useChat'
import { useTtsVoices } from '../../api/useAi'
import { ChatMessageList } from './ChatMessageList'
import { ChatInput } from './ChatInput'

interface ChatAreaProps {
  sessionId: string
  onClose?: () => void
  onNewChat?: () => void
  onHistory?: () => void
}

export function ChatArea({ sessionId, onClose, onNewChat, onHistory }: ChatAreaProps) {
  const { data: sessionData, isLoading } = useChatMessages(sessionId)
  const { mutateAsync: sendMessage, isPending: isSending } = useSendChatMessage()
  const { data: voices } = useTtsVoices()

  const [selectedVoice, setSelectedVoice] = useState('en-US-Neural2-C')
  const [speakingRate, setSpeakingRate] = useState(1.0)
  const [autoPlayAudio, setAutoPlayAudio] = useState(true)

  const handleSendText = async (text: string) => {
    if (!text.trim()) return
    await sendMessage({ sessionId, text })
  }

  return (
    <div className="flex flex-col h-full bg-[var(--color-surface)]">
      {/* Header */}
      <div className="h-14 shrink-0 border-b border-[var(--color-border)] px-3 flex items-center justify-between bg-[var(--color-surface)] shadow-sm z-10 gap-2">
        <div className="flex items-center gap-1.5 shrink-0">
          {onHistory && (
            <button onClick={onHistory} title="Chat History" className="p-1.5 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-md text-[var(--color-text-muted)] transition-colors mr-1">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><line x1="8" y1="6" x2="21" y2="6"></line><line x1="8" y1="12" x2="21" y2="12"></line><line x1="8" y1="18" x2="21" y2="18"></line><line x1="3" y1="6" x2="3.01" y2="6"></line><line x1="3" y1="12" x2="3.01" y2="12"></line><line x1="3" y1="18" x2="3.01" y2="18"></line></svg>
            </button>
          )}
          <div className="h-7 w-7 bg-violet-100 dark:bg-violet-900/30 text-violet-600 dark:text-violet-400 rounded-full flex items-center justify-center">
            <Bot className="h-3.5 w-3.5" />
          </div>
          <span className="font-semibold text-[var(--color-text-primary)] text-sm">Tutor</span>
          {onNewChat && (
            <button onClick={onNewChat} title="New Chat" className="text-xs flex items-center gap-1 text-violet-600 dark:text-violet-400 hover:bg-violet-50 dark:hover:bg-violet-900/20 px-1.5 py-1 rounded-md ml-1 transition-colors">
              <Plus className="h-3.5 w-3.5" />
            </button>
          )}
        </div>
        
        <div className="flex-1 flex items-center gap-2 overflow-x-auto [&::-webkit-scrollbar]:hidden [-ms-overflow-style:none] [scrollbar-width:none]">
          <button 
            onClick={() => setAutoPlayAudio(!autoPlayAudio)}
            title="Auto-play audio"
            className={`flex items-center justify-center h-8 w-8 rounded-md transition-colors shrink-0 ml-auto ${
              autoPlayAudio 
                ? 'bg-violet-100 text-violet-700 dark:bg-violet-500/20 dark:text-violet-300' 
                : 'text-[var(--color-text-muted)] hover:bg-[var(--color-surface-2)]'
            }`}
          >
            <Volume2 className="h-4 w-4" />
          </button>

          <Select value={selectedVoice} onValueChange={setSelectedVoice}>
            <SelectTrigger className="h-8 w-[100px] sm:w-[120px] text-xs bg-[var(--color-surface-2)] border-0 shrink-0 px-2">
              <SelectValue placeholder="Voice" />
            </SelectTrigger>
            <SelectContent className="z-[110]">
              {voices?.map(v => (
                <SelectItem key={v.id} value={v.id} className="text-xs">
                  {v.displayName.split(' ')[0]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Select value={speakingRate.toFixed(1)} onValueChange={(val) => setSpeakingRate(parseFloat(val))}>
            <SelectTrigger className="h-8 w-[70px] text-xs bg-[var(--color-surface-2)] border-0 shrink-0 px-2">
              <SelectValue />
            </SelectTrigger>
            <SelectContent className="z-[110]">
              {Array.from({ length: 17 }, (_, i) => 0.4 + i * 0.1).map(rate => {
                const val = rate.toFixed(1)
                return <SelectItem key={val} value={val} className="text-xs">{val}x</SelectItem>
              })}
            </SelectContent>
          </Select>
        </div>

        {onClose && (
          <div className="shrink-0 pl-1 border-l border-[var(--color-border)] ml-1">
            <button onClick={onClose} className="p-1.5 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-md text-[var(--color-text-muted)]">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M18 6 6 18"/><path d="m6 6 12 12"/></svg>
            </button>
          </div>
        )}
      </div>

      {/* Message List */}
      <div className="flex-1 overflow-hidden relative bg-[url('data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAiIGhlaWdodD0iMjAiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyI+PGNpcmNsZSBjeD0iMSIgY3k9IjEiIHI9IjEiIGZpbGw9InJnYmEoMTUwLDE1MCwxNTAsMC4wNSkiLz48L3N2Zz4=')]">
        {isLoading ? (
          <div className="absolute inset-0 flex items-center justify-center text-sm text-[var(--color-text-muted)]">Loading messages...</div>
        ) : (
          <ChatMessageList 
            sessionId={sessionId}
            messages={sessionData?.messages ?? []} 
            isSending={isSending}
            selectedVoice={selectedVoice}
            speakingRate={speakingRate}
            autoPlayAudio={autoPlayAudio}
          />
        )}
      </div>

      {/* Input Area */}
      <div className="shrink-0 p-4 bg-[var(--color-surface)] border-t border-[var(--color-border)] z-10">
        <ChatInput onSend={handleSendText} isSending={isSending} />
      </div>
    </div>
  )
}
