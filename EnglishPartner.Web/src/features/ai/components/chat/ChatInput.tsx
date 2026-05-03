'use client'

import { useState, useRef, useEffect } from 'react'
import { Send } from 'lucide-react'
import { Button } from '@/ui/button'
import { AudioRecorder } from '@/shared/components/AudioRecorder'
import { useAuthStore } from '@/lib/zustand/auth.store'
import { apiClient } from '@/lib/axios'

interface ChatInputProps {
  onSend: (text: string) => void
  isSending: boolean
}

export function ChatInput({ onSend, isSending }: ChatInputProps) {
  const [text, setText] = useState('')
  const [isTranscribing, setIsTranscribing] = useState(false)
  const accessToken = useAuthStore(state => state.accessToken)
  
  const wsRef = useRef<WebSocket | null>(null)
  const accumulatedRef = useRef('') // Holds final phrases
  
  // Clean up ws on unmount
  useEffect(() => {
    return () => {
      if (wsRef.current) {
        wsRef.current.close()
      }
    }
  }, [])

  const handleSend = () => {
    if (!text.trim() || isSending) return
    onSend(text)
    setText('')
    accumulatedRef.current = ''
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleSend()
    }
  }

  const handleRecordingStart = () => {
    setIsTranscribing(true)
    accumulatedRef.current = text ? text + ' ' : '' // Keep existing text
    
    const baseUrl = process.env.NEXT_PUBLIC_API_URL ?? window.location.origin
    const wsUrl = baseUrl.replace(/^http/, 'ws')
    const ws = new WebSocket(`${wsUrl}/api/v1/ai/stt/stream?languages=en-US&mimeType=audio/webm&access_token=${accessToken}`)
    
    ws.onmessage = (event) => {
      try {
        const data = JSON.parse(event.data)
        if (data.error) return

        if (data.isFinal) {
          accumulatedRef.current += data.transcript
          setText(accumulatedRef.current)
        } else {
          setText(accumulatedRef.current + data.transcript)
        }
      } catch {}
    }
    
    ws.onclose = async (event) => {
      // 1008 (Policy Violation) usually implies an invalid/expired token.
      if (event.code === 1008 || event.code === 1011 || event.code === 1006) {
        try {
          // Trigger the global interceptor to refresh the token
          await apiClient.get('/api/v1/profile/me')
        } catch {}
      }
    }
    
    wsRef.current = ws
  }

  const handleRecordingData = (blob: Blob) => {
    if (wsRef.current?.readyState === WebSocket.OPEN) {
      wsRef.current.send(blob)
    }
  }

  const handleRecordingComplete = () => {
    if (wsRef.current) {
      wsRef.current.close()
      wsRef.current = null
    }
    setIsTranscribing(false)
  }

  return (
    <div className="flex gap-2 items-end">
      <div className="flex-1 relative rounded-2xl border-[1.5px] border-[var(--color-border)] bg-[var(--color-surface)] shadow-sm focus-within:ring-4 focus-within:ring-violet-500/10 focus-within:border-violet-500 transition-all">
        <textarea
          value={text}
          onChange={e => setText(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Type a message or use your voice..."
          className="w-full max-h-32 min-h-[44px] py-3 pl-4 pr-20 bg-transparent border-0 resize-none outline-none focus:ring-0 text-[15px] [&::-webkit-scrollbar]:hidden"
          rows={1}
          disabled={isSending || isTranscribing}
        />
        
        {/* Voice Recording Button */}
        <div className="absolute right-2 bottom-1.5 z-10 bg-[var(--color-surface)] rounded-full">
          <AudioRecorder
            onRecordingStart={handleRecordingStart}
            onDataAvailable={handleRecordingData}
            onRecordingComplete={handleRecordingComplete}
            disabled={isSending}
            size="sm"
            maxDuration={60}
            className={`border-0 shadow-none text-violet-600 transition-colors ${isTranscribing ? 'bg-violet-100 dark:bg-violet-900/30' : 'bg-transparent hover:bg-violet-50 dark:hover:bg-violet-900/20'}`}
          />
        </div>
      </div>

      <Button
        onClick={handleSend}
        disabled={!text.trim() || isSending || isTranscribing}
        className="h-[44px] w-[44px] rounded-2xl shrink-0 p-0 bg-gradient-to-br from-violet-500 to-indigo-600 hover:shadow-md transition-all text-white"
      >
        <Send className="h-4 w-4 ml-0.5" />
      </Button>
    </div>
  )
}
