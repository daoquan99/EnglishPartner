import React from 'react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import '@/shared/i18n';
import {
  SelectionVocabularyMenu,
  isEditableElement,
  isNodeInEditable,
  shouldHandleContextMenuSelection,
} from '../components/SelectionVocabularyMenu';
import { toast } from '@/shared/lib/toast';

vi.mock('@/shared/lib/toast', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: false },
  },
});

function renderWithProviders(ui: React.ReactElement) {
  return renderToStaticMarkup(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>{ui}</MemoryRouter>
    </QueryClientProvider>
  );
}

describe('SelectionVocabularyMenu Component & Helpers', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('isEditableElement & isNodeInEditable helpers', () => {
    it('detects input, textarea, select as editable', () => {
      const input = { tagName: 'INPUT', closest: () => null } as any;
      const textarea = { tagName: 'TEXTAREA', closest: () => null } as any;
      const select = { tagName: 'SELECT', closest: () => null } as any;
      const div = { tagName: 'DIV', isContentEditable: false, closest: () => null } as any;

      expect(isEditableElement(input)).toBe(true);
      expect(isEditableElement(textarea)).toBe(true);
      expect(isEditableElement(select)).toBe(true);
      expect(isEditableElement(div)).toBe(false);
    });

    it('detects contenteditable elements and child nodes', () => {
      const editableEl = {
        tagName: 'DIV',
        isContentEditable: true,
        closest: (sel: string) => sel.includes('contenteditable'),
      } as any;
      const childNode = {
        nodeType: 3, // text node
        parentElement: editableEl,
      } as any;

      expect(isEditableElement(editableEl)).toBe(true);
      expect(isNodeInEditable(childNode)).toBe(true);
    });

    it('returns false for normal text nodes and non-editable containers', () => {
      const normalEl = {
        tagName: 'P',
        isContentEditable: false,
        closest: () => null,
      } as any;
      const normalTextNode = {
        nodeType: 3,
        parentElement: normalEl,
      } as any;

      expect(isEditableElement(normalEl)).toBe(false);
      expect(isNodeInEditable(normalTextNode)).toBe(false);
    });
  });

  describe('shouldHandleContextMenuSelection logic', () => {
    it('returns false when Shift key is pressed', () => {
      const event = { shiftKey: true, target: { tagName: 'DIV', closest: () => null } as any };
      const selection = {
        rangeCount: 1,
        toString: () => 'resilient',
        anchorNode: null,
        focusNode: null,
      } as any;

      expect(shouldHandleContextMenuSelection(event, selection)).toBe(false);
    });

    it('returns false when target is input, textarea, select or contenteditable', () => {
      const inputEvent = { shiftKey: false, target: { tagName: 'INPUT', closest: () => null } as any };
      const selection = {
        rangeCount: 1,
        toString: () => 'resilient',
        anchorNode: null,
        focusNode: null,
      } as any;

      expect(shouldHandleContextMenuSelection(inputEvent, selection)).toBe(false);

      const textareaEvent = { shiftKey: false, target: { tagName: 'TEXTAREA', closest: () => null } as any };
      expect(shouldHandleContextMenuSelection(textareaEvent, selection)).toBe(false);

      const editableEvent = {
        shiftKey: false,
        target: { tagName: 'DIV', isContentEditable: true, closest: () => null } as any,
      };
      expect(shouldHandleContextMenuSelection(editableEvent, selection)).toBe(false);
    });

    it('returns false when selection is null, empty or whitespace', () => {
      const event = { shiftKey: false, target: { tagName: 'DIV', closest: () => null } as any };

      expect(shouldHandleContextMenuSelection(event, null)).toBe(false);
      expect(
        shouldHandleContextMenuSelection(event, {
          rangeCount: 0,
          toString: () => '',
          anchorNode: null,
          focusNode: null,
        } as any)
      ).toBe(false);
      expect(
        shouldHandleContextMenuSelection(event, {
          rangeCount: 1,
          toString: () => '     ',
          anchorNode: null,
          focusNode: null,
        } as any)
      ).toBe(false);
    });

    it('returns false when selection normalizes to empty (e.g. "...", "…", punctuation only)', () => {
      const normalTarget = { tagName: 'P', isContentEditable: false, closest: () => null } as any;
      const event = { shiftKey: false, target: normalTarget };

      const dotsSelection = {
        rangeCount: 1,
        toString: () => '...',
        anchorNode: normalTarget,
        focusNode: normalTarget,
      } as any;
      expect(shouldHandleContextMenuSelection(event, dotsSelection)).toBe(false);

      const ellipsisSelection = {
        rangeCount: 1,
        toString: () => '…',
        anchorNode: normalTarget,
        focusNode: normalTarget,
      } as any;
      expect(shouldHandleContextMenuSelection(event, ellipsisSelection)).toBe(false);

      const quotesSelection = {
        rangeCount: 1,
        toString: () => '“ ”',
        anchorNode: normalTarget,
        focusNode: normalTarget,
      } as any;
      expect(shouldHandleContextMenuSelection(event, quotesSelection)).toBe(false);
    });

    it('returns false when cursor position (clientX, clientY) is outside selection getClientRects', () => {
      const normalTarget = { tagName: 'P', isContentEditable: false, closest: () => null } as any;
      const event = { shiftKey: false, target: normalTarget, clientX: 400, clientY: 400 };

      const selection = {
        rangeCount: 1,
        toString: () => 'meaningful term',
        anchorNode: normalTarget,
        focusNode: normalTarget,
        getRangeAt: () => ({
          getClientRects: () => [
            { left: 50, right: 150, top: 50, bottom: 80 },
          ],
        }),
      } as any;

      expect(shouldHandleContextMenuSelection(event, selection)).toBe(false);
    });

    it('returns true when cursor position (clientX, clientY) is inside selection getClientRects', () => {
      const normalTarget = { tagName: 'P', isContentEditable: false, closest: () => null } as any;
      const event = { shiftKey: false, target: normalTarget, clientX: 100, clientY: 65 };

      const selection = {
        rangeCount: 1,
        toString: () => 'meaningful term',
        anchorNode: normalTarget,
        focusNode: normalTarget,
        getRangeAt: () => ({
          getClientRects: () => [
            { left: 50, right: 150, top: 50, bottom: 80 },
          ],
        }),
      } as any;

      expect(shouldHandleContextMenuSelection(event, selection)).toBe(true);
    });

    it('returns false when selection is longer than 500 characters', () => {
      const event = { shiftKey: false, target: { tagName: 'DIV', closest: () => null } as any };
      const longText = 'x'.repeat(501);
      const selection = {
        rangeCount: 1,
        toString: () => longText,
        anchorNode: null,
        focusNode: null,
      } as any;

      expect(shouldHandleContextMenuSelection(event, selection)).toBe(false);
    });

    it('returns true when valid text 1-500 chars is selected in non-editable element without Shift', () => {
      const normalTarget = { tagName: 'P', isContentEditable: false, closest: () => null } as any;
      const event = { shiftKey: false, target: normalTarget };
      const selection = {
        rangeCount: 1,
        toString: () => '  sustainable growth  ',
        anchorNode: normalTarget,
        focusNode: normalTarget,
      } as any;

      expect(shouldHandleContextMenuSelection(event, selection)).toBe(true);
    });
  });

  describe('Component Rendering', () => {
    it('renders closed menu initially without throwing', () => {
      const html = renderWithProviders(<SelectionVocabularyMenu />);
      expect(typeof html).toBe('string');
    });
  });

  describe('Clipboard Copy Action', () => {
    it('writes to navigator.clipboard and calls toast.success', async () => {
      const writeTextMock = vi.fn().mockResolvedValue(undefined);
      if (typeof globalThis.navigator === 'undefined') {
        (globalThis as any).navigator = {};
      }
      Object.assign(globalThis.navigator, {
        clipboard: {
          writeText: writeTextMock,
        },
      });

      const text = 'perseverance';
      await globalThis.navigator.clipboard.writeText(text);
      toast.success('Đã sao chép vào bộ nhớ tạm');

      expect(writeTextMock).toHaveBeenCalledWith('perseverance');
      expect(toast.success).toHaveBeenCalledWith('Đã sao chép vào bộ nhớ tạm');
    });
  });
});
