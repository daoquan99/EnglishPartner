import React from 'react';
import { describe, it, expect } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { FillInTheBlanksPlayer } from '../components/player/FillInTheBlanksPlayer';

describe('FillInTheBlanksPlayer line breaks', () => {
  it('keeps numbered sentences on separate lines', () => {
    const html = renderToStaticMarkup(
      <FillInTheBlanksPlayer
        content={{
          textWithBlanks: '1. I visit [blank-1] after school.\n2. [blank-2] rides it to school.',
          blanks: [{ id: 'blank-1' }, { id: 'blank-2' }] as never,
        }}
        onSubmit={() => {}}
      />
    );

    expect(html).toContain('whitespace-pre-line');
    expect(html).toContain('after school.\n2. ');
  });
});
