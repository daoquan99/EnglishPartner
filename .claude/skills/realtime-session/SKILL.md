---
name: realtime-session
description: Thiết kế, sửa hoặc review luồng realtime của Live Tutor — 3 SignalR hub (tutor, transcribe, translate), GeminiLiveConnectionManager/GeminiLiveSessionBridge, Redis backplane, ownership theo connection, dọn state; phía FE là useThreeHubLiveSession, microphone, AudioContext/AudioWorklet. Dùng khi đụng hub, phiên live hoặc audio.
---

<!-- Sinh tự động bởi scripts/ai/sync-claude-skills.sh. Sửa skill nguồn, không sửa file này. -->

Skill nguồn: `.agents/skills/realtime-session/SKILL.md`. Đọc toàn bộ file đó (đường dẫn tính từ gốc repo) và làm theo; các link tương đối trong file nguồn tính từ thư mục của nó.
