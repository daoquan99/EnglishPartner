'use client'

import { useState } from 'react'
import { Plus, MessageSquare, Bot } from 'lucide-react'
import { Button } from '@/ui/button'
import { useChatSessions, useCreateChatSession } from '@/features/ai/api/useChat'
import { ChatArea } from '@/features/ai/components/chat/ChatArea'

export default function AiChatPage() {
  const { data: sessions, isLoading: loadingSessions } = useChatSessions()
  const { mutateAsync: createSession, isPending: creating } = useCreateChatSession()
  const [activeSessionId, setActiveSessionId] = useState<string | null>(null)

  const handleNewChat = async () => {
    try {
      const { sessionId } = await createSession({ title: 'New Conversation' })
      setActiveSessionId(sessionId)
    } catch {}
  }

  return (
    <div className="h-[calc(100vh-6rem)] flex bg-[var(--color-surface)] rounded-2xl border border-[var(--color-border)] shadow-sm overflow-hidden">
      {/* Sidebar */}
      <div className="w-64 sm:w-72 border-r border-[var(--color-border)] bg-[var(--color-surface-2)] flex flex-col shrink-0">
        <div className="p-4 border-b border-[var(--color-border)]">
          <Button 
            onClick={handleNewChat} 
            disabled={creating}
            className="w-full bg-gradient-to-r from-violet-500 to-indigo-600 hover:shadow-md text-white transition-all shadow-sm"
          >
            <Plus className="h-4 w-4 mr-2" />
            New Chat
          </Button>
        </div>
        
        <div className="flex-1 overflow-y-auto p-3 space-y-1">
          <p className="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider px-2 mb-2 mt-1">History</p>
          {loadingSessions ? (
            <div className="px-2 py-4 text-center text-sm text-[var(--color-text-muted)] animate-pulse">Loading...</div>
          ) : sessions?.length === 0 ? (
            <div className="px-2 py-4 text-center text-sm text-[var(--color-text-muted)]">No previous chats</div>
          ) : (
            sessions?.map(session => (
              <button
                key={session.id}
                onClick={() => setActiveSessionId(session.id)}
                className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm text-left transition-colors ${
                  activeSessionId === session.id 
                    ? 'bg-violet-100 text-violet-900 dark:bg-violet-500/20 dark:text-violet-100 font-medium' 
                    : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-surface)] hover:text-[var(--color-text-primary)]'
                }`}
              >
                <MessageSquare className="h-4 w-4 shrink-0 opacity-70" />
                <span className="truncate">{session.title}</span>
              </button>
            ))
          )}
        </div>
      </div>

      {/* Main Area */}
      <div className="flex-1 flex flex-col min-w-0 bg-white dark:bg-[#121212]">
        {activeSessionId ? (
          <ChatArea sessionId={activeSessionId} />
        ) : (
          <div className="flex-1 flex flex-col items-center justify-center text-center p-6">
            <div className="h-16 w-16 bg-gradient-to-br from-violet-100 to-indigo-100 dark:from-violet-900/30 dark:to-indigo-900/30 rounded-full flex items-center justify-center mb-4">
              <Bot className="h-8 w-8 text-violet-600 dark:text-violet-400" />
            </div>
            <h2 className="text-xl font-bold text-[var(--color-text-primary)] mb-2">AI Tutor Chat</h2>
            <p className="text-[var(--color-text-secondary)] max-w-md">
              Start a new conversation or select an existing one to practice English with your personal AI assistant.
            </p>
          </div>
        )}
      </div>
    </div>
  )
}
