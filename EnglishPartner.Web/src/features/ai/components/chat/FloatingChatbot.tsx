'use client'

import { useState, useEffect } from 'react'
import { MessageCircle, Plus, X } from 'lucide-react'
import { motion, AnimatePresence } from 'motion/react'
import { ChatArea } from './ChatArea'
import { useChatSessions, useCreateChatSession } from '../../api/useChat'
import { useTopics } from '../../../curriculum/api/useCurriculum'
import { useAuthStore } from '@/lib/zustand/auth.store'

export function FloatingChatbot() {
  const [isOpen, setIsOpen] = useState(false)
  const [activeSessionId, setActiveSessionId] = useState<string | null>(null)
  const [isCreatingNew, setIsCreatingNew] = useState(false)
  const [topicInput, setTopicInput] = useState('')
  const { data: sessions, isLoading: loadingSessions } = useChatSessions()
  const { mutateAsync: createSession, isPending: creating } = useCreateChatSession()
  
  const { user, fetchProfile } = useAuthStore()
  
  useEffect(() => {
    if (user && !user.targetLanguage) {
      fetchProfile()
    }
  }, [user, fetchProfile])

  const languageCode = user?.targetLanguage || ''
  const { data: topicsData } = useTopics({ languageCode }, { enabled: !!user?.targetLanguage })
  
  // Handle paginated response structure if backend wraps it in { items, totalCount }
  const topics = Array.isArray(topicsData) ? topicsData : (topicsData as any)?.items || []

  // Load the most recent session or create one when opened
  useEffect(() => {
    if (isOpen && !activeSessionId && !loadingSessions && !isCreatingNew) {
      if (!sessions || sessions.length === 0) {
        setIsCreatingNew(true)
      }
    }
  }, [isOpen, sessions, loadingSessions, activeSessionId, isCreatingNew])

  const handleStartNew = async (topic: string) => {
    try {
      const { sessionId } = await createSession({ title: topic || 'New Conversation', topic })
      setActiveSessionId(sessionId)
      setIsCreatingNew(false)
      setTopicInput('')
    } catch {}
  }

  return (
    <>
      <div className="fixed bottom-6 right-6 z-[100] flex flex-col items-end">
        <AnimatePresence>
          {isOpen && (
            <motion.div
              initial={{ opacity: 0, y: 20, scale: 0.95 }}
              animate={{ opacity: 1, y: 0, scale: 1 }}
              exit={{ opacity: 0, y: 20, scale: 0.95 }}
              transition={{ duration: 0.2 }}
              className="mb-4 bg-white dark:bg-[#1e1e1e] rounded-2xl shadow-2xl border border-[var(--color-border)] w-[360px] sm:w-[400px] h-[600px] max-h-[80vh] flex flex-col overflow-hidden"
            >
              {isCreatingNew ? (
                <div className="flex-1 flex flex-col items-center justify-center p-6 text-center space-y-4 bg-[var(--color-surface)]">
                  <div className="bg-violet-100 dark:bg-violet-900/30 text-violet-600 dark:text-violet-400 p-4 rounded-full mb-2">
                    <MessageCircle className="h-8 w-8" />
                  </div>
                  <h3 className="font-semibold text-lg text-[var(--color-text-primary)]">Start a New Chat</h3>
                  <p className="text-sm text-[var(--color-text-muted)]">What topic would you like to practice today?</p>
                  
                  <div className="w-full space-y-3 mt-4 text-left">
                    <input 
                      type="text" 
                      id="topicInput"
                      list="topicSuggestions"
                      value={topicInput}
                      onChange={e => setTopicInput(e.target.value)}
                      placeholder="e.g. Ordering food at a restaurant"
                      className="w-full px-4 py-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-2)] text-sm focus:ring-2 focus:ring-violet-500 focus:border-violet-500 focus:outline-none transition-all text-[var(--color-text-primary)]"
                      onKeyDown={e => {
                        if (e.key === 'Enter' && topicInput.trim()) {
                          handleStartNew(topicInput.trim())
                        }
                      }}
                    />
                    
                    {topics && topics.length > 0 && (
                      <datalist id="topicSuggestions">
                        {topics.map((t: any) => (
                          <option key={t.id} value={t.title} />
                        ))}
                      </datalist>
                    )}

                    <div className="flex gap-2 pt-2">
                      {sessions && sessions.length > 0 && (
                        <button 
                          onClick={() => setIsCreatingNew(false)}
                          className="flex-1 py-3 bg-[var(--color-surface-2)] text-[var(--color-text-primary)] rounded-xl font-medium hover:bg-gray-100 dark:hover:bg-gray-800 transition-all text-sm border border-[var(--color-border)]"
                        >
                          Cancel
                        </button>
                      )}
                      <button 
                        onClick={() => {
                          if (topicInput.trim()) handleStartNew(topicInput.trim())
                        }}
                        disabled={creating}
                        className="flex-1 py-3 bg-gradient-to-r from-violet-500 to-indigo-600 text-white rounded-xl font-medium disabled:opacity-50 hover:shadow-md transition-all text-sm"
                      >
                        {creating ? 'Starting...' : 'Start Chat'}
                      </button>
                    </div>
                  </div>
                </div>
              ) : !activeSessionId ? (
                <div className="flex-1 flex flex-col bg-[var(--color-surface)] h-full overflow-hidden">
                  <div className="h-14 border-b border-[var(--color-border)] flex items-center justify-between px-4 shrink-0 shadow-sm z-10">
                    <div className="flex items-center gap-2 text-[var(--color-text-primary)] font-semibold text-sm">
                      <MessageCircle className="h-4 w-4 text-violet-500" />
                      Chat History
                    </div>
                    <div className="flex gap-1.5">
                      <button onClick={() => setIsCreatingNew(true)} className="text-xs flex items-center gap-1 text-violet-600 bg-violet-50 dark:bg-violet-900/30 px-2.5 py-1.5 rounded-md hover:bg-violet-100 dark:hover:bg-violet-900/50 transition-colors font-medium">
                        <Plus className="h-3 w-3"/> New
                      </button>
                      <button onClick={() => setIsOpen(false)} className="p-1.5 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-md text-[var(--color-text-muted)] transition-colors">
                         <X className="h-4 w-4"/>
                      </button>
                    </div>
                  </div>
                  
                  <div className="flex-1 overflow-y-auto p-3 space-y-2 scrollbar-thin">
                    {loadingSessions ? (
                      <div className="text-center text-sm text-[var(--color-text-muted)] py-10 animate-pulse">Loading history...</div>
                    ) : sessions?.length === 0 ? (
                      <div className="text-center text-sm text-[var(--color-text-muted)] py-10">No chat history yet.</div>
                    ) : (
                      sessions?.map(s => (
                        <button 
                          key={s.id} 
                          onClick={() => setActiveSessionId(s.id)} 
                          className="w-full text-left p-4 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-2)] hover:border-violet-300 dark:hover:border-violet-700 hover:shadow-sm transition-all group flex flex-col gap-1.5"
                        >
                          <div className="font-semibold text-sm text-[var(--color-text-primary)] group-hover:text-violet-600 dark:group-hover:text-violet-400 transition-colors line-clamp-1">
                            {s.title || 'Conversation'}
                          </div>
                          <div className="text-xs text-[var(--color-text-muted)] font-medium">
                            {new Date(s.createdAt).toLocaleDateString(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })}
                          </div>
                        </button>
                      ))
                    )}
                  </div>
                </div>
              ) : (
                <ChatArea 
                  sessionId={activeSessionId} 
                  onClose={() => setIsOpen(false)}
                  onNewChat={() => setIsCreatingNew(true)}
                  onHistory={() => {
                    setActiveSessionId(null)
                    setIsCreatingNew(false)
                  }}
                />
              )}
            </motion.div>
          )}
        </AnimatePresence>

        <button
          onClick={() => setIsOpen(!isOpen)}
          className={`h-14 w-14 rounded-full shadow-xl flex items-center justify-center transition-transform hover:scale-105 active:scale-95 ${
            isOpen ? 'bg-gray-800 text-white dark:bg-gray-700' : 'bg-gradient-to-br from-violet-500 to-indigo-600 text-white'
          }`}
        >
          {isOpen ? <X className="h-6 w-6" /> : <MessageCircle className="h-6 w-6" />}
        </button>
      </div>
    </>
  )
}
