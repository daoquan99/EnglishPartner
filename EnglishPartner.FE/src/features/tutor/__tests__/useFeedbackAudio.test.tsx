import React, { act } from 'react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { createRoot } from 'react-dom/client';
import '@/shared/i18n';
import { useFeedbackAudio } from '../hooks/useFeedbackAudio';
import { vocabularyApi } from '@/features/vocabulary/api/vocabularyApi';

(globalThis as any).IS_REACT_ACT_ENVIRONMENT = true;

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
      if (childIdx >= 0) this.children.splice(childIdx, 0, c);
      else if (c instanceof MockElement) this.children.push(c);
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
};

mockWindow.document = mockDocument;
(globalThis as any).window = mockWindow;
(globalThis as any).document = mockDocument;

vi.mock('@/features/vocabulary/api/vocabularyApi', () => ({
  vocabularyApi: {
    synthesizePronunciationAudio: vi.fn(),
  },
}));

vi.mock('@/shared/lib/toast', () => ({
  toast: {
    error: vi.fn(),
    success: vi.fn(),
  },
}));

vi.mock('../store/tutorSessionStore', () => ({
  useTutorSessionStore: (selector: any) =>
    selector({
      apiKey: 'test-api-key',
      voiceName: 'Aoede',
      speechSpeed: 1.0,
      ttsAccent: 'US',
      ttsModelCode: 'gemini-3.8-live',
    }),
}));

function renderHook<T>(hookFn: () => T) {
  let hookResult: T = null as any;

  function Harness() {
    hookResult = hookFn();
    return null;
  }

  const container = mockDocument.createElement('div');
  const root = createRoot(container as any);

  act(() => {
    root.render(React.createElement(Harness));
  });

  return {
    result: {
      get current() {
        return hookResult;
      },
    },
    unmount() {
      act(() => {
        root.unmount();
      });
    },
  };
}

describe('useFeedbackAudio', () => {
  let playSpy: any;
  let pauseSpy: any;

  beforeEach(() => {
    vi.clearAllMocks();
    playSpy = vi.fn().mockResolvedValue(undefined);
    pauseSpy = vi.fn();

    // Mock HTMLAudioElement
    (globalThis as any).Audio = vi.fn().mockImplementation(() => ({
      play: playSpy,
      pause: pauseSpy,
      currentTime: 0,
    }));
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('falls back to vocabularyApi when session ended error code is returned', async () => {
    const mockRequestAudio = vi
      .fn()
      .mockRejectedValue(new Error('feedback_audio_session_ended'));

    const mockSynth = vi.spyOn(vocabularyApi, 'synthesizePronunciationAudio').mockResolvedValue({
      audioBase64: 'base64-from-fallback-api',
      mimeType: 'audio/wav',
      durationMs: 1000,
    } as any);

    const { result } = renderHook(() =>
      useFeedbackAudio({ onRequestFeedbackAudio: mockRequestAudio })
    );

    await act(async () => {
      await result.current.playFeedbackAudio('corr-1', 'corrected', 'She goes to school');
    });

    expect(mockRequestAudio).toHaveBeenCalledWith('corr-1', 'corrected');
    expect(mockSynth).toHaveBeenCalledWith(
      expect.objectContaining({
        text: 'She goes to school',
        apiKey: 'test-api-key',
      })
    );
    expect(playSpy).toHaveBeenCalled();
  });

  it('falls back to vocabularyApi when 404 is returned', async () => {
    const mockRequestAudio = vi
      .fn()
      .mockRejectedValue(new Error('Không tìm thấy hội thoại. (404)'));

    const mockSynth = vi.spyOn(vocabularyApi, 'synthesizePronunciationAudio').mockResolvedValue({
      audioBase64: 'base64-from-fallback-api-404',
      mimeType: 'audio/wav',
      durationMs: 1000,
    } as any);

    const { result } = renderHook(() =>
      useFeedbackAudio({ onRequestFeedbackAudio: mockRequestAudio })
    );

    await act(async () => {
      await result.current.playFeedbackAudio('corr-2', 'improved', 'She went to school');
    });

    expect(mockRequestAudio).toHaveBeenCalledWith('corr-2', 'improved');
    expect(mockSynth).toHaveBeenCalled();
    expect(playSpy).toHaveBeenCalled();
  });

  it('reuses module-level cache across multiple hook instances', async () => {
    const mockRequestAudio = vi.fn().mockResolvedValue({
      audioBase64: 'cached-audio-base64',
      mimeType: 'audio/wav',
      durationMs: 1000,
    });

    // Instance 1 (e.g. Panel)
    const { result: instance1 } = renderHook(() =>
      useFeedbackAudio({ onRequestFeedbackAudio: mockRequestAudio })
    );

    await act(async () => {
      await instance1.current.playFeedbackAudio('corr-shared', 'corrected', 'Distinct Sentence Text');
    });

    expect(mockRequestAudio).toHaveBeenCalledTimes(1);

    // Instance 2 (e.g. Modal)
    const { result: instance2 } = renderHook(() =>
      useFeedbackAudio({ onRequestFeedbackAudio: mockRequestAudio })
    );

    await act(async () => {
      await instance2.current.playFeedbackAudio('corr-shared', 'corrected', 'Distinct Sentence Text');
    });

    // Should NOT call mockRequestAudio again because it is in module-level cache
    expect(mockRequestAudio).toHaveBeenCalledTimes(1);
  });

  it('pauses currently playing audio when new audio is requested', async () => {
    const mockRequestAudio = vi.fn().mockResolvedValue({
      audioBase64: 'audio-data',
      mimeType: 'audio/wav',
      durationMs: 1000,
    });

    const { result } = renderHook(() =>
      useFeedbackAudio({ onRequestFeedbackAudio: mockRequestAudio })
    );

    await act(async () => {
      await result.current.playFeedbackAudio('corr-a', 'corrected', 'Sentence A');
    });

    await act(async () => {
      await result.current.playFeedbackAudio('corr-b', 'corrected', 'Sentence B');
    });

    expect(pauseSpy).toHaveBeenCalled();
  });
});
