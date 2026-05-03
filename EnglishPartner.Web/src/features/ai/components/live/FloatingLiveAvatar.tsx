'use client'

import { useState, useEffect } from 'react'
import { Phone, Plus, X, PhoneCall } from 'lucide-react'
import { motion, AnimatePresence } from 'motion/react'
import { LiveCallArea } from './LiveCallArea'
import { useChatSessions } from '../../api/useChat'
import { useTopics } from '../../../curriculum/api/useCurriculum'
import { useAuthStore } from '@/lib/zustand/auth.store'

type View = 'sessionList' | 'newCallPrompt' | 'activeCall'

export function FloatingLiveAvatar() {
  const [isOpen, setIsOpen] = useState(false)
  const [view, setView] = useState<View>('sessionList')
  const [activeSessionId, setActiveSessionId] = useState<string | null>(null)
  const [topicInput, setTopicInput] = useState('')
  const { data: sessions, isLoading: loadingSessions } = useChatSessions()
  
  const { user, fetchProfile } = useAuthStore()
  
  useEffect(() => {
    if (user && !user.targetLanguage) {
      fetchProfile()
    }
  }, [user, fetchProfile])

  const languageCode = user?.targetLanguage || ''
  const { data: topicsData } = useTopics({ languageCode }, { enabled: !!user?.targetLanguage })
  const topics = Array.isArray(topicsData) ? topicsData : (topicsData as any)?.items || []

  // Filter only LiveAudio sessions
  const liveSessions = sessions?.filter((s: any) => s.sessionType === 'LiveAudio') || []

  // When opened with no live sessions, go straight to new call prompt
  useEffect(() => {
    if (isOpen && view === 'sessionList' && !loadingSessions && liveSessions.length === 0) {
      setView('newCallPrompt')
    }
  }, [isOpen, view, loadingSessions, liveSessions.length])

  const handleStartNewCall = () => {
    setActiveSessionId(null) // Hub will create a new session
    setView('activeCall')
  }

  const handleContinueSession = (sessionId: string) => {
    setActiveSessionId(sessionId) // Hub will continue existing session with history
    setView('activeCall')
  }

  const handleCallClosed = () => {
    setActiveSessionId(null)
    setView('sessionList')
  }

  return (
    <>
      <div className="fixed bottom-24 right-6 z-[100] flex flex-col items-end">
        <AnimatePresence>
          {isOpen && (
            <motion.div
              initial={{ opacity: 0, y: 20, scale: 0.95 }}
              animate={{ opacity: 1, y: 0, scale: 1 }}
              exit={{ opacity: 0, y: 20, scale: 0.95 }}
              transition={{ duration: 0.2 }}
              className="mb-4 bg-white dark:bg-[#1e1e1e] rounded-2xl shadow-2xl border border-emerald-100 dark:border-emerald-900/50 w-[360px] sm:w-[400px] h-[600px] max-h-[80vh] flex flex-col overflow-hidden"
            >
              {view === 'newCallPrompt' ? (
                <div className="flex-1 flex flex-col items-center justify-center p-6 text-center space-y-4 bg-emerald-50/50 dark:bg-emerald-950/20">
                  <div className="bg-emerald-100 dark:bg-emerald-900/30 text-emerald-600 dark:text-emerald-400 p-4 rounded-full mb-2">
                    <PhoneCall className="h-8 w-8" />
                  </div>
                  <h3 className="font-semibold text-lg text-[var(--color-text-primary)]">Start Live Voice Call</h3>
                  <p className="text-sm text-[var(--color-text-muted)]">What would you like to practice speaking about?</p>
                  
                  <div className="w-full space-y-3 mt-4 text-left">
                    <input 
                      type="text" 
                      id="liveTopicInput"
                      list="liveTopicSuggestions"
                      value={topicInput}
                      onChange={e => setTopicInput(e.target.value)}
                      placeholder="e.g. Job Interview Practice"
                      className="w-full px-4 py-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] text-sm focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500 focus:outline-none transition-all text-[var(--color-text-primary)]"
                      onKeyDown={e => {
                        if (e.key === 'Enter' && topicInput.trim()) {
                          handleStartNewCall()
                        }
                      }}
                    />
                    
                    {topics && topics.length > 0 && (
                      <datalist id="liveTopicSuggestions">
                        {topics.map((t: any) => (
                          <option key={t.id} value={t.title} />
                        ))}
                      </datalist>
                    )}

                    <div className="flex gap-2 pt-2">
                      {liveSessions.length > 0 && (
                        <button 
                          onClick={() => setView('sessionList')}
                          className="flex-1 py-3 bg-[var(--color-surface)] text-[var(--color-text-primary)] rounded-xl font-medium hover:bg-gray-100 dark:hover:bg-gray-800 transition-all text-sm border border-[var(--color-border)]"
                        >
                          Cancel
                        </button>
                      )}
                      <button 
                        onClick={handleStartNewCall}
                        className="flex-1 py-3 bg-gradient-to-r from-emerald-500 to-teal-600 text-white rounded-xl font-medium hover:shadow-md transition-all text-sm"
                      >
                        Start Call
                      </button>
                    </div>
                  </div>
                </div>

              ) : view === 'sessionList' ? (
                <div className="flex-1 flex flex-col bg-[var(--color-surface)] h-full overflow-hidden">
                  <div className="h-14 border-b border-[var(--color-border)] flex items-center justify-between px-4 shrink-0 shadow-sm z-10">
                    <div className="flex items-center gap-2 text-[var(--color-text-primary)] font-semibold text-sm">
                      <PhoneCall className="h-4 w-4 text-emerald-500" />
                      Live Sessions
                    </div>
                    <div className="flex gap-1.5">
                      <button onClick={() => setView('newCallPrompt')} className="text-xs flex items-center gap-1 text-emerald-600 bg-emerald-50 dark:bg-emerald-900/30 px-2.5 py-1.5 rounded-md hover:bg-emerald-100 dark:hover:bg-emerald-900/50 transition-colors font-medium">
                        <Plus className="h-3 w-3"/> New
                      </button>
                      <button onClick={() => setIsOpen(false)} className="p-1.5 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-md text-[var(--color-text-muted)] transition-colors">
                         <X className="h-4 w-4"/>
                      </button>
                    </div>
                  </div>
                  
                  <div className="flex-1 overflow-y-auto p-3 space-y-2 scrollbar-thin">
                    {loadingSessions ? (
                      <div className="text-center text-sm text-[var(--color-text-muted)] py-10 animate-pulse">Loading sessions...</div>
                    ) : liveSessions.length === 0 ? (
                      <div className="text-center text-sm text-[var(--color-text-muted)] py-10">No live sessions yet.</div>
                    ) : (
                      liveSessions.map((s: any) => (
                        <button 
                          key={s.id} 
                          onClick={() => handleContinueSession(s.id)} 
                          className="w-full text-left p-4 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-2)] hover:border-emerald-300 dark:hover:border-emerald-700 hover:shadow-sm transition-all group flex flex-col gap-1.5"
                        >
                          <div className="font-semibold text-sm text-[var(--color-text-primary)] group-hover:text-emerald-600 dark:group-hover:text-emerald-400 transition-colors line-clamp-1">
                            {s.title || 'Live Session'}
                          </div>
                          <div className="flex items-center gap-2">
                            <span className="text-xs text-[var(--color-text-muted)] font-medium">
                              {new Date(s.createdAt).toLocaleDateString(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })}
                            </span>
                            {s.voiceName && (
                              <span className="text-xs bg-emerald-50 dark:bg-emerald-950/30 text-emerald-600 dark:text-emerald-400 px-1.5 py-0.5 rounded font-medium">
                                {s.voiceName}
                              </span>
                            )}
                          </div>
                        </button>
                      ))
                    )}
                  </div>
                </div>

              ) : (
                <LiveCallArea 
                  sessionId={activeSessionId} 
                  onClose={handleCallClosed}
                />
              )}
            </motion.div>
          )}
        </AnimatePresence>

        <button
          onClick={() => setIsOpen(!isOpen)}
          className={`h-14 w-14 rounded-full shadow-xl flex items-center justify-center transition-transform hover:scale-105 active:scale-95 ${
            isOpen ? 'bg-gray-800 text-white dark:bg-gray-700' : 'bg-gradient-to-br from-emerald-500 to-teal-600 text-white'
          }`}
        >
          {isOpen ? <X className="h-6 w-6" /> : <Phone className="h-6 w-6" />}
        </button>
      </div>
    </>
  )
}
