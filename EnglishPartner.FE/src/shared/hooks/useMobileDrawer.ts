import { useState, useEffect, useRef, useCallback } from 'react';
import { useLocation } from 'react-router-dom';

/**
 * Hook to detect desktop vs mobile viewport via matchMedia.
 * Defaults to true for SSR and test environments.
 */
export function useIsDesktop(query = '(min-width: 1024px)'): boolean {
  const [isDesktop, setIsDesktop] = useState<boolean>(() => {
    if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
      return true;
    }
    return window.matchMedia(query).matches;
  });

  useEffect(() => {
    if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
      return;
    }
    const mediaQueryList = window.matchMedia(query);
    setIsDesktop(mediaQueryList.matches);

    const handler = (event: MediaQueryListEvent) => {
      setIsDesktop(event.matches);
    };

    if (typeof mediaQueryList.addEventListener === 'function') {
      mediaQueryList.addEventListener('change', handler);
      return () => mediaQueryList.removeEventListener('change', handler);
    } else {
      // Compatibility with older environments
      mediaQueryList.addListener(handler);
      return () => mediaQueryList.removeListener(handler);
    }
  }, [query]);

  return isDesktop;
}

const FOCUSABLE_SELECTOR = [
  'a[href]',
  'button:not([disabled])',
  'input:not([disabled])',
  'select:not([disabled])',
  'textarea:not([disabled])',
  '[tabindex]:not([tabindex="-1"])',
].join(', ');

export interface UseMobileDrawerOptions {
  initialOpen?: boolean;
}

/**
 * Shared mobile drawer hook:
 * - Tracks open/closed state.
 * - Closes on route change (useLocation).
 * - Closes on Escape key press.
 * - Focuses first focusable element inside drawer on open.
 * - Restores focus to trigger button on close.
 */
export function useMobileDrawer(options?: UseMobileDrawerOptions) {
  const [isOpen, setIsOpen] = useState(options?.initialOpen ?? false);
  const location = useLocation();
  const triggerRef = useRef<HTMLButtonElement | null>(null);
  const drawerRef = useRef<HTMLElement | null>(null);
  const wasOpenRef = useRef(false);

  const open = useCallback(() => setIsOpen(true), []);
  const close = useCallback(() => setIsOpen(false), []);
  const toggle = useCallback(() => setIsOpen((prev) => !prev), []);

  // Close on route change
  const currentPathRef = useRef(location.pathname);
  useEffect(() => {
    if (currentPathRef.current !== location.pathname) {
      currentPathRef.current = location.pathname;
      if (isOpen) {
        close();
      }
    }
  }, [location.pathname, isOpen, close]);

  // Close on Escape key press
  useEffect(() => {
    if (!isOpen) return;

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        event.preventDefault();
        close();
      }
    };

    if (typeof window !== 'undefined') {
      window.addEventListener('keydown', handleKeyDown);
      return () => window.removeEventListener('keydown', handleKeyDown);
    }
  }, [isOpen, close]);

  // Focus management:
  // When opened -> focus first focusable element inside drawer
  // When closed -> return focus to trigger button
  useEffect(() => {
    if (isOpen) {
      wasOpenRef.current = true;
      if (typeof window === 'undefined') return;

      const frameId = window.requestAnimationFrame(() => {
        if (!drawerRef.current) return;
        const firstFocusable = drawerRef.current.querySelector<HTMLElement>(FOCUSABLE_SELECTOR);
        if (firstFocusable && typeof firstFocusable.focus === 'function') {
          firstFocusable.focus();
        } else if (typeof drawerRef.current.focus === 'function') {
          drawerRef.current.focus();
        }
      });

      return () => window.cancelAnimationFrame(frameId);
    } else if (wasOpenRef.current) {
      wasOpenRef.current = false;
      if (triggerRef.current && typeof triggerRef.current.focus === 'function') {
        triggerRef.current.focus();
      }
    }
  }, [isOpen]);

  return {
    isOpen,
    open,
    close,
    toggle,
    setIsOpen,
    triggerRef,
    drawerRef,
  };
}
