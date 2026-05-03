---
trigger: manual
---

# AI — Speech (TTS / STT)

> **Scope:** Text-to-Speech & Speech-to-Text endpoints  
> **Auth:** Bearer + `RateLimiting("ai-scoring")` cho tất cả.  
> **@mention khi làm:** TTS, Neural2, Gemini Voice, STT, nhận diện giọng nói

---

## 1. Get Voices

**Neural2 Voices:**
```
GET /api/v1/ai/tts/voices
Trả về list từ TtsVoiceCatalog.All
```

**Gemini Live Voices (Polyglot):**
```
GET /api/v1/ai/live-voices
Trả về list từ LiveVoiceCatalog.All (30 giọng, mặc định: "zephyr")
```

---

## 2. Text-to-Speech (Neural2)

Sử dụng Google Cloud Text-to-Speech (gRPC). Cần chứng chỉ ADC hoặc file JSON.
```
POST /api/v1/ai/tts
Body: { "text": "...", "voice": "en-US-Neural2-C", "speakingRate": 1.0 }
Max text length: 5000 chars.

Response 200: file binary (audio/mpeg)
```

---

## 3. Text-to-Speech (Gemini)

Sử dụng Gemini Text-to-Speech REST API. Có độ truyền cảm cực cao. Có caching qua `IMemoryCache` (24h) dùng SHA-256 key để tránh gọi API trùng lặp.
```
POST /api/v1/ai/gemini-tts
Body: { 
  "text": "...", 
  "voice": "zephyr", 
  "stylePrompt": "Speak slowly for English learners",
  "languageCode": "en-US" 
}
Max text length: 5000 chars.

Response 200: file binary (audio/mpeg)
```

---

## 4. Speech-to-Text

Sử dụng Google Cloud Speech-to-Text (gRPC).
```
POST /api/v1/ai/stt (multipart/form-data)
Fields:
- audioFile (IFormFile, max 10MB)
- languages: "en-US,vi-VN" (optional, hỗ trợ nhận diện đa ngôn ngữ)

Supported types: webm, ogg, wav, flac, mpeg, amr

Response 200: { transcript, confidence, detectedLanguage }
```
