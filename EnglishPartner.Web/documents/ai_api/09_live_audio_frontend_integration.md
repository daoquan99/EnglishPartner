# Live Audio SignalR Integration Guide

This document explains how the Frontend (React/TypeScript) should connect to the `AiLiveAvatarHub` SignalR connection to stream audio to the Gemini Live API and receive real-time audio/text responses.

Since the Backend handles the `Google.GenAI` SDK connection internally, the frontend no longer connects directly to the Google API (`ai.current.live.connect`). Instead, it communicates via a WebSockets Hub connection to the EnglishPartner Backend.

---

## 1. Connection Lifecycle

### Establishing the Connection
Use `@microsoft/signalr` to connect to the `/hubs/ai-live-avatar` endpoint. Pass the `systemPrompt` (and optionally `sessionId`) as a query parameter.

```ts
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${API_URL}/hubs/ai-live-avatar?systemPrompt=${encodeURIComponent(systemPrompt)}`)
    .withAutomaticReconnect()
    .build();

await connection.start();
```

### Disconnecting
When the user ends the call:
```ts
await connection.stop();
```

---

## 2. Event Listeners (Backend -> Frontend)

The backend exposes several events. You must bind these *before* starting the connection.

| Event Name | Parameter | Description |
|---|---|---|
| `AudioChunkReceived` | `byte[]` (Base64 in JS) | PCM 16kHz audio data from Gemini |
| `InputTranscriptionReceived` | `string` | Speech-to-Text transcription of the **User's** audio |
| `OutputTranscriptionReceived` | `string` | Speech-to-Text transcription of the **AI's** audio |
| `TextChunkReceived` | `string` | Fallback text chunk from the AI |
| `TurnComplete` | `None` | AI has finished its speaking turn |

**Example Implementation:**
```ts
connection.on("AudioChunkReceived", (base64Data: string) => {
    playAudioChunk(base64Data); // Same as your existing playAudioChunk logic
});

connection.on("InputTranscriptionReceived", (text: string) => {
    appendTranscript(text, true); // isUser = true
});

connection.on("OutputTranscriptionReceived", (text: string) => {
    appendTranscript(text, false); // isUser = false
});

connection.on("TurnComplete", () => {
    // Hide "AI is thinking/speaking" animation if desired
});
```

---

## 3. Sending Data (Frontend -> Backend)

### Streaming Microphone Audio
Capture microphone input via `AudioContext` and `ScriptProcessorNode` (just like your existing code). Extract the PCM data, convert it to a Base64 string or an array of bytes, and send it to the Hub using `SendAudioChunk`.

```ts
// Inside your micProcessor.onaudioprocess
const inputData = e.inputBuffer.getChannelData(0);
const pcm16 = new Int16Array(inputData.length);
for (let i = 0; i < inputData.length; i++) {
    const s = Math.max(-1, Math.min(1, inputData[i]));
    pcm16[i] = s < 0 ? s * 0x8000 : s * 0x7FFF;
}

// Convert to Base64 (or Uint8Array depending on SignalR config, but Base64 is standard)
const buffer = new ArrayBuffer(pcm16.length * 2);
const view = new DataView(buffer);
pcm16.forEach((val, i) => view.setInt16(i * 2, val, true));
const base64Data = btoa(String.fromCharCode(...new Uint8Array(buffer)));

// Send to backend
connection.invoke("SendAudioChunk", base64Data)
    .catch(err => console.error(err));
```

*(Note: SignalR automatically decodes Base64 into `byte[]` on the C# side).*

### Sending Text (Optional)
If you want to send a text message (e.g. user clicked a predefined prompt):
```ts
connection.invoke("SendTextMessage", "Hello Gemini!")
    .catch(err => console.error(err));
```

---

## 4. Key Differences from Direct TS SDK

1. **No `ai.current.live.connect()`**: The C# backend maintains the Gemini API session securely. You don't need `GEMINI_API_KEY` on the frontend.
2. **Audio Modality Only**: The backend requests `["AUDIO"]` modality. The text you see on the screen comes from the `InputTranscription` and `OutputTranscription` streams, rather than the raw `modelTurn.parts.text`. This prevents duplicate text outputs while the AI speaks.
3. **Interruption Handling**: The backend SDK doesn't expose a direct `interrupted` flag easily yet. If the user starts talking over the AI, the backend simply forwards the user's new audio, and Gemini stops generating. The frontend should handle `handleInterrupt()` locally when it detects microphone input > volume threshold while `isBotSpeaking` is true.
