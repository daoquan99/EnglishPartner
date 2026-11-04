import React, { act } from 'react';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { createRoot, Root } from 'react-dom/client';
import { TutorSessionHeader } from '../components/TutorSessionHeader';
import type { ThreeHubStates } from '../types/tutor.types';
import '@/shared/i18n';

(globalThis as any).IS_REACT_ACT_ENVIRONMENT = true;

let currentActiveElement: any = null;
let matchMediaMatches = false;
const storageMap = new Map<string, string>();

const mockStorage: Storage = {
  get length() {
    return storageMap.size;
  },
  key(index: number) {
    return Array.from(storageMap.keys())[index] ?? null;
  },
  getItem(key: string) {
    return storageMap.get(key) ?? null;
  },
  setItem(key: string, value: string) {
    storageMap.set(key, String(value));
  },
  removeItem(key: string) {
    storageMap.delete(key);
  },
  clear() {
    storageMap.clear();
  },
};

class MockNode {}
class MockElement extends MockNode {
  tagName: string;
  nodeType = 1;
  children: MockElement[] = [];
  childNodes: any[] = [];
  style: Record<string, string> = {};
  attributes: Record<string, string> = {};
  parentNode: MockElement | null = null;
  ownerDocument: any = null;
  _listeners = new Map<string, Function[]>();

  constructor(tag?: string) {
    super();
    this.tagName = (tag || 'DIV').toUpperCase();
  }

  setAttribute(k: string, v: any) {
    this.attributes[k.toLowerCase()] = String(v);
  }

  removeAttribute(k: string) {
    delete this.attributes[k.toLowerCase()];
  }

  getAttribute(k: string) {
    return this.attributes[k.toLowerCase()] ?? null;
  }

  hasAttribute(k: string) {
    return k.toLowerCase() in this.attributes;
  }

  appendChild(c: any) {
    c.parentNode = this;
    this.childNodes.push(c);
    if (c instanceof MockElement) {
      this.children.push(c);
    }
    return c;
  }

  insertBefore(c: any, ref: any) {
    c.parentNode = this;
    const idx = this.childNodes.indexOf(ref);
    if (idx >= 0) {
      this.childNodes.splice(idx, 0, c);
      const childIdx = this.children.indexOf(ref);
      if (childIdx >= 0) {
        this.children.splice(childIdx, 0, c);
      } else if (c instanceof MockElement) {
        this.children.push(c);
      }
    } else {
      this.appendChild(c);
    }
    return c;
  }

  removeChild(c: any) {
    c.parentNode = null;
    const idx = this.childNodes.indexOf(c);
    if (idx >= 0) this.childNodes.splice(idx, 1);
    const childIdx = this.children.indexOf(c);
    if (childIdx >= 0) this.children.splice(childIdx, 1);
    return c;
  }

  addEventListener(type: string, fn: Function) {
    if (!this._listeners.has(type)) this._listeners.set(type, []);
    this._listeners.get(type)!.push(fn);
  }

  removeEventListener(type: string, fn: Function) {
    if (this._listeners.has(type)) {
      this._listeners.set(type, this._listeners.get(type)!.filter((l) => l !== fn));
    }
  }

  focus() {
    currentActiveElement = this;
  }

  click() {
    const propKey = Object.keys(this).find((k) => k.startsWith('__reactProps'));
    if (propKey && (this as any)[propKey]?.onClick) {
      (this as any)[propKey].onClick({ preventDefault: () => {}, stopPropagation: () => {} });
      return;
    }
    const list = this._listeners.get('click') || [];
    for (const fn of list) fn({ preventDefault: () => {}, stopPropagation: () => {} });
  }

  querySelector(selector: string): MockElement | null {
    const s = selector.toLowerCase();
    for (const child of this.children) {
      if (child instanceof MockElement) {
        if (s.includes('button') && child.tagName === 'BUTTON') return child;
        if (s.startsWith('#') && child.getAttribute('id') === s.slice(1)) return child;
        if (child.tagName.toLowerCase() === s) return child;
        const nested = child.querySelector(selector);
        if (nested) return nested;
      }
    }
    return null;
  }
}

class MockHTMLElement extends MockElement {}
class MockHTMLIFrameElement extends MockHTMLElement {}

(globalThis as any).Node = MockNode;
(globalThis as any).Element = MockElement;
(globalThis as any).HTMLElement = MockHTMLElement;
(globalThis as any).HTMLIFrameElement = MockHTMLIFrameElement;
(globalThis as any).Document = class {};

const mockWindow: any = {
  addEventListener() {},
  removeEventListener() {},
  requestAnimationFrame(cb: (time: number) => void) {
    cb(Date.now());
    return 1;
  },
  cancelAnimationFrame() {},
  matchMedia: (query: string) => ({
    matches: matchMediaMatches,
    media: query,
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    addListener: vi.fn(),
    removeListener: vi.fn(),
  }),
  localStorage: mockStorage,
  HTMLIFrameElement: MockHTMLIFrameElement,
  HTMLElement: MockHTMLElement,
  Element: MockElement,
  Node: MockNode,
};

const mockDocument: any = {
  nodeType: 9,
  createElement(tag: string) {
    const el = new MockElement(tag);
    el.ownerDocument = mockDocument;
    return el;
  },
  createElementNS(_ns: string, tag: string) {
    const el = new MockElement(tag);
    el.ownerDocument = mockDocument;
    return el;
  },
  createTextNode(text: string) {
    return { nodeType: 3, nodeValue: text, textContent: text, parentNode: null };
  },
  createComment(c: string) {
    return { nodeType: 8, nodeValue: c, parentNode: null };
  },
  addEventListener() {},
  removeEventListener() {},
  defaultView: mockWindow,
  get activeElement() {
    return currentActiveElement;
  },
};

mockWindow.document = mockDocument;
(globalThis as any).window = mockWindow;
(globalThis as any).document = mockDocument;
(globalThis as any).localStorage = mockStorage;

function findElement(root: MockElement, predicate: (el: MockElement) => boolean): MockElement | null {
  if (predicate(root)) return root;
  for (const child of root.children) {
    if (child instanceof MockElement) {
      const found = findElement(child, predicate);
      if (found) return found;
    }
  }
  return null;
}

function findAllElements(root: MockElement, predicate: (el: MockElement) => boolean): MockElement[] {
  const result: MockElement[] = [];
  if (predicate(root)) result.push(root);
  for (const child of root.children) {
    if (child instanceof MockElement) {
      result.push(...findAllElements(child, predicate));
    }
  }
  return result;
}

const mockHubStates: ThreeHubStates = {
  tutorHub: 'connected',
  transcribeHub: 'connected',
  translateHub: 'connected',
  grammarHub: 'connected',
};

describe('TutorSessionHeader Responsive Toggle & Accessibility', () => {
  let container: MockElement;
  let root: Root;

  beforeEach(() => {
    currentActiveElement = null;
    storageMap.clear();
    matchMediaMatches = false;
    container = mockDocument.createElement('div');
    root = createRoot(container as any);
  });

  afterEach(() => {
    act(() => {
      root.unmount();
    });
  });

  it('on mobile viewport: defaults to collapsed, clicking toggle expands and sets aria-expanded="true"', async () => {
    matchMediaMatches = false; // Mobile viewport (< 1024px)

    await act(async () => {
      root.render(
        <TutorSessionHeader
          cefrLevel="B1"
          conversationMode="Casual Talk"
          voiceName="Aoede"
          speechSpeed={1.0}
          topic="Travel"
          hubStates={mockHubStates}
          historyCount={3}
          onOpenSetup={vi.fn()}
          onNewConversation={vi.fn()}
          onOpenHistory={vi.fn()}
        />
      );
    });

    // Mobile collapsed: details container stays in the DOM (aria-controls target) but is hidden
    const detailsBefore = findElement(container, (e) => e.getAttribute('id') === 'tutor-header-details');
    expect(detailsBefore).not.toBeNull();
    expect(detailsBefore!.getAttribute('hidden')).not.toBeNull();

    // Toggle button should have aria-expanded="false" and aria-controls="tutor-header-details"
    const toggleButton = findElement(
      container,
      (e) => e.tagName === 'BUTTON' && e.getAttribute('aria-controls') === 'tutor-header-details'
    );
    expect(toggleButton).not.toBeNull();
    expect(toggleButton!.getAttribute('aria-expanded')).toBe('false');

    // Click toggle button to expand
    await act(async () => {
      toggleButton!.click();
    });

    // Details container is now visible
    const detailsAfter = findElement(container, (e) => e.getAttribute('id') === 'tutor-header-details');
    expect(detailsAfter).not.toBeNull();
    expect(detailsAfter!.getAttribute('hidden')).toBeNull();

    // Exactly one toggle, and it is the SAME element (not re-mounted), so keyboard focus is preserved
    const togglesExpanded = findAllElements(
      container,
      (e) => e.tagName === 'BUTTON' && e.getAttribute('aria-controls') === 'tutor-header-details'
    );
    expect(togglesExpanded).toHaveLength(1);
    expect(togglesExpanded[0]).toBe(toggleButton);
    expect(togglesExpanded[0].getAttribute('aria-expanded')).toBe('true');

    // Value should be persisted in localStorage
    expect(mockStorage.getItem('lingomint_tutor_header_expanded')).toBe('true');

    // Collapsing again persists 'false' and hides the details
    await act(async () => {
      toggleButton!.click();
    });
    expect(toggleButton!.getAttribute('aria-expanded')).toBe('false');
    expect(detailsAfter!.getAttribute('hidden')).not.toBeNull();
    expect(mockStorage.getItem('lingomint_tutor_header_expanded')).toBe('false');
  });

  it('does not crash when localStorage throws and still toggles', async () => {
    matchMediaMatches = false;
    const originalGet = mockStorage.getItem;
    const originalSet = mockStorage.setItem;
    mockStorage.getItem = () => {
      throw new Error('blocked');
    };
    mockStorage.setItem = () => {
      throw new Error('blocked');
    };
    try {
      await act(async () => {
        root.render(
          <TutorSessionHeader
            cefrLevel="B1"
            conversationMode="Casual Talk"
            voiceName="Aoede"
            speechSpeed={1.0}
            topic="Travel"
            hubStates={mockHubStates}
            historyCount={0}
            onOpenSetup={vi.fn()}
            onNewConversation={vi.fn()}
            onOpenHistory={vi.fn()}
          />
        );
      });
      const toggle = findElement(
        container,
        (e) => e.tagName === 'BUTTON' && e.getAttribute('aria-controls') === 'tutor-header-details'
      );
      expect(toggle!.getAttribute('aria-expanded')).toBe('false');
      await act(async () => {
        toggle!.click();
      });
      expect(toggle!.getAttribute('aria-expanded')).toBe('true');
    } finally {
      mockStorage.getItem = originalGet;
      mockStorage.setItem = originalSet;
    }
  });

  it('on desktop viewport: defaults to expanded with aria-expanded="true"', async () => {
    matchMediaMatches = true; // Desktop viewport (>= 1024px)

    await act(async () => {
      root.render(
        <TutorSessionHeader
          cefrLevel="B1"
          conversationMode="Casual Talk"
          voiceName="Aoede"
          speechSpeed={1.0}
          topic="Travel"
          hubStates={mockHubStates}
          historyCount={3}
          onOpenSetup={vi.fn()}
          onNewConversation={vi.fn()}
          onOpenHistory={vi.fn()}
        />
      );
    });

    // Desktop default: details container #tutor-header-details exists immediately
    const details = findElement(container, (e) => e.getAttribute('id') === 'tutor-header-details');
    expect(details).not.toBeNull();

    // Toggle buttons on desktop should have aria-expanded="true"
    const toggleButtons = findAllElements(
      container,
      (e) => e.tagName === 'BUTTON' && e.getAttribute('aria-controls') === 'tutor-header-details'
    );
    expect(toggleButtons.length).toBeGreaterThan(0);
    expect(toggleButtons[0].getAttribute('aria-expanded')).toBe('true');
  });

  it('respects stored user preference from localStorage when available', async () => {
    matchMediaMatches = false; // Mobile
    mockStorage.setItem('lingomint_tutor_header_expanded', 'true'); // Previously expanded by user

    await act(async () => {
      root.render(
        <TutorSessionHeader
          cefrLevel="B1"
          conversationMode="Casual Talk"
          voiceName="Aoede"
          speechSpeed={1.0}
          topic="Travel"
          hubStates={mockHubStates}
          historyCount={3}
          onOpenSetup={vi.fn()}
          onNewConversation={vi.fn()}
          onOpenHistory={vi.fn()}
        />
      );
    });

    // Should be expanded despite mobile viewport because of saved preference
    const details = findElement(container, (e) => e.getAttribute('id') === 'tutor-header-details');
    expect(details).not.toBeNull();
    expect(details!.getAttribute('hidden')).toBeNull();
  });
});
