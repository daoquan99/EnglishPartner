import * as signalR from '@microsoft/signalr'
import { useAuthStore } from '@/lib/zustand/auth.store'

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'https://localhost:7001'

let connection: signalR.HubConnection | null = null

export const getHubConnection = (): signalR.HubConnection => {
  if (!connection) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/notifications`, {
        accessTokenFactory: () => useAuthStore.getState().accessToken ?? '',
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build()
  }
  return connection
}

export const startHubConnection = async (): Promise<void> => {
  const hub = getHubConnection()
  if (hub.state === signalR.HubConnectionState.Disconnected) {
    try {
      await hub.start()
    } catch (err) {
      console.error('[SignalR] Failed to connect:', err)
    }
  }
}

export const stopHubConnection = async (): Promise<void> => {
  if (connection?.state !== signalR.HubConnectionState.Disconnected) {
    await connection?.stop()
  }
}
