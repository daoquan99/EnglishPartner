'use client'

import { useEffect, useRef, useState } from 'react'
import * as signalR from '@microsoft/signalr'
import { Mic, MicOff, PhoneOff, Loader2, Bot, User, Volume2 } from 'lucide-react'
import { motion, AnimatePresence } from 'motion/react'
import { useAuthStore } from '@/lib/zustand/auth.store'
import { useChatMessages } from '../../api/useChat'
import { useLiveVoices } from '../../api/useAi'

interface LiveCallAreaProps {
  sessionId?: string | null
  onClose: () => void
}

interface LiveMessage {
  id: string
  role: 'user' | 'ai'
  text: string
  isFinal: boolean
}

export function LiveCallArea({ sessionId, onClose }: LiveCallAreaProps) {
  const { data: sessionData, isLoading } = useChatMessages(sessionId ?? null)
  const { accessToken, user } = useAuthStore()
  const { data: liveVoices, isLoading: voicesLoading } = useLiveVoices()

  const [selectedVoice, setSelectedVoice] = useState<string | null>(null)
  const [callStarted, setCallStarted] = useState(false)
  const [connectionState, setConnectionState] = useState<'connecting' | 'connected' | 'disconnected' | 'error'>('connecting')
  const [isMuted, setIsMuted] = useState(false)
  const [messages, setMessages] = useState<LiveMessage[]>([])
  
  const connectionRef = useRef<signalR.HubConnection | null>(null)
  const audioContextRef = useRef<AudioContext | null>(null)
  const playContextRef = useRef<AudioContext | null>(null)
  const streamRef = useRef<MediaStream | null>(null)
  const processorRef = useRef<ScriptProcessorNode | null>(null)
  const nextStartTimeRef = useRef<number>(0)
  const isMutedRef = useRef(false)
  const messagesEndRef = useRef<HTMLDivElement>(null)

  // Scroll to bottom when messages change
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  // Disconnect everything
  const cleanup = () => {
    if (processorRef.current) {
      processorRef.current.disconnect()
      processorRef.current.onaudioprocess = null
      processorRef.current = null
    }
    if (streamRef.current) {
      streamRef.current.getTracks().forEach(t => t.stop())
      streamRef.current = null
    }
    if (audioContextRef.current) {
      audioContextRef.current.close()
      audioContextRef.current = null
    }
    if (playContextRef.current) {
      playContextRef.current.close()
      playContextRef.current = null
    }
    if (connectionRef.current) {
      connectionRef.current.stop()
      connectionRef.current = null
    }
  }

  // Pre-load existing messages from DB when continuing a session
  useEffect(() => {
    if (sessionData?.messages && sessionData.messages.length > 0 && messages.length === 0) {
      setMessages(
        sessionData.messages.map((m) => ({
          id: m.id,
          role: m.role === 'user' ? 'user' as const : 'ai' as const,
          text: m.text,
          isFinal: true,
        }))
      )
    }
  }, [sessionData?.messages])

  useEffect(() => {
    if (!accessToken || !callStarted) return

    const setupConnection = async () => {
      try {
        setConnectionState('connecting')
        
        // Setup Playback Context
        const playCtx = new (window.AudioContext || (window as any).webkitAudioContext)({ sampleRate: 24000 })
        playContextRef.current = playCtx
        nextStartTimeRef.current = playCtx.currentTime

        // Connect to SignalR with language, voice, and optional sessionId for continuation
        const apiUrl = process.env.NEXT_PUBLIC_API_URL;
        if (!apiUrl) {
            throw new Error('NEXT_PUBLIC_API_URL is not defined')
        }
        const targetLanguage = encodeURIComponent(user?.targetLanguage || 'English')
        const voiceParam = selectedVoice ? `&voiceName=${encodeURIComponent(selectedVoice)}` : ''
        const sessionParam = sessionId ? `&sessionId=${encodeURIComponent(sessionId)}` : ''
        const systemPromptParam = sessionData?.systemPrompt ? `&systemPrompt=${encodeURIComponent(sessionData.systemPrompt)}` : ''
        
        const connection = new signalR.HubConnectionBuilder()
          .withUrl(`${apiUrl}/hubs/ai-live-avatar?access_token=${accessToken}&targetLanguage=${targetLanguage}${voiceParam}${sessionParam}${systemPromptParam}`)
          .withAutomaticReconnect()
          .build()

        connectionRef.current = connection

        // Connection lifecycle events
        connection.onreconnecting(() => setConnectionState('connecting'))
        connection.onreconnected(() => setConnectionState('connected'))
        connection.onclose(() => setConnectionState('disconnected'))
        connection.on('SessionError', (msg: string) => {
          console.error('[LiveAudio] Session error:', msg)
          setConnectionState('error')
        })

        connection.on('AudioChunkReceived', (base64Audio: string) => {
          if (!playContextRef.current) return
          const binaryString = window.atob(base64Audio)
          const len = binaryString.length
          const bytes = new Uint8Array(len)
          for (let i = 0; i < len; i++) {
              bytes[i] = binaryString.charCodeAt(i)
          }

          const int16Array = new Int16Array(bytes.buffer)
          const float32Array = new Float32Array(int16Array.length)
          for (let i = 0; i < int16Array.length; i++) {
            float32Array[i] = int16Array[i] / 32768.0
          }

          const audioBuffer = playContextRef.current.createBuffer(1, float32Array.length, 24000)
          audioBuffer.getChannelData(0).set(float32Array)
          
          const source = playContextRef.current.createBufferSource()
          source.buffer = audioBuffer
          source.connect(playContextRef.current.destination)
          
          if (nextStartTimeRef.current < playContextRef.current.currentTime) {
              nextStartTimeRef.current = playContextRef.current.currentTime
          }
          source.start(nextStartTimeRef.current)
          nextStartTimeRef.current += audioBuffer.duration
        })

        const handleAiText = (text: string) => {
          setMessages(prev => {
            const newMsgs = [...prev]
            const lastUserMsg = newMsgs.findLast(m => m.role === 'user')
            if (lastUserMsg && !lastUserMsg.isFinal) {
              lastUserMsg.isFinal = true
            }
            const lastMsg = newMsgs[newMsgs.length - 1]
            if (lastMsg && lastMsg.role === 'ai' && !lastMsg.isFinal) {
              lastMsg.text += text
            } else {
              newMsgs.push({ id: Math.random().toString(), role: 'ai', text, isFinal: false })
            }
            return newMsgs
          })
        }

        connection.on('OutputTranscriptionReceived', handleAiText)
        connection.on('TextChunkReceived', handleAiText)

        connection.on('InputTranscriptionReceived', (text: string) => {
          setMessages(prev => {
            const newMsgs = [...prev]
            const lastMsg = newMsgs[newMsgs.length - 1]
            if (lastMsg && lastMsg.role === 'user' && !lastMsg.isFinal) {
              lastMsg.text += text
            } else {
              newMsgs.push({ id: Math.random().toString(), role: 'user', text, isFinal: false })
            }
            return newMsgs
          })
        })

        connection.on('TurnComplete', () => {
          setMessages(prev => {
            const newMsgs = [...prev]
            const lastMsg = newMsgs[newMsgs.length - 1]
            if (lastMsg && lastMsg.role === 'ai') {
              lastMsg.isFinal = true
            }
            return newMsgs
          })
        })

        // Wait for Gemini session to be ready before sending initial greeting
        connection.on('SessionReady', () => {
          console.log('[LiveAudio] Session ready — sending initial greeting')
          connection.invoke('SendTextMessage', "Hello! Let's start the conversation.").catch(console.error)
        })

        await connection.start()
        setConnectionState('connected')

        // Setup Microphone
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true })
        streamRef.current = stream
        
        const audioCtx = new (window.AudioContext || (window as any).webkitAudioContext)({ sampleRate: 16000 })
        audioContextRef.current = audioCtx
        const source = audioCtx.createMediaStreamSource(stream)
        
        const processor = audioCtx.createScriptProcessor(4096, 1, 1)
        processorRef.current = processor
        
        source.connect(processor)
        processor.connect(audioCtx.destination)
        
        processor.onaudioprocess = (e) => {
          if (!connectionRef.current || connectionRef.current.state !== signalR.HubConnectionState.Connected) return
          if (isMutedRef.current) return
          
          const inputData = e.inputBuffer.getChannelData(0)
          const pcm16 = new Int16Array(inputData.length)
          for (let i = 0; i < inputData.length; i++) {
            const s = Math.max(-1, Math.min(1, inputData[i]))
            pcm16[i] = s < 0 ? s * 0x8000 : s * 0x7FFF
          }
          
          // Convert to Base64 for SignalR JSON protocol compatibility
          const bytes = new Uint8Array(pcm16.buffer)
          let binary = ''
          for (let i = 0; i < bytes.length; i++) {
            binary += String.fromCharCode(bytes[i])
          }
          const base64 = window.btoa(binary)
          connectionRef.current.invoke('SendAudioChunk', base64).catch(err => console.error(err))
        }

      } catch (err) {
        console.error('Failed to start Live Audio:', err)
        setConnectionState('error')
      }
    }

    setupConnection()

    return () => {
      cleanup()
    }
  }, [accessToken, callStarted, selectedVoice, sessionId])

  const toggleMute = () => {
    if (streamRef.current) {
      streamRef.current.getAudioTracks().forEach(t => {
        t.enabled = !t.enabled
      })
      const newMuted = !isMuted
      setIsMuted(newMuted)
      isMutedRef.current = newMuted
    }
  }

  const handleEndCall = () => {
    cleanup()
    onClose()
  }

  if (voicesLoading || (sessionId && isLoading)) {
    return (
      <div className="flex-1 flex flex-col items-center justify-center p-6 bg-[var(--color-surface)] h-full">
        <Loader2 className="h-8 w-8 text-emerald-500 animate-spin mb-4" />
        <p className="text-sm text-[var(--color-text-muted)]">Loading session...</p>
      </div>
    )
  }

  // Voice selection screen (before call starts)
  if (!callStarted) {
    return (
      <div className="flex-1 flex flex-col bg-[var(--color-surface)] h-full">
        {/* Header */}
        <div className="h-14 flex items-center justify-between px-4 shrink-0 shadow-sm border-b border-[var(--color-border)] bg-[var(--color-surface)]">
          <div className="font-semibold text-sm text-[var(--color-text-primary)]">
            Choose a Voice
          </div>
          <button
            onClick={onClose}
            className="text-xs text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] transition-colors"
          >
            Cancel
          </button>
        </div>

        {/* Voice Grid */}
        <div className="flex-1 overflow-y-auto p-4">
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
            {liveVoices?.map((voice) => (
              <button
                key={voice.id}
                onClick={() => setSelectedVoice(voice.id)}
                className={`p-4 rounded-xl border-2 transition-all text-left ${
                  selectedVoice === voice.id
                    ? 'border-emerald-500 bg-emerald-50 dark:bg-emerald-950/30 shadow-md'
                    : 'border-[var(--color-border)] bg-[var(--color-surface-2)] hover:border-emerald-300 dark:hover:border-emerald-700'
                }`}
              >
                <div className="flex items-center gap-2 mb-1">
                  <Volume2 className={`h-4 w-4 ${
                    selectedVoice === voice.id ? 'text-emerald-500' : 'text-[var(--color-text-muted)]'
                  }`} />
                  <span className="font-medium text-sm text-[var(--color-text-primary)]">
                    {voice.displayName}
                  </span>
                </div>
                <span className="text-xs text-[var(--color-text-muted)] capitalize">
                  {voice.gender?.toLowerCase()}
                </span>
              </button>
            ))}
          </div>
        </div>

        {/* Start Call Button */}
        <div className="p-4 border-t border-[var(--color-border)] bg-[var(--color-surface)]">
          <button
            onClick={() => setCallStarted(true)}
            disabled={!selectedVoice}
            className={`w-full py-3 rounded-xl font-medium text-sm transition-all ${
              selectedVoice
                ? 'bg-emerald-500 hover:bg-emerald-600 text-white shadow-lg hover:shadow-xl active:scale-[0.98]'
                : 'bg-gray-200 dark:bg-gray-800 text-gray-400 cursor-not-allowed'
            }`}
          >
            {selectedVoice ? `Start Call with ${liveVoices?.find(v => v.id === selectedVoice)?.displayName}` : 'Select a voice to start'}
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="flex-1 flex flex-col bg-[var(--color-surface)] h-full relative overflow-hidden">
      {/* Header */}
      <div className="h-14 flex items-center justify-between px-4 shrink-0 shadow-sm z-10 border-b border-[var(--color-border)] bg-[var(--color-surface)]">
        <div className="font-semibold text-sm text-[var(--color-text-primary)] truncate max-w-[200px]">
          {sessionData?.title || 'Live Voice Call'}
        </div>
        <div className={`text-xs px-2 py-1 rounded-full font-medium ${
          connectionState === 'connected' ? 'bg-emerald-500/20 text-emerald-600 dark:text-emerald-400' :
          connectionState === 'connecting' ? 'bg-amber-500/20 text-amber-600 dark:text-amber-400 animate-pulse' :
          'bg-red-500/20 text-red-600 dark:text-red-400'
        }`}>
          {connectionState === 'connected' ? 'Connected' : connectionState === 'connecting' ? 'Connecting...' : 'Disconnected'}
        </div>
      </div>

      {/* Messages Area */}
      <div className="flex-1 overflow-y-auto p-4 space-y-4 bg-[url('data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAiIGhlaWdodD0iMjAiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyI+PGNpcmNsZSBjeD0iMSIgY3k9IjEiIHI9IjEiIGZpbGw9InJnYmEoMTUwLDE1MCwxNTAsMC4wNSkiLz48L3N2Zz4=')]">
        {messages.length === 0 ? (
          <div className="h-full flex flex-col items-center justify-center text-[var(--color-text-muted)] text-sm space-y-3">
            <div className="w-16 h-16 rounded-full bg-emerald-100 dark:bg-emerald-900/30 flex items-center justify-center">
               <Mic className="h-8 w-8 text-emerald-500" />
            </div>
            <p>Start speaking to AI...</p>
          </div>
        ) : (
          <AnimatePresence initial={false}>
            {messages.map((msg) => {
              const isAi = msg.role === 'ai'
              return (
                <motion.div
                  key={msg.id}
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  className={`flex w-full ${isAi ? 'justify-start' : 'justify-end'}`}
                >
                  <div className={`flex max-w-[85%] gap-2 ${isAi ? 'flex-row' : 'flex-row-reverse'}`}>
                    <div className={`shrink-0 h-8 w-8 rounded-full flex items-center justify-center shadow-sm ${
                      isAi 
                        ? 'bg-gradient-to-br from-emerald-500 to-teal-600 text-white' 
                        : 'bg-gradient-to-br from-blue-500 to-indigo-600 text-white'
                    }`}>
                      {isAi ? <Bot className="h-4 w-4" /> : <User className="h-4 w-4" />}
                    </div>

                    <div className={`relative px-4 py-3 shadow-sm text-[15px] leading-relaxed border ${
                      isAi
                        ? 'bg-white dark:bg-[var(--color-surface-2)] border-[var(--color-border)] rounded-2xl rounded-tl-sm text-[var(--color-text-primary)]'
                        : 'bg-emerald-50 dark:bg-emerald-950/40 border-emerald-100 dark:border-emerald-900/50 rounded-2xl rounded-tr-sm text-[var(--color-text-primary)]'
                    }`}>
                      {msg.text}
                      {!msg.isFinal && (
                        <span className="ml-2 inline-flex gap-0.5 items-center opacity-60">
                          <span className="w-1 h-1 rounded-full bg-current animate-bounce" style={{ animationDelay: '0ms' }} />
                          <span className="w-1 h-1 rounded-full bg-current animate-bounce" style={{ animationDelay: '150ms' }} />
                          <span className="w-1 h-1 rounded-full bg-current animate-bounce" style={{ animationDelay: '300ms' }} />
                        </span>
                      )}
                    </div>
                  </div>
                </motion.div>
              )
            })}
          </AnimatePresence>
        )}
        <div ref={messagesEndRef} className="h-4" />
      </div>

      {/* Controls */}
      <div className="h-20 bg-[var(--color-surface)] border-t border-[var(--color-border)] flex items-center justify-center gap-6 shrink-0 z-10 shadow-[0_-4px_15px_-3px_rgba(0,0,0,0.05)]">
        <button 
          onClick={toggleMute}
          className={`h-12 w-12 rounded-full flex items-center justify-center transition-all shadow-sm border ${
            isMuted 
              ? 'bg-red-50 dark:bg-red-900/20 text-red-500 border-red-200 dark:border-red-900/50 hover:bg-red-100 dark:hover:bg-red-900/40' 
              : 'bg-white dark:bg-[var(--color-surface-2)] text-[var(--color-text-secondary)] border-[var(--color-border)] hover:bg-gray-50 dark:hover:bg-gray-800'
          }`}
        >
          {isMuted ? <MicOff className="h-5 w-5" /> : <Mic className="h-5 w-5" />}
        </button>
        <button 
          onClick={handleEndCall}
          className="h-14 w-14 rounded-full bg-red-500 hover:bg-red-600 flex items-center justify-center shadow-lg transition-transform hover:scale-105 active:scale-95 text-white"
        >
          <PhoneOff className="h-6 w-6" />
        </button>
      </div>
    </div>
  )
}
