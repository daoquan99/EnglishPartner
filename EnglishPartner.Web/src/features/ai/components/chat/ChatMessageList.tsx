'use client'

import { useEffect, useRef, useState } from 'react'
import { motion } from 'motion/react'
import { Languages, Loader2, Play, User, Bot } from 'lucide-react'
import type { ChatMessage } from '../../types/ai.types'
import { useTts, useTranslate } from '../../api/useAi'
import { useUploadMessageAudio, useUpdateMessageTranslation } from '../../api/useChat'
import { useAuthStore } from '@/lib/zustand/auth.store'

interface ChatMessageListProps {
  sessionId: string
  messages: ChatMessage[]
  isSending: boolean
  selectedVoice: string
  speakingRate: number
  autoPlayAudio: boolean
}

export function ChatMessageList({ sessionId, messages, isSending, selectedVoice, speakingRate, autoPlayAudio }: ChatMessageListProps) {
  const bottomRef = useRef<HTMLDivElement>(null)
  const [playingId, setPlayingId] = useState<string | null>(null)
  const [translatingId, setTranslatingId] = useState<string | null>(null)
  const { mutateAsync: tts } = useTts()
  const { mutateAsync: uploadAudio } = useUploadMessageAudio()
  const { mutateAsync: translateText } = useTranslate()
  const { mutateAsync: updateTranslation } = useUpdateMessageTranslation()
  const { user } = useAuthStore()
  const audioCacheRef = useRef<Map<string, string>>(new Map())
  const lastMessageCount = useRef(messages.length)

  // Scroll to bottom
  useEffect(() => {
    if (bottomRef.current) {
      bottomRef.current.scrollIntoView({ behavior: 'smooth' })
    }
  }, [messages, isSending])

  // Handle auto-play for new AI messages
  useEffect(() => {
    if (autoPlayAudio && messages.length > lastMessageCount.current) {
      const lastMsg = messages[messages.length - 1]
      if (lastMsg.role !== 'user') {
        playAudio(lastMsg)
      }
    }
    lastMessageCount.current = messages.length
  }, [messages, autoPlayAudio])

  const playAudio = async (msg: ChatMessage) => {
    if (playingId) return // Already playing something
    
    setPlayingId(msg.id)
    try {
      let url = msg.audioUrl || audioCacheRef.current.get(msg.id)
      if (!url) {
        const blob = await tts({ text: msg.text, voice: selectedVoice, speakingRate })
        url = URL.createObjectURL(blob)
        audioCacheRef.current.set(msg.id, url)
        
        // Background upload
        uploadAudio({ sessionId, messageId: msg.id, audioFile: blob }).catch(() => {})
      }
      
      const audio = new Audio(url)
      audio.onended = () => setPlayingId(null)
      audio.onerror = () => setPlayingId(null)
      audio.play().catch(e => {
        console.error('Autoplay blocked or failed:', e)
        setPlayingId(null)
      })
    } catch {
      setPlayingId(null)
    }
  }

  const translateMessage = async (msg: ChatMessage) => {
    if (translatingId || msg.translation) return
    setTranslatingId(msg.id)
    try {
      const targetLanguageCode = user?.nativeLanguage || 'vi-VN' // Default to Vietnamese if native language is not set
      const result = await translateText({ text: msg.text, targetLanguageCode })
      if (result.translation) {
        await updateTranslation({ sessionId, messageId: msg.id, translation: result.translation })
      }
    } catch (e) {
      console.error("Translation failed", e)
    } finally {
      setTranslatingId(null)
    }
  }

  return (
    <div className="absolute inset-0 overflow-y-auto p-4 sm:p-6 space-y-6">
      {messages.length === 0 ? (
        <div className="h-full flex flex-col items-center justify-center text-[var(--color-text-muted)] text-sm">
          No messages yet. Say hello!
        </div>
      ) : (
        messages.map((msg, idx) => {
          const isAi = msg.role !== 'user'
          return (
            <motion.div
              key={msg.id}
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.3 }}
              className={`flex w-full ${isAi ? 'justify-start' : 'justify-end'}`}
            >
              <div className={`flex max-w-[85%] sm:max-w-[75%] gap-2 ${isAi ? 'flex-row' : 'flex-row-reverse'}`}>
                {/* Avatar */}
                <div className={`shrink-0 h-8 w-8 rounded-full flex items-center justify-center shadow-sm ${
                  isAi 
                    ? 'bg-gradient-to-br from-violet-500 to-indigo-600 text-white' 
                    : 'bg-gradient-to-br from-emerald-500 to-teal-600 text-white'
                }`}>
                  {isAi ? <Bot className="h-4 w-4" /> : <User className="h-4 w-4" />}
                </div>

                {/* Bubble */}
                <div className={`relative px-4 py-3 shadow-sm text-[15px] leading-relaxed border ${
                  isAi
                    ? 'bg-white dark:bg-[var(--color-surface-2)] border-[var(--color-border)] rounded-2xl rounded-tl-sm text-[var(--color-text-primary)]'
                    : 'bg-emerald-50 dark:bg-emerald-950/40 border-emerald-100 dark:border-emerald-900/50 rounded-2xl rounded-tr-sm text-[var(--color-text-primary)]'
                }`}>
                  {msg.text}

                  {msg.translation && (
                    <div className="mt-2 pt-2 border-t border-[var(--color-border)] text-sm text-[var(--color-text-muted)] italic">
                      {msg.translation}
                    </div>
                  )}

                  {isAi && (
                    <div className="mt-2 pt-2 border-t border-[var(--color-border)] flex items-center justify-between">
                      <div className="flex items-center gap-4">
                        <button
                          onClick={() => playAudio(msg)}
                          disabled={playingId !== null}
                          className="text-xs flex items-center gap-1.5 text-violet-600 dark:text-violet-400 hover:text-violet-800 disabled:opacity-50 transition-colors"
                        >
                          {playingId === msg.id ? (
                            <Loader2 className="h-3.5 w-3.5 animate-spin" />
                          ) : (
                            <Play className="h-3.5 w-3.5" />
                          )}
                          Listen
                        </button>
                        
                        {!msg.translation && (
                          <button
                            onClick={() => translateMessage(msg)}
                            disabled={translatingId !== null}
                            className="text-xs flex items-center gap-1.5 text-blue-600 dark:text-blue-400 hover:text-blue-800 disabled:opacity-50 transition-colors"
                          >
                            {translatingId === msg.id ? (
                              <Loader2 className="h-3.5 w-3.5 animate-spin" />
                            ) : (
                              <Languages className="h-3.5 w-3.5" />
                            )}
                            Translate
                          </button>
                        )}
                      </div>
                    </div>
                  )}
                </div>
              </div>
            </motion.div>
          )
        })
      )}

      {isSending && (
        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          className="flex w-full justify-start"
        >
          <div className="flex max-w-[85%] gap-2 flex-row">
            <div className="shrink-0 h-8 w-8 rounded-full bg-gradient-to-br from-violet-500 to-indigo-600 text-white flex items-center justify-center shadow-sm">
              <Bot className="h-4 w-4" />
            </div>
            <div className="bg-white dark:bg-[var(--color-surface-2)] border border-[var(--color-border)] rounded-2xl rounded-tl-sm px-5 py-4 shadow-sm flex items-center gap-1">
              <span className="w-2 h-2 rounded-full bg-violet-400 animate-bounce" style={{ animationDelay: '0ms' }} />
              <span className="w-2 h-2 rounded-full bg-violet-400 animate-bounce" style={{ animationDelay: '150ms' }} />
              <span className="w-2 h-2 rounded-full bg-violet-400 animate-bounce" style={{ animationDelay: '300ms' }} />
            </div>
          </div>
        </motion.div>
      )}

      <div ref={bottomRef} className="h-2" />
    </div>
  )
}
