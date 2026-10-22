import React from 'react';
import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import {
  FallingWordCard,
  computeLiveSlots,
  getPatternTypographyClass,
} from '../components/FallingWordCard';
import { GameInputBar } from '../components/GameInputBar';
import { RoundResultOverlay } from '../components/RoundResultOverlay';
import { SessionSummaryModal } from '../components/SessionSummaryModal';
import type {
  ActiveFallingWordRoundDto,
  FallingWordSessionDto,
  TerminalFallingWordRoundDto,
} from '../types/fallingWords.types';

describe('Falling Words Game Component Rendering Tests', () => {
  describe('FallingWordCard', () => {
    const mockActiveRound: ActiveFallingWordRoundDto = {
      roundId: 'round-1',
      sessionId: 'session-1',
      roundOrder: 1,
      totalRounds: 10,
      vocabularyId: 'vocab-1',
      senseId: 'sense-1',
      meaningVi: 'lịch trình, kế hoạch làm việc',
      cefrLevel: 'B1',
      maskedPattern: '_ _ _ _ _ _ _ _',
      letterCount: 8,
      status: 'Active',
      startedAtUtc: '2026-10-22T10:00:00Z',
      expiresAtUtc: '2026-10-22T10:00:05Z',
      serverNowUtc: '2026-10-22T10:00:00Z',
      timeLimitSeconds: 5,
      remainingSeconds: 5,
    };

    it('renders masked pattern and Vietnamese meaning without exposing target word in DOM', () => {
      const html = renderToStaticMarkup(
        <FallingWordCard
          round={mockActiveRound}
          progressRatio={0.5}
          isReducedMotion={false}
        />
      );

      expect(html).toContain('_ _ _ _ _ _ _ _');
      expect(html).toContain('lịch trình, kế hoạch làm việc');
      expect(html).toContain('8 chữ cái');
      expect(html).toContain('B1');
      // Ensure target word is NOT exposed in the DOM
      expect(html.toLowerCase()).not.toContain('schedule');
    });

    it('positions card according to progressRatio in normal motion mode', () => {
      const html = renderToStaticMarkup(
        <FallingWordCard
          round={mockActiveRound}
          progressRatio={0.5}
          isReducedMotion={false}
        />
      );

      expect(html).toContain('top:41%'); // 0.5 * 82 = 41%
    });

    it('keeps card stationary in reduced-motion mode with explanatory notice', () => {
      const html = renderToStaticMarkup(
        <FallingWordCard
          round={mockActiveRound}
          progressRatio={0.9}
          isReducedMotion={true}
        />
      );

      expect(html).toContain('top:10%');
      expect(html).toContain('Chế độ giảm chuyển động');
    });

    it('dynamically reflects typed letters into blank slots without exposing remaining target letters', () => {
      const html = renderToStaticMarkup(
        <FallingWordCard
          round={mockActiveRound}
          progressRatio={0.2}
          isReducedMotion={false}
          currentInput="sc"
        />
      );

      // Typed letters are present with emerald highlight styling
      expect(html).toContain('text-emerald-600');
      expect(html).toContain('s');
      expect(html).toContain('c');
      // Remaining unfilled slots are still underscores
      expect(html).toContain('_');
      // The rest of the private word ('hedule') is never revealed in DOM
      expect(html.toLowerCase()).not.toContain('hedule');
    });

    it('correctly maps typed characters with computeLiveSlots including spaces and hyphens', () => {
      // Single word test
      const slots1 = computeLiveSlots('_ _ _ _', 'he');
      expect(slots1.map((s) => s.char)).toEqual(['h', 'e', '_', '_']);
      expect(slots1[0].isFilled).toBe(true);
      expect(slots1[1].isFilled).toBe(true);
      expect(slots1[2].isFilled).toBe(false);

      // Multi-word with space test: "ice cream" -> tokens "_ _ _", " ", "_ _ _ _ _"
      const iceCreamMasked = '_ _ _   _ _ _ _ _';
      const slots2 = computeLiveSlots(iceCreamMasked, 'ice cr');
      expect(slots2.map((s) => s.char)).toEqual(['i', 'c', 'e', ' ', 'c', 'r', '_', '_', '_']);
      expect(slots2[3].isSeparator).toBe(true);
    });

    it('scales typography dynamically based on word length to prevent overflowing the card', () => {
      expect(getPatternTypographyClass(4)).toContain('text-2xl sm:text-3xl');
      expect(getPatternTypographyClass(8)).toContain('text-xl sm:text-2xl');
      expect(getPatternTypographyClass(12)).toContain('text-base sm:text-xl');
      expect(getPatternTypographyClass(18)).toContain('text-sm sm:text-base');

      const longWordRound: ActiveFallingWordRoundDto = {
        ...mockActiveRound,
        letterCount: 16,
        maskedPattern: '_ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _',
        meaningVi: 'sự quốc tế hóa các tiêu chuẩn',
      };

      const html = renderToStaticMarkup(
        <FallingWordCard
          round={longWordRound}
          progressRatio={0.3}
          isReducedMotion={false}
        />
      );

      // Card container has larger max width and wrapping support
      expect(html).toContain('max-w-lg sm:max-w-xl');
      expect(html).toContain('break-words whitespace-pre-wrap');
      expect(html).toContain('text-sm sm:text-base');
    });
  });

  describe('GameInputBar', () => {
    it('renders input with placeholder, spellcheck disabled, and submit button', () => {
      const html = renderToStaticMarkup(
        <GameInputBar
          value="schedule"
          onChange={() => {}}
          onSubmit={() => {}}
          disabled={false}
          isSubmitting={false}
        />
      );

      expect(html).toContain('value="schedule"');
      expect(html).toContain('spellCheck="false"');
      expect(html).toContain('autoComplete="off"');
      expect(html).toContain('role="searchbox"');
      expect(html).toContain('name="vocabularyGameWordQuery"');
      expect(html).toContain('data-form-type="other"');
      expect(html).toContain('data-lpignore="true"');
      expect(html).toContain('Nộp');
      expect(html).toContain('Enter');
    });

    it('renders disabled state with loading spinner when isSubmitting is true', () => {
      const html = renderToStaticMarkup(
        <GameInputBar
          value="schedule"
          onChange={() => {}}
          onSubmit={() => {}}
          disabled={false}
          isSubmitting={true}
        />
      );

      expect(html).toContain('disabled=""');
      expect(html).toContain('Đang nộp...');
    });
  });

  describe('RoundResultOverlay', () => {
    const mockTerminalResult: TerminalFallingWordRoundDto = {
      roundId: 'round-1',
      roundOrder: 1,
      vocabularyId: 'vocab-1',
      senseId: 'sense-1',
      meaningVi: 'lịch trình',
      cefrLevel: 'B1',
      status: 'Correct',
      expectedWord: 'schedule',
      learnerAnswer: 'Schedule',
      startedAtUtc: '2026-10-22T10:00:00Z',
      expiresAtUtc: '2026-10-22T10:00:05Z',
      submittedAtUtc: '2026-10-22T10:00:02Z',
      elapsedMs: 2100,
      failureReason: null,
    };

    it('reveals expected word and correct outcome badge', () => {
      const html = renderToStaticMarkup(
        <RoundResultOverlay
          result={mockTerminalResult}
          onNext={() => {}}
          isLastRound={false}
          isLoadingNext={false}
        />
      );

      expect(html).toContain('schedule');
      expect(html).toContain('Chính xác! 🎉');
      expect(html).toContain('Tiếp theo');
    });

    it('renders TimedOut outcome with elapsed time and last-round summary button', () => {
      const timedResult: TerminalFallingWordRoundDto = {
        ...mockTerminalResult,
        status: 'TimedOut',
        learnerAnswer: null,
        failureReason: 'ExpiredBeforeSubmit',
      };

      const html = renderToStaticMarkup(
        <RoundResultOverlay
          result={timedResult}
          onNext={() => {}}
          isLastRound={true}
          isLoadingNext={false}
        />
      );

      expect(html).toContain('Hết giờ! ⏰');
      expect(html).toContain('Xem tổng kết ván chơi');
    });

    it('renders Skipped outcome with revealed expected word and skipped indicator', () => {
      const skippedResult: TerminalFallingWordRoundDto = {
        ...mockTerminalResult,
        status: 'Incorrect',
        learnerAnswer: '[Bỏ qua]',
        failureReason: 'SkippedByLearner',
      };

      const html = renderToStaticMarkup(
        <RoundResultOverlay
          result={skippedResult}
          onNext={() => {}}
          isLastRound={false}
          isLoadingNext={false}
        />
      );

      expect(html).toContain('Đã bỏ qua! ⏭️');
      expect(html).toContain('schedule');
      expect(html).toContain('(Đã bỏ qua từ này)');
    });

    it('renders pronunciation speaker audio button when onPlayAudio is provided', () => {
      const html = renderToStaticMarkup(
        <RoundResultOverlay
          result={mockTerminalResult}
          onNext={() => {}}
          isLastRound={false}
          isLoadingNext={false}
          onPlayAudio={() => {}}
          isAudioPlaying={false}
        />
      );

      expect(html).toContain('title="Nghe phát âm từ này"');
    });
  });

  describe('SessionSummaryModal', () => {
    const mockSession: FallingWordSessionDto = {
      id: 'session-1',
      source: 'VocabularyCatalog',
      cefrLevel: 'B1',
      playFilter: 'All',
      timeLimitSeconds: 5,
      requestedCount: 2,
      actualCount: 2,
      shortfall: 0,
      status: 'Completed',
      startedAtUtc: '2026-10-22T10:00:00Z',
      completedAtUtc: '2026-10-22T10:00:20Z',
      correctCount: 1,
      incorrectCount: 1,
      timedOutCount: 0,
      abandonedCount: 0,
      bestStreak: 1,
      currentStreak: 0,
      accuracy: 50.0,
      currentRoundOrder: null,
      totalRounds: 2,
      activeRound: null,
      completedRounds: [
        {
          roundId: 'r1',
          roundOrder: 1,
          vocabularyId: 'v1',
          senseId: 's1',
          meaningVi: 'lịch trình',
          cefrLevel: 'B1',
          status: 'Correct',
          expectedWord: 'schedule',
          learnerAnswer: 'schedule',
          startedAtUtc: null,
          expiresAtUtc: null,
          submittedAtUtc: null,
          elapsedMs: 2000,
          failureReason: null,
        },
        {
          roundId: 'r2',
          roundOrder: 2,
          vocabularyId: 'v2',
          senseId: 's2',
          meaningVi: 'lịch',
          cefrLevel: 'A2',
          status: 'Incorrect',
          expectedWord: 'calendar',
          learnerAnswer: 'calender',
          startedAtUtc: null,
          expiresAtUtc: null,
          submittedAtUtc: null,
          elapsedMs: 3000,
          failureReason: 'IncorrectSpelling',
        },
      ],
    };

    it('renders accurate stats summary and round details list', () => {
      const html = renderToStaticMarkup(
        <SessionSummaryModal
          session={mockSession}
          onPlayAgain={() => {}}
          onGoToHistory={() => {}}
          onGoToSetup={() => {}}
        />
      );

      expect(html).toContain('50.0%');
      expect(html).toContain('schedule');
      expect(html).toContain('calendar');
      expect(html).toContain('Hoàn thành ván chơi!');
      expect(html).toContain('Chơi ván mới');
      expect(html).toContain('Xem lịch sử');
    });
  });
});
