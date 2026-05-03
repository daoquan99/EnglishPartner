# AI Live Avatar (Real-time Audio Session)

**Scope:** Real-time, full-duplex bidirectional audio/text communication with Gemini Live. Avoids STT->LLM->TTS latency. 
**Audio Format:** 16-bit PCM at 16kHz.

## Components & Flow

1. **`AiLiveAvatarHub` (SignalR)**
   - Receives WebSocket connection from frontend (`wss://.../hubs/ai-live-avatar?access_token=...&systemPrompt=...`).
   - Uses `LiveAudioSessionManager` to create/bind a session to the `ConnectionId`.
   - Exposes `SendAudioChunk(byte[])` and `SendTextMessage(string)` to the client.
   - Forwards AI responses back to the client via `AudioChunkReceived` and `TextChunkReceived` events.

2. **`LiveAudioSessionManager`**
   - Manages the lifecycle of active sessions in a `ConcurrentDictionary`.
   - Provisions `ILiveAudioSession` via DI and cleans up resources on disconnect to prevent memory leaks.

3. **`ILiveAudioSession`**
   - Abstraction for AI bidirectional streaming (`ConnectAsync`, `SendAudioAsync`, `SendTextAsync`).
   - Exposes hooks: `OnAudioReceived` and `OnTextReceived`.

4. **`GoogleGenAILiveAudioSession` (Google.GenAI Implementation)**
   - Directly connects to Gemini Live (`gemini-live-2.5-flash-native-audio`).
   - Runs a background `ReceiveLoopAsync` to continuously consume the gRPC stream.
   - Pushes output audio/text to the mapped `OnAudioReceived`/`OnTextReceived` actions.

5. **`SpeechEndpoints` (`/live-voices`)**
   - Returns valid voice options from `LiveVoiceCatalog` for the client to use.

## Design Highlights
- **Ultra-low latency:** Direct streaming of PCM chunks.
- **Interruption Support:** Streaming allows the model to immediately recognize user interruptions.
- **Concurrency:** Audio and text chunks arrive simultaneously in the same connection.
- **Clean Architecture:** SignalR Hub (Transport) -> Manager (State) -> ILiveAudioSession (Domain/Abstraction) -> GoogleGenAILiveAudioSession (Infrastructure).
