import React, { useCallback, useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Copy, PlusCircle, Sparkles } from 'lucide-react';
import { toast } from '@/shared/lib/toast';
import { cn } from '@/shared/lib/cn';
import { TargetedVocabularyDialog } from './TargetedVocabularyDialog';
import { normalizeTargetText } from '../lib/targetedVocabulary';

export function isEditableElement(element: Element | null): boolean {
  if (!element) return false;
  const tagName = element.tagName;
  if (tagName === 'INPUT' || tagName === 'TEXTAREA' || tagName === 'SELECT') {
    return true;
  }
  if ((element as HTMLElement).isContentEditable) {
    return true;
  }
  if (element.closest('input, textarea, select, [contenteditable="true"], [contenteditable=""]')) {
    return true;
  }
  return false;
}

export function isNodeInEditable(node: Node | null): boolean {
  if (!node) return false;
  const isElement = typeof Node !== 'undefined' ? node.nodeType === Node.ELEMENT_NODE : node.nodeType === 1;
  const el = isElement ? (node as Element) : node.parentElement;
  return isEditableElement(el);
}

export function shouldHandleContextMenuSelection(
  event: { shiftKey: boolean; target: EventTarget | null; clientX?: number; clientY?: number },
  selection: Selection | null
): boolean {
  if (event.shiftKey) {
    return false;
  }
  const target = event.target as HTMLElement | null;
  if (isEditableElement(target)) {
    return false;
  }
  if (!selection || selection.rangeCount === 0) {
    return false;
  }
  const rawText = selection.toString();
  const normalized = normalizeTargetText(rawText);
  if (!normalized || normalized.length < 1 || normalized.length > 500) {
    return false;
  }
  if (isNodeInEditable(selection.anchorNode) || isNodeInEditable(selection.focusNode)) {
    return false;
  }

  // Right click outside selection range bounding rects -> let browser native context menu show
  if (typeof event.clientX === 'number' && typeof event.clientY === 'number') {
    try {
      const range = selection.getRangeAt(0);
      const rects = range.getClientRects();
      if (rects && rects.length > 0) {
        let isInside = false;
        for (let i = 0; i < rects.length; i++) {
          const r = rects[i];
          if (
            event.clientX >= r.left - 2 &&
            event.clientX <= r.right + 2 &&
            event.clientY >= r.top - 2 &&
            event.clientY <= r.bottom + 2
          ) {
            isInside = true;
            break;
          }
        }
        if (!isInside) {
          return false;
        }
      }
    } catch {
      // Range rect calculation edge case, ignore
    }
  }

  return true;
}

export interface SelectionVocabularyMenuProps {
  container?: HTMLElement | null;
}

export const SelectionVocabularyMenu: React.FC<SelectionVocabularyMenuProps> = () => {
  const { t } = useTranslation();
  const location = useLocation();

  const [menuState, setMenuState] = useState<{
    isOpen: boolean;
    x: number;
    y: number;
    text: string;
  }>({
    isOpen: false,
    x: 0,
    y: 0,
    text: '',
  });

  const [dialogState, setDialogState] = useState<{
    isOpen: boolean;
    text: string;
    initialAssignToDaily: boolean;
  }>({
    isOpen: false,
    text: '',
    initialAssignToDaily: true,
  });

  const menuRef = useRef<HTMLDivElement | null>(null);
  const itemRefs = useRef<(HTMLButtonElement | null)[]>([]);
  const previousActiveElementRef = useRef<HTMLElement | null>(null);
  const focusTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [focusedIndex, setFocusedIndex] = useState(0);

  const closeMenu = useCallback(() => {
    setMenuState((prev) => (prev.isOpen ? { ...prev, isOpen: false } : prev));
  }, []);

  // Listen to contextmenu on document
  useEffect(() => {
    const handleContextMenu = (e: MouseEvent) => {
      const selection = typeof window !== 'undefined' ? window.getSelection() : null;
      if (!shouldHandleContextMenuSelection(e, selection)) {
        closeMenu();
        return;
      }

      e.preventDefault();

      previousActiveElementRef.current = document.activeElement as HTMLElement | null;
      const normalizedText = normalizeTargetText(selection!.toString());

      setMenuState({
        isOpen: true,
        x: e.clientX,
        y: e.clientY,
        text: normalizedText,
      });
      setFocusedIndex(0);
    };

    document.addEventListener('contextmenu', handleContextMenu);
    return () => {
      document.removeEventListener('contextmenu', handleContextMenu);
    };
  }, [closeMenu]);

  // Close when route changes
  useEffect(() => {
    closeMenu();
  }, [location.pathname, location.search, closeMenu]);

  // Close on Escape, click outside, scroll, resize
  useEffect(() => {
    if (!menuState.isOpen) return;

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        closeMenu();
      }
    };

    const handlePointerDown = (e: MouseEvent | PointerEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        closeMenu();
      }
    };

    const handleScroll = () => {
      closeMenu();
    };

    const handleResize = () => {
      closeMenu();
    };

    window.addEventListener('keydown', handleKeyDown);
    window.addEventListener('pointerdown', handlePointerDown);
    window.addEventListener('scroll', handleScroll, { capture: true, passive: true });
    window.addEventListener('resize', handleResize);

    return () => {
      window.removeEventListener('keydown', handleKeyDown);
      window.removeEventListener('pointerdown', handlePointerDown);
      window.removeEventListener('scroll', handleScroll, { capture: true });
      window.removeEventListener('resize', handleResize);
    };
  }, [menuState.isOpen, closeMenu]);

  // Focus management: focus the first item when menu opens, clean up timeout and restore previous focus on close
  useEffect(() => {
    if (menuState.isOpen) {
      focusTimeoutRef.current = setTimeout(() => {
        itemRefs.current[focusedIndex]?.focus();
      }, 30);
    } else {
      if (focusTimeoutRef.current) {
        clearTimeout(focusTimeoutRef.current);
        focusTimeoutRef.current = null;
      }
      if (previousActiveElementRef.current && typeof previousActiveElementRef.current.focus === 'function') {
        try {
          previousActiveElementRef.current.focus();
        } catch {
          // Previous element may be detached
        }
        previousActiveElementRef.current = null;
      }
    }

    return () => {
      if (focusTimeoutRef.current) {
        clearTimeout(focusTimeoutRef.current);
        focusTimeoutRef.current = null;
      }
    };
  }, [menuState.isOpen, focusedIndex]);

  const handleMenuKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      const next = (focusedIndex + 1) % 3;
      setFocusedIndex(next);
      itemRefs.current[next]?.focus();
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      const prev = (focusedIndex - 1 + 3) % 3;
      setFocusedIndex(prev);
      itemRefs.current[prev]?.focus();
    }
  };

  // Adjust menu position so it always stays completely inside the viewport
  const [adjustedPos, setAdjustedPos] = useState({ x: 0, y: 0 });
  useEffect(() => {
    if (!menuState.isOpen) return;
    const padding = 12;
    const menuWidth = menuRef.current?.offsetWidth || 260;
    const menuHeight = menuRef.current?.offsetHeight || 140;

    let targetX = menuState.x;
    let targetY = menuState.y;

    if (typeof window !== 'undefined') {
      if (targetX + menuWidth > window.innerWidth - padding) {
        targetX = Math.max(padding, window.innerWidth - menuWidth - padding);
      }
      if (targetY + menuHeight > window.innerHeight - padding) {
        targetY = Math.max(padding, window.innerHeight - menuHeight - padding);
      }
    }

    setAdjustedPos({ x: targetX, y: targetY });
  }, [menuState.isOpen, menuState.x, menuState.y]);

  const handleCreateVocabulary = () => {
    const text = menuState.text;
    closeMenu();
    setDialogState({
      isOpen: true,
      text,
      initialAssignToDaily: false,
    });
  };

  const handleCreateAndAddToDaily = () => {
    const text = menuState.text;
    closeMenu();
    setDialogState({
      isOpen: true,
      text,
      initialAssignToDaily: true,
    });
  };

  const handleCopyText = async () => {
    const text = menuState.text;
    closeMenu();
    try {
      if (navigator.clipboard && navigator.clipboard.writeText) {
        await navigator.clipboard.writeText(text);
      } else {
        const ta = document.createElement('textarea');
        ta.value = text;
        ta.style.position = 'fixed';
        ta.style.opacity = '0';
        document.body.appendChild(ta);
        ta.focus();
        ta.select();
        document.execCommand('copy');
        document.body.removeChild(ta);
      }
      toast.success(t('vocab.copiedToClipboard', 'Đã sao chép vào bộ nhớ tạm'));
    } catch {
      toast.error(t('vocab.copyTextFailed', 'Không thể sao chép vào bộ nhớ tạm'));
    }
  };

  return (
    <>
      {menuState.isOpen &&
        createPortal(
          <div
            ref={menuRef}
            role="menu"
            aria-label={t('vocab.selectionMenuAriaLabel', 'Menu ngữ cảnh tạo từ vựng')}
            onKeyDown={handleMenuKeyDown}
            style={{
              position: 'fixed',
              top: `${adjustedPos.y}px`,
              left: `${adjustedPos.x}px`,
              zIndex: 9999,
            }}
            className="w-64 rounded-2xl bg-white/95 backdrop-blur-md border border-slate-200/90 shadow-xl p-1.5 space-y-1 text-xs select-none animate-in fade-in zoom-in-95 duration-100"
          >
            <button
              ref={(el) => {
                itemRefs.current[0] = el;
              }}
              type="button"
              role="menuitem"
              tabIndex={focusedIndex === 0 ? 0 : -1}
              onClick={handleCreateVocabulary}
              className={cn(
                'w-full flex items-center gap-2.5 px-3 py-2 rounded-xl text-left font-semibold text-slate-700 hover:bg-emerald-50 hover:text-emerald-700 transition-colors cursor-pointer outline-hidden focus:bg-emerald-50 focus:text-emerald-700'
              )}
            >
              <Sparkles className="w-4 h-4 text-emerald-600 shrink-0" />
              <span className="truncate">{t('vocab.createVocabulary', 'Tạo từ vựng…')}</span>
            </button>

            <button
              ref={(el) => {
                itemRefs.current[1] = el;
              }}
              type="button"
              role="menuitem"
              tabIndex={focusedIndex === 1 ? 0 : -1}
              onClick={handleCreateAndAddToDaily}
              className={cn(
                'w-full flex items-center gap-2.5 px-3 py-2 rounded-xl text-left font-semibold text-slate-700 hover:bg-emerald-50 hover:text-emerald-700 transition-colors cursor-pointer outline-hidden focus:bg-emerald-50 focus:text-emerald-700'
              )}
            >
              <PlusCircle className="w-4 h-4 text-emerald-600 shrink-0" />
              <span className="truncate">
                {t('vocab.createAndAddToDaily', 'Tạo và thêm vào Daily hôm nay…')}
              </span>
            </button>

            <div className="h-px bg-slate-200 my-1" />

            <button
              ref={(el) => {
                itemRefs.current[2] = el;
              }}
              type="button"
              role="menuitem"
              tabIndex={focusedIndex === 2 ? 0 : -1}
              onClick={() => void handleCopyText()}
              className={cn(
                'w-full flex items-center gap-2.5 px-3 py-2 rounded-xl text-left font-semibold text-slate-700 hover:bg-slate-100 hover:text-slate-900 transition-colors cursor-pointer outline-hidden focus:bg-slate-100 focus:text-slate-900'
              )}
            >
              <Copy className="w-4 h-4 text-slate-500 shrink-0" />
              <span className="truncate">{t('vocab.copyText', 'Sao chép')}</span>
            </button>
          </div>,
          document.body
        )}

      <TargetedVocabularyDialog
        isOpen={dialogState.isOpen}
        onClose={() => setDialogState((prev) => ({ ...prev, isOpen: false }))}
        rawText={dialogState.text}
        initialAssignToDaily={dialogState.initialAssignToDaily}
      />
    </>
  );
};
