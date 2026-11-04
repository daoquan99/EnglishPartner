import React, { act } from 'react';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { createRoot } from 'react-dom/client';

(globalThis as any).IS_REACT_ACT_ENVIRONMENT = true;

let currentActiveElement: any = null;

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
  get activeElement() {
    return currentActiveElement;
  },
};

mockWindow.document = mockDocument;
(globalThis as any).window = mockWindow;
(globalThis as any).document = mockDocument;

// SignalR mock using vi.hoisted to prevent hoisting reference errors
const { mockHubsByUrl, MockHubConnectionBuilder } = vi.hoisted(() => {
  interface MockHubInstance {
    url: string;
    state: string;
    handlers: Map<string, Function[]>;
    reconnectedHandlers: Function[];
    reconnectingHandlers: Function[];
    closeHandlers: Function[];
    invoke: any;
    start: any;
    stop: any;
    on: any;
    off: any;
    onreconnected: any;
    onreconnecting: any;
    onclose: any;
    emit: (event: string, ...args: any[]) => void;
    simulateReconnect: (connectionId?: string) => Promise<void>;
    simulateReconnecting: (error?: Error) => void;
    simulateClose: (error?: Error) => void;
  }

  const mockHubsByUrl = new Map<string, MockHubInstance>();

  class MockHubConnectionBuilder {
    private targetUrl = '';

    withUrl(url: string, _options?: any) {
      this.targetUrl = url;
      return this;
    }

    withAutomaticReconnect(_delays?: any) {
      return this;
    }

    configureLogging(_level?: any) {
      return this;
    }

    build(): MockHubInstance {
      const handlers = new Map<string, Function[]>();
      const reconnectedHandlers: Function[] = [];
      const reconnectingHandlers: Function[] = [];
      const closeHandlers: Function[] = [];
      const hub: MockHubInstance = {
        url: this.targetUrl,
        state: 'Disconnected',
        handlers,
        reconnectedHandlers,
        reconnectingHandlers,
        closeHandlers,
        invoke: vi.fn().mockImplementation(async (_method: string, ..._args: any[]) => undefined),
        start: vi.fn().mockImplementation(async () => {
          hub.state = 'Connected';
        }),
        stop: vi.fn().mockImplementation(async () => {
          hub.state = 'Disconnected';
        }),
        on: vi.fn().mockImplementation((event: string, callback: Function) => {
          if (!handlers.has(event)) {
            handlers.set(event, []);
          }
          handlers.get(event)!.push(callback);
        }),
        off: vi.fn(),
        onreconnected: vi.fn().mockImplementation((cb: Function) => {
          reconnectedHandlers.push(cb);
        }),
        onreconnecting: vi.fn().mockImplementation((cb: Function) => {
          reconnectingHandlers.push(cb);
        }),
        onclose: vi.fn().mockImplementation((cb: Function) => {
          closeHandlers.push(cb);
        }),
        async simulateReconnect(connectionId = 'new-conn-123') {
          hub.state = 'Connected';
          for (const cb of reconnectedHandlers) {
            await cb(connectionId);
          }
        },
        simulateReconnecting(error?: Error) {
          hub.state = 'Reconnecting';
          for (const cb of reconnectingHandlers) {
            cb(error);
          }
        },
        simulateClose(error?: Error) {
          hub.state = 'Disconnected';
          for (const cb of closeHandlers) {
            cb(error);
          }
        },
        emit(event: string, ...args: any[]) {
          const list = handlers.get(event) || [];
          for (const cb of list) {
            cb(...args);
          }
        },
      };

      mockHubsByUrl.set(this.targetUrl, hub);
      return hub;
    }
  }

  return { mockHubsByUrl, MockHubConnectionBuilder };
});

vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: MockHubConnectionBuilder,
  HubConnectionState: {
    Disconnected: 'Disconnected',
    Connecting: 'Connecting',
    Connected: 'Connected',
    Disconnecting: 'Disconnecting',
    Reconnecting: 'Reconnecting',
  },
  LogLevel: {
    None: 0,
    Critical: 1,
    Error: 2,
    Warning: 3,
    Information: 4,
    Debug: 5,
    Trace: 6,
  },
}));

vi.mock('@/shared/api/httpClient', () => ({
  getAccessToken: () => 'mock-jwt-token',
  resolveBackendBaseUrl: () => 'http://localhost:5099',
}));

// Mock Web Audio & MediaDevices
class MockAudioContext {
  currentTime = 0;
  state = 'running';
  resume = vi.fn().mockResolvedValue(undefined);
  close = vi.fn().mockResolvedValue(undefined);
  createMediaStreamSource = vi.fn().mockReturnValue({ connect: vi.fn() });
  audioWorklet = {
    addModule: vi.fn().mockResolvedValue(undefined),
  };
}

class MockAudioWorkletNode {
  port = { onmessage: null };
  connect = vi.fn();
  disconnect = vi.fn();
}

(globalThis as any).AudioContext = MockAudioContext;
(globalThis as any).AudioWorkletNode = MockAudioWorkletNode;

const mockMediaStream = {
  getTracks: () => [{ stop: vi.fn() }],
};

(globalThis as any).navigator = {
  mediaDevices: {
    getUserMedia: vi.fn().mockResolvedValue(mockMediaStream),
  },
};

import { useThreeHubLiveSession } from '../hooks/useThreeHubLiveSession';

describe('useThreeHubLiveSession - Grammar Correction Hub Integration', () => {
  beforeEach(() => {
    mockHubsByUrl.clear();
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  function renderHookHarness() {
    let hookResult: ReturnType<typeof useThreeHubLiveSession> = null as any;

    function Harness() {
      hookResult = useThreeHubLiveSession();
      return null;
    }

    const container = mockDocument.createElement('div');
    const root = createRoot(container as any);

    act(() => {
      root.render(<Harness />);
    });

    return {
      get current() {
        return hookResult;
      },
      unmount() {
        act(() => {
          root.unmount();
        });
      },
    };
  }

  const defaultStartParams = {
    apiKey: 'AIzaSyFakeKeyForTesting1234567890',
    cefrLevel: 'B1' as const,
    extendedThinking: false,
    voiceName: 'Puck' as const,
    speechSpeed: 1.0,
    conversationMode: 'free_talk' as const,
    topic: 'Daily Life',
    conversationId: 'test-conv-123',
  };

  it('initializes /hubs/grammar and calls InitializeGrammarSession on startSession', async () => {
    const harness = renderHookHarness();

    await act(async () => {
      await harness.current.startSession(defaultStartParams);
    });

    const grammarHub = mockHubsByUrl.get('http://localhost:5099/hubs/grammar');
    expect(grammarHub).toBeDefined();
    expect(grammarHub!.start).toHaveBeenCalled();

    // Verify InitializeGrammarSession invocation with correct payload
    expect(grammarHub!.invoke).toHaveBeenCalledWith('InitializeGrammarSession', {
      conversationId: 'test-conv-123',
      apiKey: 'AIzaSyFakeKeyForTesting1234567890',
    });

    // Verify hubStates contains grammarHub connected
    expect(harness.current.hubStates.grammarHub).toBe('connected');

    harness.unmount();
  });

  it('invokes AnalyzeTurnGrammar on grammar hub, and NOT on tutor hub', async () => {
    const harness = renderHookHarness();

    await act(async () => {
      await harness.current.startSession(defaultStartParams);
    });

    const grammarHub = mockHubsByUrl.get('http://localhost:5099/hubs/grammar')!;
    const tutorHub = mockHubsByUrl.get('http://localhost:5099/hubs/tutor')!;

    // Call analyzeTurnGrammar
    await act(async () => {
      await harness.current.analyzeTurnGrammar('turn-test-1', 'She go to the library every morning.');
    });

    // Check grammar hub was invoked
    expect(grammarHub.invoke).toHaveBeenCalledWith(
      'AnalyzeTurnGrammar',
      'test-conv-123',
      'turn-test-1',
      'She go to the library every morning.',
      'B1'
    );

    // Tutor hub must NOT be invoked with AnalyzeTurnGrammar
    const tutorCalls = tutorHub.invoke.mock.calls;
    const tutorAnalyzeCalls = tutorCalls.filter((c: any[]) => c[0] === 'AnalyzeTurnGrammar');
    expect(tutorAnalyzeCalls).toHaveLength(0);

    harness.unmount();
  });

  it('updates corrections state when ReceiveLanguageCorrection is emitted from grammar hub', async () => {
    const harness = renderHookHarness();

    await act(async () => {
      await harness.current.startSession(defaultStartParams);
    });

    const grammarHub = mockHubsByUrl.get('http://localhost:5099/hubs/grammar')!;

    // Verify initial corrections are empty
    expect(harness.current.corrections).toHaveLength(0);

    // Simulate ReceiveLanguageCorrection event from grammar hub
    act(() => {
      grammarHub.emit('ReceiveLanguageCorrection', {
        turnId: 'turn-test-1',
        originalText: 'She go to school yesterday.',
        correctedText: 'She went to school yesterday.',
        errorCategory: 'grammar',
        explanationVi: 'Hành động trong quá khứ cần chia thì Quá khứ đơn (went).',
        cefrTip: 'B1: Sử dụng thì quá khứ đơn cho hành động đã kết thúc.',
      });
    });

    // Check state has received the correction
    expect(harness.current.corrections).toHaveLength(1);
    expect(harness.current.corrections[0]).toMatchObject({
      turnId: 'turn-test-1',
      originalSentence: 'She go to school yesterday.',
      correctedSentence: 'She went to school yesterday.',
      errorCategory: 'grammar',
      explanationVi: 'Hành động trong quá khứ cần chia thì Quá khứ đơn (went).',
    });

    harness.unmount();
  });

  it('calls InitializeGrammarSession when grammar hub reconnects', async () => {
    const harness = renderHookHarness();

    await act(async () => {
      await harness.current.startSession(defaultStartParams);
    });

    const grammarHub = mockHubsByUrl.get('http://localhost:5099/hubs/grammar')!;
    grammarHub.invoke.mockClear();

    // Simulate grammar hub reconnecting -> state becomes reconnecting
    act(() => {
      grammarHub.simulateReconnecting();
    });
    expect(harness.current.hubStates.grammarHub).toBe('reconnecting');

    // Simulate grammar hub reconnected -> re-invokes InitializeGrammarSession
    await act(async () => {
      await grammarHub.simulateReconnect('new-grammar-conn-123');
    });

    expect(harness.current.hubStates.grammarHub).toBe('connected');
    expect(grammarHub.invoke).toHaveBeenCalledWith('InitializeGrammarSession', {
      conversationId: 'test-conv-123',
      apiKey: defaultStartParams.apiKey,
    });

    harness.unmount();
  });

  it('does NOT automatically invoke AnalyzeTurnGrammar when grammar hub is connected', async () => {
    vi.useFakeTimers();
    const harness = renderHookHarness();

    await act(async () => {
      await harness.current.startSession(defaultStartParams);
    });

    const grammarHub = mockHubsByUrl.get('http://localhost:5099/hubs/grammar')!;
    const transcribeHub = mockHubsByUrl.get('http://localhost:5099/hubs/transcribe')!;
    grammarHub.invoke.mockClear();

    // User speaks and transcript arrives
    act(() => {
      transcribeHub.emit('ReceiveTranscriptLine1', {
        speaker: 'user',
        turnId: 'turn-user-1',
        sequenceOrder: 1,
        fullTranscriptText: 'I went to school yesterday.',
        isFinal: true,
      });
    });

    // Advance timers by 2 seconds (past 450ms and 1250ms)
    act(() => {
      vi.advanceTimersByTime(2000);
    });

    // Verify grammarHub.invoke was NOT called for AnalyzeTurnGrammar
    const grammarAnalyzeCalls = grammarHub.invoke.mock.calls.filter(
      (c: any[]) => c[0] === 'AnalyzeTurnGrammar'
    );
    expect(grammarAnalyzeCalls).toHaveLength(0);

    vi.useRealTimers();
    harness.unmount();
  });

  it('triggers client fallback when grammar hub is disconnected', async () => {
    vi.useFakeTimers();
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: true,
      json: async () => ({
        candidates: [
          {
            content: {
              parts: [
                {
                  text: JSON.stringify({
                    originalText: 'I goes to school.',
                    correctedText: 'I go to school.',
                    naturalAlternative: 'I walk to school.',
                    errorCategory: 'Grammar',
                    explanationVi: 'Lỗi chia động từ.',
                    cefrTip: '[B1] Chú ý thì hiện tại đơn.',
                  }),
                },
              ],
            },
          },
        ],
      }),
    } as any);

    const harness = renderHookHarness();

    await act(async () => {
      await harness.current.startSession(defaultStartParams);
    });

    const grammarHub = mockHubsByUrl.get('http://localhost:5099/hubs/grammar')!;
    const transcribeHub = mockHubsByUrl.get('http://localhost:5099/hubs/transcribe')!;

    // Grammar hub disconnects
    act(() => {
      grammarHub.simulateClose();
    });
    expect(harness.current.hubStates.grammarHub).toBe('disconnected');

    // User speech arrives
    act(() => {
      transcribeHub.emit('ReceiveTranscriptLine1', {
        speaker: 'user',
        turnId: 'turn-fallback-1',
        sequenceOrder: 1,
        fullTranscriptText: 'I goes to school.',
        isFinal: true,
      });
    });

    // Advance timer past 450ms to trigger fallback
    await act(async () => {
      vi.advanceTimersByTime(500);
    });

    // Client fallback was invoked (corrections list has the item)
    expect(harness.current.corrections.length).toBeGreaterThanOrEqual(1);
    expect(harness.current.corrections.some((c) => c.turnId === 'turn-fallback-1')).toBe(true);

    fetchSpy.mockRestore();
    vi.useRealTimers();
    harness.unmount();
  });

  it('re-analyze with forceReanalyze=true re-invokes grammar hub and allows repeated call', async () => {
    const harness = renderHookHarness();

    await act(async () => {
      await harness.current.startSession(defaultStartParams);
    });

    const grammarHub = mockHubsByUrl.get('http://localhost:5099/hubs/grammar')!;

    // Initial correction received
    act(() => {
      grammarHub.emit('ReceiveLanguageCorrection', {
        turnId: 'turn-failed-1',
        originalText: 'He go to school',
        correctedText: '',
        improvedText: '',
        errorCategory: 'None',
        explanationVi: 'Lỗi',
        cefrTip: 'Tip',
        status: 'analysis_failed',
      });
    });

    grammarHub.invoke.mockClear();

    // Calling analyze without forceReanalyze should be ignored
    await act(async () => {
      await harness.current.analyzeTurnGrammar('turn-failed-1', 'He go to school', false);
    });
    expect(grammarHub.invoke).not.toHaveBeenCalled();

    // Calling with forceReanalyze=true must invoke AnalyzeTurnGrammar
    await act(async () => {
      await harness.current.analyzeTurnGrammar('turn-failed-1', 'He go to school', true);
    });
    expect(grammarHub.invoke).toHaveBeenCalledWith(
      'AnalyzeTurnGrammar',
      'test-conv-123',
      'turn-failed-1',
      'He go to school',
      'B1'
    );

    harness.unmount();
  });

  it('does NOT trigger 1.6s client fallback when grammar hub is connected', async () => {
    vi.useFakeTimers();
    const fetchSpy = vi.spyOn(globalThis, 'fetch');

    const harness = renderHookHarness();
    await act(async () => {
      await harness.current.startSession(defaultStartParams);
    });

    const grammarHub = mockHubsByUrl.get('http://localhost:5099/hubs/grammar')!;
    grammarHub.invoke.mockClear();

    await act(async () => {
      await harness.current.analyzeTurnGrammar('turn-connected-1', 'She don\'t like apples');
    });

    expect(grammarHub.invoke).toHaveBeenCalledWith(
      'AnalyzeTurnGrammar',
      'test-conv-123',
      'turn-connected-1',
      'She don\'t like apples',
      'B1'
    );

    // Fast-forward 2 seconds
    await act(async () => {
      vi.advanceTimersByTime(2000);
    });

    // fetch (client fallback) was NOT called
    expect(fetchSpy).not.toHaveBeenCalled();

    fetchSpy.mockRestore();
    vi.useRealTimers();
    harness.unmount();
  });

  it('does NOT trigger Gemini client fallback when grammar hub is reconnecting', async () => {
    vi.useFakeTimers();
    const fetchSpy = vi.spyOn(globalThis, 'fetch');

    const harness = renderHookHarness();
    await act(async () => {
      await harness.current.startSession(defaultStartParams);
    });

    const grammarHub = mockHubsByUrl.get('http://localhost:5099/hubs/grammar')!;
    act(() => {
      grammarHub.simulateReconnecting();
    });

    await act(async () => {
      await harness.current.analyzeTurnGrammar('turn-recon-1', 'She don\'t like apples');
    });

    await act(async () => {
      vi.advanceTimersByTime(2000);
    });

    expect(fetchSpy).not.toHaveBeenCalled();

    fetchSpy.mockRestore();
    vi.useRealTimers();
    harness.unmount();
  });
});
