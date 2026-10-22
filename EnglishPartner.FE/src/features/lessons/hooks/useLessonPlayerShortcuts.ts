import { useEffect } from 'react';

export interface UseLessonPlayerShortcutsProps {
  onNext: () => void;
  onPrevious: () => void;
  onOpenSpeechSettings: () => void;
  onOpenShortcutsHelp: () => void;
  canGoNext: boolean;
  canGoPrevious: boolean;
  containerRef: React.RefObject<HTMLDivElement | null>;
}

export function useLessonPlayerShortcuts({
  onNext,
  onPrevious,
  onOpenSpeechSettings,
  onOpenShortcutsHelp,
  canGoNext,
  canGoPrevious,
  containerRef,
}: UseLessonPlayerShortcutsProps) {
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      const activeEl = document.activeElement as HTMLElement | null;
      const isInput =
        activeEl &&
        (activeEl.tagName === 'INPUT' ||
          activeEl.tagName === 'TEXTAREA' ||
          activeEl.isContentEditable);

      // 1. Submit Answer: Ctrl+Enter or Cmd+Enter (Works even when typing inside input/textarea!)
      if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
        const container = containerRef.current;
        if (container) {
          const submitBtn = Array.from(
            container.querySelectorAll<HTMLButtonElement>('button:not(:disabled)')
          ).find((btn) =>
            /xác nhận nộp|nộp bài|nộp các câu/i.test(btn.textContent || '')
          );
          if (submitBtn) {
            e.preventDefault();
            submitBtn.click();
            return;
          }
        }
      }

      // 2. Play / Stop Audio Sample: Ctrl+Space (or Alt+P)
      if ((e.ctrlKey || e.metaKey) && (e.code === 'Space' || e.key === ' ')) {
        const container = containerRef.current;
        if (container) {
          const audioBtn = Array.from(
            container.querySelectorAll<HTMLButtonElement>('button:not(:disabled)')
          ).find((btn) =>
            /nghe đọc|nghe câu|nghe phát âm|nghe lại|dừng nghe/i.test(btn.textContent || '')
          );
          if (audioBtn) {
            e.preventDefault();
            audioBtn.click();
            return;
          }
        }
      }

      // 3. Navigation Next: Alt + ArrowRight (or Ctrl + ArrowRight)
      if ((e.altKey || (e.ctrlKey && !e.shiftKey)) && e.key === 'ArrowRight') {
        if (canGoNext) {
          e.preventDefault();
          onNext();
          return;
        }
      }

      // 4. Navigation Previous: Alt + ArrowLeft (or Ctrl + ArrowLeft)
      if ((e.altKey || (e.ctrlKey && !e.shiftKey)) && e.key === 'ArrowLeft') {
        if (canGoPrevious) {
          e.preventDefault();
          onPrevious();
          return;
        }
      }

      // 5. Open AI Speech Settings: Alt + S
      if (e.altKey && (e.key === 's' || e.key === 'S')) {
        e.preventDefault();
        onOpenSpeechSettings();
        return;
      }

      // 6. Record toggle (Speaking / Shadowing): Alt + R
      if (e.altKey && (e.key === 'r' || e.key === 'R')) {
        const container = containerRef.current;
        if (container) {
          const recBtn = Array.from(
            container.querySelectorAll<HTMLButtonElement>('button:not(:disabled)')
          ).find((btn) =>
            /bắt đầu ghi âm|dừng ghi âm/i.test(btn.textContent || '')
          );
          if (recBtn) {
            e.preventDefault();
            recBtn.click();
            return;
          }
        }
      }

      // 7. Request AI Review (Dictation): Alt + I
      if (e.altKey && (e.key === 'i' || e.key === 'I')) {
        const container = containerRef.current;
        if (container) {
          const reviewBtn = Array.from(
            container.querySelectorAll<HTMLButtonElement>('button:not(:disabled)')
          ).find((btn) => /ai nhận xét bài chép/i.test(btn.textContent || ''));
          if (reviewBtn) {
            e.preventDefault();
            reviewBtn.click();
            return;
          }
        }
      }

      // 8. Open Shortcuts Cheatsheet Help: ? (Shift + /) when not in text input
      if (e.key === '?' && !isInput) {
        e.preventDefault();
        onOpenShortcutsHelp();
        return;
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [
    onNext,
    onPrevious,
    onOpenSpeechSettings,
    onOpenShortcutsHelp,
    canGoNext,
    canGoPrevious,
    containerRef,
  ]);
}
