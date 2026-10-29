import React from 'react';
import { describe, it, expect } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import '@/shared/i18n';
import { TheoryRenderer, SafeHighlightedText } from '../components/TheoryRenderer';
import {
  mapGrammarExerciseToLearnerDto,
  mapGrammarAnswerToExerciseResult,
} from '../utils/grammarExerciseAdapter';
import type {
  GrammarTheoryDto,
  GrammarExerciseLearnerDto,
  GrammarAttemptAnswerDto,
} from '../types/grammar.types';

describe('TheoryRenderer & grammarExerciseAdapter', () => {
  describe('SafeHighlightedText (XSS safety & highlight)', () => {
    it('wraps only the first occurrence of highlight substring in <mark>', () => {
      const html = renderToStaticMarkup(
        <SafeHighlightedText text="She goes to school every day. She goes by bus." highlight="goes" />
      );

      expect(html).toContain('She <mark class="bg-amber-100 text-amber-900 font-semibold px-1 py-0.5 rounded">goes</mark> to school every day. She goes by bus.');
    });

    it('renders text with <script> purely as escaped text (never executes or injects raw HTML)', () => {
      const malicious = '<script>alert("XSS")</script>';
      const html = renderToStaticMarkup(
        <SafeHighlightedText text={malicious} highlight="alert" />
      );

      // React escapes < and > as &lt; and &gt;
      expect(html).not.toContain('<script>');
      expect(html).toContain('&lt;script&gt;');
      expect(html).toContain('<mark class="bg-amber-100 text-amber-900 font-semibold px-1 py-0.5 rounded">alert</mark>');
    });

    it('renders full text unchanged when highlight is null or not found', () => {
      const html = renderToStaticMarkup(
        <SafeHighlightedText text="Clean text with no highlight" highlight="missing" />
      );

      expect(html).toBe('Clean text with no highlight');
      expect(html).not.toContain('<mark>');
    });
  });

  describe('TheoryRenderer component', () => {
    const mockTheory: GrammarTheoryDto = {
      objectives: ['Master Present Simple for daily habits', 'Understand 3rd person singular s/es'],
      sections: [
        {
          heading: 'Khái niệm & Cách dùng',
          explanationVi: 'Diễn tả thói quen hoặc chân lý hiển nhiên.',
          formulas: [
            { label: 'Khẳng định', pattern: 'S + V(s/es) + O' },
            { label: 'Phủ định', pattern: 'S + do/does not + V_inf' },
          ],
          examples: [
            { en: 'He studies English every evening.', vi: 'Anh ấy học tiếng Anh mỗi tối.', highlight: 'studies' },
          ],
          notes: ['Với he/she/it động từ thêm -s hoặc -es.'],
        },
      ],
      commonMistakes: [
        {
          wrong: 'He study English.',
          right: 'He studies English.',
          explanationVi: 'Chủ ngữ ngôi thứ 3 số ít động từ phải chia đuôi -ies.',
        },
      ],
      tips: ['Nhớ các từ nhận biết: always, usually, often.'],
      summary: ['Thì hiện tại đơn dùng cho thói quen và sự thật.', 'Chú ý chia động từ ở ngôi thứ 3 số ít.'],
    };

    it('renders objectives, formulas table, examples, common mistakes, tips, and summary', () => {
      const html = renderToStaticMarkup(
        <MemoryRouter>
          <TheoryRenderer theory={mockTheory} prevCode="A1-01" nextCode="A1-03" />
        </MemoryRouter>
      );

      expect(html).toContain('Mục tiêu bài học');
      expect(html).toContain('Master Present Simple for daily habits');
      expect(html).toContain('Khái niệm &amp; Cách dùng');
      expect(html).toContain('S + V(s/es) + O');
      expect(html).toContain('studies');
      expect(html).toContain('Lưu ý quan trọng');
      expect(html).toContain('He study English.');
      expect(html).toContain('He studies English.');
      expect(html).toContain('Mẹo ghi nhớ &amp; Luyện tập');
      expect(html).toContain('Tóm tắt bài học (Summary)');
      expect(html).toContain('Bài trước (A1-01)');
      expect(html).toContain('Bài tiếp theo (A1-03)');
    });
  });

  describe('grammarExerciseAdapter', () => {
    it('mapGrammarExerciseToLearnerDto maps GrammarExerciseLearnerDto to ExerciseLearnerDto cleanly', () => {
      const grammarEx: GrammarExerciseLearnerDto = {
        id: 'g-ex-1',
        key: 'mc-01',
        type: 'MultipleChoice',
        title: 'Choose the correct verb form',
        instructions: 'Select one option.',
        order: 1,
        maxPoints: 10,
        gradingMode: 'Deterministic',
        content: { question: 'She ___ tennis.', options: [{ id: 'a', text: 'plays' }] },
        difficulty: 2,
        focus: 'Present Simple',
        source: 'Official',
        visibility: 'Public',
        moderation: 'Approved',
        authorName: 'Admin',
      };

      const learnerDto = mapGrammarExerciseToLearnerDto(grammarEx);

      expect(learnerDto).toEqual({
        id: 'g-ex-1',
        key: 'mc-01',
        type: 'MultipleChoice',
        title: 'Choose the correct verb form',
        instructions: 'Select one option.',
        order: 1,
        maxPoints: 10,
        gradingMode: 'Deterministic',
        content: { question: 'She ___ tennis.', options: [{ id: 'a', text: 'plays' }] },
      });
    });

    it('mapGrammarAnswerToExerciseResult maps answer and extracts suggestedAnswer and feedback structure', () => {
      const answer: GrammarAttemptAnswerDto = {
        exerciseId: 'g-ex-2',
        status: 'Completed',
        score100: 90,
        pointsEarned: 9,
        maxPoints: 10,
        feedback: {
          criteria: [{ code: 'ACCURACY', score: 9 }],
          strengths: ['Great vocabulary'],
          improvements: ['Minor punctuation'],
          suggestedAnswer: 'She plays tennis every weekend.',
        },
        answerPayload: { translatedText: 'She plays tennis.' },
      };

      const result = mapGrammarAnswerToExerciseResult(answer);

      expect(result.exerciseId).toBe('g-ex-2');
      expect(result.status).toBe('Completed');
      expect(result.score100).toBe(90);
      expect(result.pointsEarned).toBe(9);
      expect(result.maxPoints).toBe(10);
      expect(result.suggestedAnswer).toBe('She plays tennis every weekend.');
      expect(result.strengths).toEqual(['Great vocabulary']);
      expect(result.improvements).toEqual(['Minor punctuation']);
      expect(result.criteriaBreakdown).toEqual([{ code: 'ACCURACY', score: 9 }]);
    });
  });
});
