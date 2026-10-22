export type FallingWordGameSource = 'VocabularyCatalog' | 'DailyVocabulary';

export type FallingWordPlayFilter = 'All' | 'Unplayed' | 'Played' | 'Correct' | 'Failed';

export type FallingWordGameStatus = 'Ready' | 'InProgress' | 'Completed' | 'Abandoned';

export type FallingWordRoundStatus =
  | 'Queued'
  | 'Active'
  | 'Correct'
  | 'Incorrect'
  | 'TimedOut'
  | 'Abandoned';

export interface FallingWordPoolCountDto {
  totalValidCount: number;
  source: string;
  cefrLevel: string | null;
  playFilter: string;
  allCount: number;
  unplayedCount: number;
  playedCount: number;
  correctCount: number;
  failedCount: number;
}

export interface CreateFallingWordSessionRequest {
  source: FallingWordGameSource;
  cefrLevel?: string | null;
  playFilter: FallingWordPlayFilter;
  timeLimitSeconds: number;
  requestedCount: number;
  timeZoneId?: string | null;
}

export interface ActiveFallingWordRoundDto {
  roundId: string;
  sessionId: string;
  roundOrder: number;
  totalRounds: number;
  vocabularyId: string;
  senseId: string;
  meaningVi: string;
  cefrLevel?: string | null;
  maskedPattern: string;
  letterCount: number;
  status: string;
  startedAtUtc?: string | null;
  expiresAtUtc?: string | null;
  serverNowUtc: string;
  timeLimitSeconds: number;
  remainingSeconds: number;
}

export interface TerminalFallingWordRoundDto {
  roundId: string;
  roundOrder: number;
  vocabularyId: string;
  senseId: string;
  meaningVi: string;
  cefrLevel?: string | null;
  status: string;
  expectedWord: string;
  learnerAnswer?: string | null;
  startedAtUtc?: string | null;
  expiresAtUtc?: string | null;
  submittedAtUtc?: string | null;
  elapsedMs?: number | null;
  failureReason?: string | null;
}

export interface FallingWordSessionDto {
  id: string;
  source: string;
  cefrLevel?: string | null;
  playFilter: string;
  timeLimitSeconds: number;
  requestedCount: number;
  actualCount: number;
  shortfall: number;
  status: string;
  startedAtUtc?: string | null;
  completedAtUtc?: string | null;
  correctCount: number;
  incorrectCount: number;
  timedOutCount: number;
  abandonedCount: number;
  bestStreak: number;
  currentStreak: number;
  accuracy?: number | null;
  currentRoundOrder?: number | null;
  totalRounds: number;
  activeRound?: ActiveFallingWordRoundDto | null;
  completedRounds: TerminalFallingWordRoundDto[];
}

export interface SubmitFallingWordAnswerResponse {
  roundResult: TerminalFallingWordRoundDto;
  isSessionCompleted: boolean;
  correctCount: number;
  incorrectCount: number;
  timedOutCount: number;
  currentStreak: number;
  bestStreak: number;
  accuracy?: number | null;
}

export interface FallingWordSessionSummaryDto {
  id: string;
  source: string;
  cefrLevel?: string | null;
  playFilter: string;
  timeLimitSeconds: number;
  actualCount: number;
  correctCount: number;
  incorrectCount: number;
  timedOutCount: number;
  abandonedCount: number;
  bestStreak: number;
  accuracy?: number | null;
  status: string;
  createdAtUtc: string;
  startedAtUtc?: string | null;
  completedAtUtc?: string | null;
}

export interface WordFallingHistoryItemDto {
  roundId: string;
  sessionId: string;
  startedAtUtc: string;
  outcome: string;
  learnerAnswer?: string | null;
  expectedWord: string;
  elapsedMs?: number | null;
  timeLimitSeconds: number;
  source: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface SynthesizeFallingWordAudioRequest {
  apiKey?: string;
  voiceName?: string;
  speechSpeed?: number;
  accent?: string;
  modelCode?: string;
}

export interface FallingWordAudioDto {
  audioBase64: string;
  mimeType: string;
  sampleRate: number;
  durationMs: number;
}
