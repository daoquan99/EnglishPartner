# Update Chat Message

These APIs allow updating metadata (such as AudioUrl or Translation) for a specific message within a chat session.

## 1. Upload Message Audio File

**Endpoint:** `POST /api/v1/ai/chat/sessions/{sessionId}/messages/{messageId}/audio`

**Authorization:** `Bearer Token` (But actually requires DisableAntiforgery for file upload)

Uploads an audio file (`multipart/form-data`) for a specific chat message. The backend will save the file to the configured file storage and automatically update the message's `AudioUrl` field to point to the uploaded file.

### Path Parameters

- `sessionId` (guid): The ID of the chat session.
- `messageId` (guid): The ID of the message to attach audio to.

### Request Body (multipart/form-data)

- `audioFile` (file): The audio file to upload (Max size 10MB).

### Responses

- `200 OK`: Success. Returns the `audioUrl` as a JSON object: `{ "audioUrl": "https://..." }`
- `400 Bad Request`: File is missing or too large.
- `401 Unauthorized`: Invalid or missing token.
- `403 Forbidden`: User does not own the session.
- `404 Not Found`: Session or message not found.

## 2. Update Message Audio URL directly

**Endpoint:** `PATCH /api/v1/ai/chat/sessions/{sessionId}/messages/{messageId}/audio-url`

**Authorization:** `Bearer Token`

Updates the audio URL for a specific chat message, usually after a text-to-speech task has completed on the client side or via a third-party service.

### Path Parameters

- `sessionId` (guid): The ID of the chat session.
- `messageId` (guid): The ID of the message to update.

### Request Body

```json
{
  "audioUrl": "https://example.com/audio/12345.mp3"
}
```

### Responses

- `200 OK`: Success.
- `400 Bad Request`: Validation errors.
- `401 Unauthorized`: Invalid or missing token.
- `403 Forbidden`: User does not own the session.
- `404 Not Found`: Session or message not found.


## 3. Update Message Translation

**Endpoint:** `PATCH /api/v1/ai/chat/sessions/{sessionId}/messages/{messageId}/translation`

**Authorization:** `Bearer Token`

Updates the translation text for a specific chat message.

### Path Parameters

- `sessionId` (guid): The ID of the chat session.
- `messageId` (guid): The ID of the message to update.

### Request Body

```json
{
  "translation": "Đây là văn bản được dịch sang tiếng Việt."
}
```

### Responses

- `200 OK`: Success.
- `400 Bad Request`: Validation errors.
- `401 Unauthorized`: Invalid or missing token.
- `403 Forbidden`: User does not own the session.
- `404 Not Found`: Session or message not found.
