/**
 * Client-side timeout for requests that call an AI provider (generation, grading, assessment, TTS).
 * Defaults to 30 minutes; override with VITE_AI_REQUEST_TIMEOUT_MS. Kept above the backend provider
 * timeouts (Ai:*:TimeoutSeconds in appsettings, default 10 minutes) so the backend's own timeout error
 * reaches the user instead of a client-side abort.
 */
const DEFAULT_AI_REQUEST_TIMEOUT_MS = 30 * 60 * 1000;
// Browser timers overflow above 2^31-1 ms and would fire immediately.
const MAX_TIMER_MS = 2_147_483_647;

export function readTimeout(raw: unknown): number {
  const value = Number(raw);
  return Number.isFinite(value) && value > 0 && value <= MAX_TIMER_MS ? value : DEFAULT_AI_REQUEST_TIMEOUT_MS;
}

export const AI_REQUEST_TIMEOUT_MS = readTimeout(import.meta.env.VITE_AI_REQUEST_TIMEOUT_MS);
