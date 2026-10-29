import { describe, it, expect } from 'vitest';
import { resolveGrammarAttemptShortcut } from '../hooks/useGrammarAttemptShortcuts';

const key = (k: string, mods: Partial<{ ctrlKey: boolean; metaKey: boolean; altKey: boolean; shiftKey: boolean; repeat: boolean }> = {}) => ({
  key: k,
  ctrlKey: false,
  metaKey: false,
  altKey: false,
  shiftKey: false,
  ...mods,
});

describe('resolveGrammarAttemptShortcut', () => {
  it('Ctrl/Cmd + Enter submits, even while typing an answer', () => {
    expect(resolveGrammarAttemptShortcut(key('Enter', { ctrlKey: true }), false)).toBe('submit');
    expect(resolveGrammarAttemptShortcut(key('Enter', { metaKey: true }), true)).toBe('submit');
    expect(resolveGrammarAttemptShortcut(key('Enter', { ctrlKey: true, repeat: true }), false)).toBeNull();
  });

  it('Ctrl + arrows switch questions outside text fields only', () => {
    expect(resolveGrammarAttemptShortcut(key('ArrowRight', { ctrlKey: true }), false)).toBe('next');
    expect(resolveGrammarAttemptShortcut(key('ArrowLeft', { ctrlKey: true }), false)).toBe('previous');
    // Inside an input Ctrl+Arrow keeps jumping by word.
    expect(resolveGrammarAttemptShortcut(key('ArrowRight', { ctrlKey: true }), true)).toBeNull();
    // Text selection (Ctrl+Shift+Arrow) and browser history (Alt+Arrow) are untouched.
    expect(resolveGrammarAttemptShortcut(key('ArrowRight', { ctrlKey: true, shiftKey: true }), false)).toBeNull();
    expect(resolveGrammarAttemptShortcut(key('ArrowLeft', { altKey: true }), false)).toBeNull();
  });

  it('ignores keys without a modifier', () => {
    expect(resolveGrammarAttemptShortcut(key('Enter'), false)).toBeNull();
    expect(resolveGrammarAttemptShortcut(key('ArrowRight'), false)).toBeNull();
  });
});
