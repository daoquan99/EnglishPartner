import React, { act, useEffect } from 'react';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { createRoot, Root } from 'react-dom/client';
import { MemoryRouter, useNavigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useMobileDrawer } from '../useMobileDrawer';
import { StudentLayout } from '@/app/layouts/StudentLayout';
import { useAuthStore } from '@/features/auth/store/authStore';
import type { UserProfileDto } from '@/shared/api/openapi.types';
import '@/shared/i18n';

vi.mock('@/shared/config/features', () => ({ isAgyLocalEnabled: false }));

// Setup global mock DOM for react-dom/client in node/jsdom test runner
(globalThis as any).IS_REACT_ACT_ENVIRONMENT = true;

let currentActiveElement: any = null;
const windowEventListeners = new Map<string, Function[]>();
let matchMediaMatches = false;

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
        if (s.includes('a') && child.tagName === 'A') return child;
        if (s.includes('input') && child.tagName === 'INPUT') return child;
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
  addEventListener(type: string, fn: Function) {
    if (!windowEventListeners.has(type)) windowEventListeners.set(type, []);
    windowEventListeners.get(type)!.push(fn);
  },
  removeEventListener(type: string, fn: Function) {
    if (windowEventListeners.has(type)) {
      windowEventListeners.set(type, windowEventListeners.get(type)!.filter((l) => l !== fn));
    }
  },
  dispatchEvent(event: { type: string; key?: string; defaultPrevented?: boolean; preventDefault?: () => void }) {
    const list = windowEventListeners.get(event.type) || [];
    for (const fn of list) {
      fn(event);
    }
    return true;
  },
  requestAnimationFrame(cb: (time: number) => void) {
    cb(Date.now());
    return 1;
  },
  cancelAnimationFrame(_id: number) {},
  matchMedia: (query: string) => ({
    matches: matchMediaMatches,
    media: query,
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    addListener: vi.fn(),
    removeListener: vi.fn(),
  }),
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

const mockLearner: UserProfileDto = {
  id: 'learner-1',
  username: 'student1',
  email: 'student1@lingomint.local',
  fullName: 'Student One',
  preferredCefrLevel: 'B1',
  preferredVoiceName: 'Aoede',
  timeZoneId: 'Asia/Ho_Chi_Minh',
  isActive: true,
  roles: ['LEARNER'],
  permissions: ['conversations.access', 'vocabularies.read', 'grammar.read'],
};

describe('useMobileDrawer - DOM Render Tests (react-dom/client + act)', () => {
  let container: MockElement;
  let root: Root;

  beforeEach(() => {
    currentActiveElement = null;
    windowEventListeners.clear();
    matchMediaMatches = false;
    container = mockDocument.createElement('div');
    root = createRoot(container as any);
  });

  afterEach(() => {
    act(() => {
      root.unmount();
    });
  });

  it('Escape closes drawer when open', async () => {
    let drawerApi: ReturnType<typeof useMobileDrawer> | null = null;

    function TestHarness() {
      drawerApi = useMobileDrawer({ initialOpen: true });
      return (
        <div>
          <button ref={drawerApi.triggerRef} type="button">
            Menu Trigger
          </button>
          <aside ref={drawerApi.drawerRef as any}>
            <button type="button">Drawer First Button</button>
          </aside>
        </div>
      );
    }

    await act(async () => {
      root.render(
        <MemoryRouter initialEntries={['/student/tutor']}>
          <TestHarness />
        </MemoryRouter>
      );
    });

    expect(drawerApi).not.toBeNull();
    expect(drawerApi!.isOpen).toBe(true);

    // Dispatch Escape keydown on window
    const preventDefault = vi.fn();
    await act(async () => {
      mockWindow.dispatchEvent({
        type: 'keydown',
        key: 'Escape',
        preventDefault,
      });
    });

    expect(preventDefault).toHaveBeenCalled();
    expect(drawerApi!.isOpen).toBe(false);
  });

  it('closes drawer on route change', async () => {
    let drawerApi: ReturnType<typeof useMobileDrawer> | null = null;
    let navigateFn: any = null;

    function TestHarness() {
      drawerApi = useMobileDrawer({ initialOpen: true });
      const navigate = useNavigate();
      useEffect(() => {
        navigateFn = navigate;
      }, [navigate]);

      return (
        <div>
          <button ref={drawerApi.triggerRef} type="button">
            Menu Trigger
          </button>
          <aside ref={drawerApi.drawerRef as any}>Drawer Content</aside>
        </div>
      );
    }

    await act(async () => {
      root.render(
        <MemoryRouter initialEntries={['/student/tutor']}>
          <TestHarness />
        </MemoryRouter>
      );
    });

    expect(drawerApi!.isOpen).toBe(true);

    // Navigate to new route
    await act(async () => {
      navigateFn('/student/vocabulary');
    });

    expect(drawerApi!.isOpen).toBe(false);
  });

  it('focuses first focusable element inside drawer on open', async () => {
    let drawerApi: ReturnType<typeof useMobileDrawer> | null = null;
    let firstDrawerButton: any = null;

    function TestHarness() {
      drawerApi = useMobileDrawer({ initialOpen: false });
      return (
        <div>
          <button ref={drawerApi.triggerRef} type="button">
            Menu Trigger
          </button>
          <aside ref={drawerApi.drawerRef as any}>
            <button
              ref={(el) => {
                firstDrawerButton = el;
              }}
              type="button"
            >
              Drawer First Action
            </button>
          </aside>
        </div>
      );
    }

    await act(async () => {
      root.render(
        <MemoryRouter initialEntries={['/student/tutor']}>
          <TestHarness />
        </MemoryRouter>
      );
    });

    expect(drawerApi!.isOpen).toBe(false);

    // Open drawer
    await act(async () => {
      drawerApi!.open();
    });

    expect(drawerApi!.isOpen).toBe(true);
    expect(currentActiveElement).toBe(firstDrawerButton);
  });

  it('returns focus to trigger button on close', async () => {
    let drawerApi: ReturnType<typeof useMobileDrawer> | null = null;
    let triggerButton: any = null;

    function TestHarness() {
      drawerApi = useMobileDrawer({ initialOpen: false });
      return (
        <div>
          <button
            ref={(el) => {
              triggerButton = el;
              drawerApi!.triggerRef.current = el as any;
            }}
            type="button"
          >
            Menu Trigger
          </button>
          <aside ref={drawerApi.drawerRef as any}>
            <button type="button">Drawer First Action</button>
          </aside>
        </div>
      );
    }

    await act(async () => {
      root.render(
        <MemoryRouter initialEntries={['/student/tutor']}>
          <TestHarness />
        </MemoryRouter>
      );
    });

    // Open drawer
    await act(async () => {
      drawerApi!.open();
    });
    expect(drawerApi!.isOpen).toBe(true);

    // Close drawer
    await act(async () => {
      drawerApi!.close();
    });

    expect(drawerApi!.isOpen).toBe(false);
    expect(currentActiveElement).toBe(triggerButton);
  });

  describe('StudentLayout Accessibility & Inert Contract', () => {
    let queryClient: QueryClient;

    beforeEach(() => {
      (useAuthStore as any).getInitialState = () => useAuthStore.getState();
      (useAuthStore as any).getServerSnapshot = () => useAuthStore.getState();
      queryClient = new QueryClient({
        defaultOptions: { queries: { retry: false } },
      });
      queryClient.setQueryData(['auth', 'me'], mockLearner);
      useAuthStore.setState({
        isAuthenticated: true,
        isInitialized: true,
        currentUser: mockLearner,
        accessToken: 'test-token',
      });
    });

    function findByPredicate(el: MockElement, pred: (e: MockElement) => boolean): MockElement | null {
      if (pred(el)) return el;
      for (const child of el.children) {
        if (child instanceof MockElement) {
          const res = findByPredicate(child, pred);
          if (res) return res;
        }
      }
      return null;
    }

    it('on mobile when drawer closed: aside has inert attribute and main does not', async () => {
      matchMediaMatches = false; // Mobile viewport

      await act(async () => {
        root.render(
          <QueryClientProvider client={queryClient}>
            <MemoryRouter initialEntries={['/student/tutor']}>
              <StudentLayout />
            </MemoryRouter>
          </QueryClientProvider>
        );
      });

      const aside = findByPredicate(container, (e) => e.tagName === 'ASIDE' && e.getAttribute('id') === 'student-sidebar');
      const main = findByPredicate(container, (e) => e.tagName === 'MAIN');

      expect(aside).not.toBeNull();
      expect(main).not.toBeNull();

      // Mobile closed: aside must have inert, role should NOT be dialog
      expect(aside!.hasAttribute('inert')).toBe(true);
      expect(aside!.getAttribute('role')).toBeNull();

      // main must not have inert while drawer is closed
      expect(main!.hasAttribute('inert')).toBe(false);
    });

    it('on mobile when drawer open: aside has role="dialog" and aria-modal="true", main has inert attribute', async () => {
      matchMediaMatches = false; // Mobile viewport

      await act(async () => {
        root.render(
          <QueryClientProvider client={queryClient}>
            <MemoryRouter initialEntries={['/student/tutor']}>
              <StudentLayout />
            </MemoryRouter>
          </QueryClientProvider>
        );
      });

      // Find the mobile hamburger menu button to open drawer
      const menuTrigger = findByPredicate(container, (e) => e.tagName === 'BUTTON' && e.getAttribute('aria-controls') === 'student-sidebar');
      expect(menuTrigger).not.toBeNull();

      // Trigger click on menu button to open drawer
      await act(async () => {
        menuTrigger!.click();
      });

      const aside = findByPredicate(container, (e) => e.tagName === 'ASIDE' && e.getAttribute('id') === 'student-sidebar');
      const main = findByPredicate(container, (e) => e.tagName === 'MAIN');

      expect(aside).not.toBeNull();
      expect(main).not.toBeNull();

      // When open on mobile: aside loses inert, acquires dialog semantics
      expect(aside!.hasAttribute('inert')).toBe(false);
      expect(aside!.getAttribute('role')).toBe('dialog');
      expect(aside!.getAttribute('aria-modal')).toBe('true');
      expect(aside!.getAttribute('aria-label')).toBeTruthy();

      // main becomes inert so focus cannot escape into background
      expect(main!.hasAttribute('inert')).toBe(true);
    });

    it('on desktop: neither aside nor main has inert attribute', async () => {
      matchMediaMatches = true; // Desktop viewport (>= 1024px)

      await act(async () => {
        root.render(
          <QueryClientProvider client={queryClient}>
            <MemoryRouter initialEntries={['/student/tutor']}>
              <StudentLayout />
            </MemoryRouter>
          </QueryClientProvider>
        );
      });

      const aside = findByPredicate(container, (e) => e.tagName === 'ASIDE' && e.getAttribute('id') === 'student-sidebar');
      const main = findByPredicate(container, (e) => e.tagName === 'MAIN');

      expect(aside).not.toBeNull();
      expect(main).not.toBeNull();

      // Desktop: neither element has inert
      expect(aside!.hasAttribute('inert')).toBe(false);
      expect(main!.hasAttribute('inert')).toBe(false);
      expect(aside!.getAttribute('role')).toBeNull();
    });
  });
});
