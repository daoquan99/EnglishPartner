import { describe, it, expect } from 'vitest';
import { shouldConfirmOnEnter } from '../ConfirmDialog';

describe('ConfirmDialog Enter shortcut', () => {
  it('confirms on a plain Enter press', () => {
    expect(shouldConfirmOnEnter({ key: 'Enter', target: { tagName: 'DIV' } as unknown as EventTarget })).toBe(true);
    expect(shouldConfirmOnEnter({ key: 'Enter', target: null })).toBe(true);
  });

  it('ignores other keys, repeats, IME composition and Enter on focusable controls', () => {
    expect(shouldConfirmOnEnter({ key: 'Escape' })).toBe(false);
    expect(shouldConfirmOnEnter({ key: 'Enter', repeat: true })).toBe(false);
    expect(shouldConfirmOnEnter({ key: 'Enter', isComposing: true })).toBe(false);
    expect(shouldConfirmOnEnter({ key: 'Enter', defaultPrevented: true })).toBe(false);
    for (const tagName of ['BUTTON', 'TEXTAREA', 'INPUT', 'A', 'SELECT']) {
      expect(shouldConfirmOnEnter({ key: 'Enter', target: { tagName } as unknown as EventTarget })).toBe(false);
    }
  });
});
