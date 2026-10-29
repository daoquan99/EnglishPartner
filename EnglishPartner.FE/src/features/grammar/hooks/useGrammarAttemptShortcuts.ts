import { useEffect, useRef } from 'react';

export type GrammarAttemptShortcut = 'submit' | 'next' | 'previous';

/**
 * Maps a keydown to an attempt shortcut.
 * - Ctrl/Cmd + Enter: submit the current exercise (also while typing in an answer field).
 * - Ctrl/Cmd + ArrowRight / ArrowLeft: next / previous question, except inside text fields where the
 *   combination keeps its native "jump by word" behaviour.
 */
export function resolveGrammarAttemptShortcut(
  event: { key: string; ctrlKey: boolean; metaKey: boolean; altKey: boolean; shiftKey: boolean; repeat?: boolean },
  isEditingText: boolean
): GrammarAttemptShortcut | null {
  const mod = event.ctrlKey || event.metaKey;
  if (!mod || event.altKey) return null;
  if (event.key === 'Enter' && !event.repeat) return 'submit';
  if (isEditingText || event.shiftKey) return null;
  if (event.key === 'ArrowRight') return 'next';
  if (event.key === 'ArrowLeft') return 'previous';
  return null;
}

function isEditingText(el: Element | null): boolean {
  if (!el) return false;
  const tag = el.tagName.toUpperCase();
  return tag === 'TEXTAREA' || tag === 'INPUT' || (el as HTMLElement).isContentEditable;
}

interface UseGrammarAttemptShortcutsOptions {
  containerRef: React.RefObject<HTMLElement | null>;
  onNext: () => void;
  onPrevious: () => void;
  enabled?: boolean;
}

export function useGrammarAttemptShortcuts({ containerRef, onNext, onPrevious, enabled = true }: UseGrammarAttemptShortcutsOptions) {
  const handlers = useRef({ onNext, onPrevious });
  handlers.current = { onNext, onPrevious };

  useEffect(() => {
    if (!enabled) return;
    const handleKeyDown = (event: KeyboardEvent) => {
      // An open dialog (e.g. submit confirmation) owns the keyboard.
      if (document.querySelector('[role="dialog"]')) return;
      const action = resolveGrammarAttemptShortcut(event, isEditingText(document.activeElement));
      if (!action) return;

      if (action === 'submit') {
        const submitButton = containerRef.current?.querySelector<HTMLButtonElement>(
          'button[data-exercise-submit]:not(:disabled)'
        );
        if (!submitButton) return;
        event.preventDefault();
        submitButton.click();
        return;
      }

      event.preventDefault();
      if (action === 'next') handlers.current.onNext();
      else handlers.current.onPrevious();
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [enabled, containerRef]);
}
