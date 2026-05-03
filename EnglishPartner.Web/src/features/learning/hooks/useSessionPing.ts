'use client'

import { useEffect, useRef } from 'react'
import { usePingSession } from '../api/useLearning'

/**
 * Pings the active study session every 3 minutes to keep it alive.
 * Auto-clears on unmount or when sessionId becomes null.
 */
export function useSessionPing(sessionId: string | null) {
  const { mutate: ping } = usePingSession()
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null)

  useEffect(() => {
    if (intervalRef.current) clearInterval(intervalRef.current)

    if (sessionId) {
      intervalRef.current = setInterval(() => ping(sessionId), 3 * 60 * 1000)
    }

    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current)
    }
  }, [sessionId, ping])
}
