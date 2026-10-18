import React from 'react';
import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { ReadingComprehensionPlayer } from '../components/player/ReadingComprehensionPlayer';
import { FillInTheBlanksPlayer } from '../components/player/FillInTheBlanksPlayer';
import { buildComprehensionSubmission, normalizeBlankText, normalizeQuestions } from '../components/player/lessonContent';

describe('Lesson player content from resume responses', () => {
  it('renders the supplied legacy text and {{blank}} markers as context and editable inputs', () => {
    const content = {
      text: 'Every weekday, I {{b1}} at 7 a.m. Then I {{b2}} for work.',
      blanks: [{ id: 'b1', hint: 'stop sleeping' }, { id: 'b2', hint: 'prepare yourself' }],
    };
    const html = renderToStaticMarkup(<FillInTheBlanksPlayer content={content} onSubmit={() => {}} />);
    expect(html).toContain('Every weekday, I ');
    expect(html).toContain('at 7 a.m. Then I ');
    expect(html.match(/<input /g)).toHaveLength(2);
    expect(html).toContain('aria-label="Ô trống 1"');
    expect(html.match(/<input[^>]*disabled/g)).toBeNull();
  });

  it('keeps canonical bracket markers and bilingual context intact', () => {
    expect(normalizeBlankText({ textWithBlanks: 'I [blank-1] coffee. Tôi uống cà phê.' }))
      .toBe('I [blank-1] coffee. Tôi uống cà phê.');
  });

  it('renders questionText and SingleChoice options instead of blank answer fields', () => {
    const html = renderToStaticMarkup(<ReadingComprehensionPlayer content={{
      passage: 'Nam works remotely from Da Nang.',
      subQuestions: [{ id: 'q-1', questionKind: 'SingleChoice', questionText: 'Where does Nam work?',
        options: [{ id: 'o-1', text: 'At home' }, { id: 'o-2', text: 'In an office' }], maxPoints: 5 }],
    }} onSubmit={() => {}} />);
    expect(html).toContain('Where does Nam work?');
    expect(html).toContain('At home');
    expect(html.match(/type="radio"/g)).toHaveLength(2);
    expect(html).not.toContain('type="text"');
  });

  it('renders editable, labelled fields for open questions and supports legacy prompt/kind', () => {
    const html = renderToStaticMarkup(<ReadingComprehensionPlayer content={{ passage: 'Passage', subQuestions: [
      { id: 'q-1', questionKind: 'ShortAnswer', questionText: 'Why work remotely?' },
      { id: 'q-2', kind: 'FillBlank', prompt: 'Nam lives in ___.' },
    ] }} onSubmit={() => {}} />);
    expect(html).toContain('Why work remotely?');
    expect(html).toContain('Nam lives in ___.');
    expect(html.match(/type="text"/g)).toHaveLength(2);
    expect(html.match(/<fieldset[^>]*disabled/g)).toBeNull();
  });

  it('submits stable question IDs, option sets and false without losing their types', () => {
    const questions = normalizeQuestions([
      { id: 'one', questionKind: 'SingleChoice', questionText: 'One' },
      { id: 'many', questionKind: 'MultipleChoice', questionText: 'Many' },
      { id: 'boolean', questionKind: 'TrueFalse', questionText: 'TF' },
      { id: 'text', questionKind: 'ShortAnswer', questionText: 'Text' },
    ]);
    expect(buildComprehensionSubmission(questions, { one: 'o-2', many: ['o-1', 'o-3'], boolean: false, text: '  At home  ' }))
      .toEqual({ subQuestionAnswers: [
        { questionId: 'one', selectedOptionIds: ['o-2'] },
        { questionId: 'many', selectedOptionIds: ['o-1', 'o-3'] },
        { questionId: 'boolean', answer: false },
        { questionId: 'text', textAnswer: 'At home' },
      ] });
  });
});
