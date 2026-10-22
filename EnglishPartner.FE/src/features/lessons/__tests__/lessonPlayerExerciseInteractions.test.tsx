import React from 'react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MultipleChoicePlayer } from '../components/player/MultipleChoicePlayer';
import { SpellingPracticePlayer } from '../components/player/SpellingPracticePlayer';
import { MatchingPlayer } from '../components/player/MatchingPlayer';
import { ShadowingPlayer } from '../components/player/ShadowingPlayer';
import { DictationPlayer } from '../components/player/DictationPlayer';
import { useTutorSessionStore } from '@/features/tutor/store/tutorSessionStore';
import type { DictationAiReviewDto } from '../types/lessons.types';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: false },
  },
});

function renderWithQuery(ui: React.ReactElement) {
  return renderToStaticMarkup(
    <QueryClientProvider client={queryClient}>
      {ui}
    </QueryClientProvider>
  );
}

describe('Lesson Player Exercise Interactions & Renderers', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('MultipleChoicePlayer', () => {
    it('renders single choice question and options with radio-style indicator', () => {
      const content = {
        question: 'What is the main topic?',
        options: [
          { id: 'opt-1', text: 'Clean Architecture' },
          { id: 'opt-2', text: 'Monolithic Architecture' },
        ],
        selectionMode: 'Single' as const,
      };

      const html = renderToStaticMarkup(
        <MultipleChoicePlayer
          content={content}
          onSubmit={() => {}}
          initialSelectedIds={['opt-1']}
        />
      );

      expect(html).toContain('What is the main topic?');
      expect(html).toContain('Clean Architecture');
      expect(html).toContain('Monolithic Architecture');
      // Contains the selected class / emerald border
      expect(html).toContain('border-emerald-500');
    });

    it('hydrates multiple selection with multiple options', () => {
      const content = {
        question: 'Select all correct principles:',
        options: [
          { id: 'opt-a', text: 'Dependency Inversion' },
          { id: 'opt-b', text: 'Single Responsibility' },
          { id: 'opt-c', text: 'Tight Coupling' },
        ],
        selectionMode: 'Multiple' as const,
      };

      const html = renderToStaticMarkup(
        <MultipleChoicePlayer
          content={content}
          onSubmit={() => {}}
          initialSelectedIds={['opt-a', 'opt-b']}
        />
      );

      expect(html).toContain('Dependency Inversion');
      expect(html).toContain('Single Responsibility');
      expect(html).toContain('Tight Coupling');
      // Should mark both as selected
      const selectedMatches = html.match(/border-emerald-500/g);
      expect(selectedMatches?.length).toBe(2);
    });
  });

  describe('SpellingPracticePlayer', () => {
    it('renders exampleSentence and Vietnamese clues (definition, IPA, example)', () => {
      const content = {
        prompt: 'Từ vựng chỉ lịch trình làm việc',
        definition: 'a plan that gives expected times for different things to happen',
        meaningVi: 'lịch trình, thời gian biểu',
        exampleSentence: 'The conference schedule is available online.',
        ipa: 'ˈskedʒ.uːl',
        hint: 'Bắt đầu bằng s-c-h',
      };

      const html = renderToStaticMarkup(
        <SpellingPracticePlayer
          content={content}
          onSubmit={() => {}}
          initialSpelledWord="schedule"
        />
      );

      expect(html).toContain('Từ vựng chỉ lịch trình làm việc');
      expect(html).toContain('Định nghĩa / Gợi ý nghĩa:');
      expect(html).toContain('a plan that gives expected times for different things to happen');
      expect(html).toContain('Phiên âm: /ˈskedʒ.uːl/');
      expect(html).toContain('Câu ví dụ minh họa:');
      expect(html).toContain('The conference schedule is available online.');
      expect(html).toContain('value="schedule"');
    });
  });

  describe('MatchingPlayer', () => {
    it('renders left and right items and hydrates matched pairs', () => {
      const content = {
        pairs: [
          { leftId: 'l1', leftText: 'Hello', rightId: 'r1', rightText: 'Xin chào' },
          { leftId: 'l2', leftText: 'Goodbye', rightId: 'r2', rightText: 'Tạm biệt' },
        ],
      };

      const html = renderToStaticMarkup(
        <MatchingPlayer
          content={content}
          onSubmit={() => {}}
          initialPairs={[{ leftId: 'l1', rightId: 'r1' }]}
        />
      );

      expect(html).toContain('Hello');
      expect(html).toContain('Xin chào');
      expect(html).toContain('Goodbye');
      expect(html).toContain('Tạm biệt');
    });
  });

  describe('ShadowingPlayer', () => {
    it('renders referenceText and audio playback, never pre-filling referenceText as user spokenText', () => {
      const content = {
        referenceText: 'I usually wake up at 6:30 a.m. and have a cup of coffee.',
        audioUrl: 'https://example.com/audio.mp3',
      };

      const html = renderToStaticMarkup(
        <ShadowingPlayer content={content} onSubmit={() => {}} />
      );

      expect(html).toContain('Luyện nói nhại (Shadowing) theo giọng mẫu');
      expect(html).toContain('I usually wake up at 6:30 a.m. and have a cup of coffee.');
      // Does not contain any input with referenceText as user spoken input
      expect(html).not.toContain('value="I usually wake up');
    });
  });

  describe('DictationPlayer & AI Formative Review', () => {
    it('renders transcription input and hydrates initialTranscribedText', () => {
      const content = {
        hint: 'Nghe kỹ âm đuôi',
      };

      const html = renderWithQuery(
        <DictationPlayer
          content={content}
          onSubmit={() => {}}
          initialTranscribedText="The weather is nice today."
        />
      );

      expect(html).toContain('Nghe chép chính tả (Dictation)');
      expect(html).toContain('The weather is nice today.');
    });

    it('renders "AI nhận xét bài chép" button when answer is submitted without review', () => {
      const html = renderWithQuery(
        <DictationPlayer
          content={{}}
          onSubmit={() => {}}
          isSubmitted={true}
          answerId="ans-123"
          attemptId="att-123"
          result={{
            answerId: 'ans-123',
            status: 'Completed',
            maxPoints: 10,
          }}
        />
      );

      expect(html).toContain('AI nhận xét bài chép');
      expect(html).toContain('Bài chép đã được chấm điểm tự động');
    });

    it('renders comprehensive formative review details when aiReview is present', () => {
      const mockReview: DictationAiReviewDto = {
        reviewId: 'rev-01',
        answerId: 'ans-123',
        status: 'Completed',
        feedback: 'Bạn đã chép được 85% câu, chú ý các từ phát âm lướt.',
        missingWords: ['handmade', 'for'],
        extraWords: ['a'],
        wordSubstitutions: [
          { expected: 'handmade', actual: 'hand made' },
        ],
        explanationsVi: 'Từ "handmade" được phát âm liền khối với trọng âm rơi vào âm tiết thứ hai. Người học dễ nghe nhầm thành hai từ rời.',
        actionableImprovements: [
          'Luyện nghe nối âm giữa phụ âm cuối và nguyên âm đầu.',
          'Chú ý nghe hiện tượng nuốt âm (elision).',
        ],
      };

      const html = renderWithQuery(
        <DictationPlayer
          content={{}}
          onSubmit={() => {}}
          isSubmitted={true}
          answerId="ans-123"
          attemptId="att-123"
          result={{
            answerId: 'ans-123',
            status: 'Completed',
            maxPoints: 10,
            aiReview: mockReview,
          }}
        />
      );

      expect(html).toContain('Nhận xét chi tiết từ AI Tutor');
      expect(html).toContain('Formative Review');
      expect(html).toContain('Bạn đã chép được 85% câu');
      expect(html).toContain('handmade');
      expect(html).toContain('Từ bị bỏ sót (Missing):');
      expect(html).toContain('Từ thừa (Extra):');
      expect(html).toContain('Cặp từ nhầm lẫn / đồng âm (Substitutions):');
      expect(html).toContain('Đúng: &quot;handmade&quot;');
      expect(html).toContain('Bạn nghe thành: &quot;hand made&quot;');
      expect(html).toContain('Giải thích hiện tượng ngữ âm tiếng Anh:');
      expect(html).toContain('Luyện nghe nối âm giữa phụ âm cuối và nguyên âm đầu.');
    });
  });

  describe('useTutorSessionStore Shared Speech Settings', () => {
    it('manages ttsModelCode, ttsAccent, and assessmentModelCode consistently', () => {
      const store = useTutorSessionStore.getState();

      store.setTtsModelCode('gemini-3.8-live-extended-thinking');
      expect(useTutorSessionStore.getState().ttsModelCode).toBe('gemini-3.8-live-extended-thinking');

      store.setTtsAccent('UK');
      expect(useTutorSessionStore.getState().ttsAccent).toBe('UK');

      store.setAssessmentModelCode('gemini-2.5-pro');
      expect(useTutorSessionStore.getState().assessmentModelCode).toBe('gemini-2.5-pro');

      // Reset to defaults
      store.setTtsModelCode('gemini-3.8-live');
      store.setTtsAccent('US');
      store.setAssessmentModelCode('gemini-3.8-flash');
    });
  });
});
