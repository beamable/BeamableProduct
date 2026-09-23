import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  BeamWebSocket,
  DEFAULT_REALTIME_CONNECT_TIMEOUT_MS,
} from '@/network/websocket/BeamWebSocket';
import { BeamWebSocketError } from '@/constants/Errors';
import * as apis from '@/__generated__/apis';

// replace the wait helper with a no-op (instantly resolves)
vi.mock('@/utils/wait', () => ({ wait: () => Promise.resolve() }));

// promiseWithResolvers polyfill
vi.mock('@/utils/promiseWithResolvers', () => {
  const promiseWithResolvers = () => {
    let resolve!: (v?: unknown) => void;
    let reject!: (e?: unknown) => void;
    const promise = new Promise((r, j) => {
      resolve = r;
      reject = j;
    });
    return { promise, resolve, reject };
  };
  return { promiseWithResolvers };
});

// mock generated API calls for token refresh and realm config
vi.mock('@/__generated__/apis', () => {
  const authPostTokensRefreshToken = vi
    .fn()
    .mockResolvedValue({ body: { accessToken: 'access-from-refresh' } });
  const realmsGetClientDefaultsBasic = vi.fn().mockResolvedValue({
    body: { websocketConfig: { provider: 'beamable', uri: 'ws://test' } },
  });
  return { authPostTokensRefreshToken, realmsGetClientDefaultsBasic };
});

const fakeRequester: any = {};

/**
 * How the next MockWebSocket instances behave:
 * - `open`: opens asynchronously (the default)
 * - `never`: stays CONNECTING forever, like a socket stalled by a proxy
 * - `close`: closes (code 1006) without ever opening
 * - `handshake-error`: fails the handshake: error with readyState CLOSED, then close
 */
type SocketBehavior = 'open' | 'never' | 'close' | 'handshake-error';

// mock WebSocket implementation
class MockWebSocket {
  static behavior: SocketBehavior = 'open';
  static instances: MockWebSocket[] = [];

  static CONNECTING = 0;
  static OPEN = 1;
  static CLOSING = 2;
  static CLOSED = 3;

  readyState = MockWebSocket.CONNECTING;
  onopen: (() => void) | null = null;
  onmessage: ((e: any) => void) | null = null;
  onerror: ((e: any) => void) | null = null;
  onclose: ((e: any) => void) | null = null;

  constructor(public readonly url: string) {
    MockWebSocket.instances.push(this);
    const behavior = MockWebSocket.behavior;
    // simulate async connection establishment
    setTimeout(() => {
      switch (behavior) {
        case 'open':
          this.readyState = MockWebSocket.OPEN;
          this.onopen?.();
          break;
        case 'close':
          this.readyState = MockWebSocket.CLOSED;
          this.onclose?.({ code: 1006, reason: '' });
          break;
        case 'handshake-error':
          this.readyState = MockWebSocket.CLOSED;
          this.onerror?.({ type: 'error' });
          this.onclose?.({ code: 1006, reason: '' });
          break;
        case 'never':
          break;
      }
    }, 0);
  }

  close(code = 1000, reason = '') {
    this.readyState = MockWebSocket.CLOSED;
    this.onclose?.({ code, reason });
  }

  send(_data: any) {
    /* no-op */
  }
}

describe('BeamWebSocket', () => {
  let OriginalWS: any;

  beforeEach(() => {
    MockWebSocket.behavior = 'open';
    MockWebSocket.instances = [];
    vi.spyOn(console, 'warn').mockImplementation(() => {});
    OriginalWS = globalThis.WebSocket;
    (globalThis.WebSocket as any) = MockWebSocket;
    vi.useFakeTimers();
  });

  afterEach(() => {
    // restore the real WebSocket (if one existed)
    globalThis.WebSocket = OriginalWS;
    vi.useRealTimers();
    vi.unstubAllGlobals();
    vi.clearAllMocks();
    vi.mocked(console.warn).mockRestore();
  });

  it('connects successfully and resolves the promise', async () => {
    const ws = new BeamWebSocket();

    const connectPromise = ws.connect({
      requester: fakeRequester,
      cid: 'cid-1',
      pid: 'pid-2',
      refreshToken: 'refresh-123',
      apiUrl: 'https://api.test',
    });

    // advance the fake timers so the constructor `setTimeout` in MockWebSocket fires
    await vi.runAllTimersAsync();

    await expect(connectPromise).resolves.toBeUndefined();
    // refresh token endpoint was called once
    expect(apis.authPostTokensRefreshToken).toHaveBeenCalledTimes(1);
  });

  it('disconnect() closes the socket and can be called safely twice', async () => {
    const ws = new BeamWebSocket();

    const connectPromise = ws.connect({
      requester: fakeRequester,
      cid: 'cid-1',
      pid: 'pid-2',
      refreshToken: 'refresh-123',
      apiUrl: 'https://api.test',
    });
    await vi.runAllTimersAsync();

    await expect(connectPromise).resolves.toBeUndefined();

    // disconnect first time
    ws.disconnect();
    expect((ws as any).socket).toBeUndefined();

    // disconnect second time, this should be a harmless no-op
    expect(() => ws.disconnect()).not.toThrow();
  });

  it('fails the connect promise if no access token can be obtained', async () => {
    const ws = new BeamWebSocket();

    // patch the refresh-token API call to return null
    vi.spyOn(apis, 'authPostTokensRefreshToken').mockResolvedValueOnce({
      status: 200,
      headers: {},
      body: { accessToken: null },
    });

    const p = ws.connect({
      requester: fakeRequester,
      cid: 'cid-1',
      pid: 'pid-2',
      refreshToken: 'refresh-123',
      apiUrl: 'https://api.test',
    });

    await expect(p).rejects.toThrow(
      'Failed to obtain access token for WebSocket connection',
    );
  });

  it('sends a session-start frame as the first message after open', async () => {
    const sendSpy = vi.spyOn(MockWebSocket.prototype, 'send');
    const ws = new BeamWebSocket();

    const connectPromise = ws.connect({
      requester: fakeRequester,
      cid: 'cid-1',
      pid: 'pid-2',
      refreshToken: 'refresh-123',
      apiUrl: 'https://api.test',
    });
    await vi.runAllTimersAsync();
    await expect(connectPromise).resolves.toBeUndefined();

    expect(sendSpy).toHaveBeenCalledTimes(1);
    const payload = JSON.parse(sendSpy.mock.calls[0][0] as string);
    expect(payload.type).toBe('session-start');
    expect(typeof payload.device.platform).toBe('string');
    expect(typeof payload.device.model).toBe('string');
  });

  it('reconnects when the socket closes unexpectedly', async () => {
    const ws = new BeamWebSocket();

    const connectPromise = ws.connect({
      requester: fakeRequester,
      cid: 'cid-1',
      pid: 'pid-2',
      refreshToken: 'refresh-123',
      apiUrl: 'https://api.test',
    });
    await vi.runAllTimersAsync();

    await expect(connectPromise).resolves.toBeUndefined();

    // simulate socket close
    (ws as any).socket.onclose?.({ code: 1000, reason: 'Normal closure' });

    // wait for the reconnect() logic to kick in
    await vi.runAllTimersAsync();

    // check that the connect() promise resolves again
    expect(apis.authPostTokensRefreshToken).toHaveBeenCalledTimes(2);
    await expect(connectPromise).resolves.toBeUndefined();
  });

  const connectParams = (extra: Record<string, unknown> = {}) => ({
    requester: fakeRequester,
    cid: 'cid-1',
    pid: 'pid-2',
    refreshToken: 'refresh-123',
    apiUrl: 'https://api.test',
    ...extra,
  });

  it('rejects when the socket never opens within the default timeout', async () => {
    MockWebSocket.behavior = 'never';
    const ws = new BeamWebSocket();

    const p = ws.connect(connectParams());
    const settled = expect(p).rejects.toThrow(/did not open within 15000 ms/);

    await vi.advanceTimersByTimeAsync(DEFAULT_REALTIME_CONNECT_TIMEOUT_MS);
    await settled;
    await expect(p).rejects.toBeInstanceOf(BeamWebSocketError);
    await expect(p).rejects.toThrow(/WebSockets may be blocked/);
    // the error names the socket host
    await expect(p).rejects.toThrow(/test/);
  });

  it('honours a custom connectTimeoutMs', async () => {
    MockWebSocket.behavior = 'never';
    const ws = new BeamWebSocket();

    let rejected = false;
    const p = ws.connect(connectParams({ connectTimeoutMs: 500 }));
    p.catch(() => (rejected = true));

    await vi.advanceTimersByTimeAsync(499);
    expect(rejected).toBe(false);
    await vi.advanceTimersByTimeAsync(1);
    await expect(p).rejects.toThrow(/did not open within 500 ms/);
  });

  it('does not fire the timeout after the socket opened', async () => {
    const ws = new BeamWebSocket();
    const p = ws.connect(connectParams({ connectTimeoutMs: 1000 }));
    await vi.advanceTimersByTimeAsync(0);
    await expect(p).resolves.toBeUndefined();
    await vi.advanceTimersByTimeAsync(5000);
    expect((ws as any).socket).toBeDefined();
  });

  it('rejects after maxRetries when the socket keeps closing before it opens', async () => {
    MockWebSocket.behavior = 'close';
    const ws = new BeamWebSocket();

    const p = ws.connect(connectParams({ connectTimeoutMs: 0 }));
    const settled = expect(p).rejects.toThrow(
      /Maximum web socket reconnect attempts reached/,
    );
    await vi.runAllTimersAsync();
    await settled;
    // the initial attempt plus maxRetries (3) reconnects
    expect(MockWebSocket.instances).toHaveLength(4);
  });

  it('settles when a close arrives after a reconnect was interrupted', async () => {
    const ws = new BeamWebSocket();
    const first = ws.connect(connectParams());
    await vi.runAllTimersAsync();
    await expect(first).resolves.toBeUndefined();

    // A close starts a reconnect; disconnect() lands while it is waiting.
    (ws as any).socket.onclose?.({ code: 1006, reason: '' });
    ws.disconnect();
    await vi.runAllTimersAsync();
    expect((ws as any).isReconnecting).toBe(false);

    // A fresh connect whose sockets keep closing must still settle.
    MockWebSocket.behavior = 'close';
    const second = ws.connect(connectParams({ connectTimeoutMs: 0 }));
    const settled = expect(second).rejects.toThrow(
      /Maximum web socket reconnect attempts reached/,
    );
    await vi.runAllTimersAsync();
    await settled;
  });

  it('includes the HTTP status when the handshake is rejected', async () => {
    MockWebSocket.behavior = 'handshake-error';
    const fetchMock = vi.fn().mockResolvedValue({ status: 401 });
    vi.stubGlobal('fetch', fetchMock);
    const ws = new BeamWebSocket();

    const p = ws.connect(connectParams());
    const settled = expect(p).rejects.toThrow(/HTTP 401/);
    await vi.runAllTimersAsync();
    await settled;
    await expect(p).rejects.toThrow(/access token was not accepted/);
    // the probe hits the same URL over http(s)
    expect(fetchMock.mock.calls[0][0]).toMatch(/^http:\/\/test\/connect\?/);
    // the failed connect doesn't keep reconnecting in the background
    expect(MockWebSocket.instances).toHaveLength(1);
    expect((ws as any).socket).toBeUndefined();
  });

  it('reports a handshake failure without a status when it cannot be probed', async () => {
    MockWebSocket.behavior = 'handshake-error';
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('cors')));
    const ws = new BeamWebSocket();

    const p = ws.connect(connectParams());
    const settled = expect(p).rejects.toThrow(
      /WebSocket handshake with test failed/,
    );
    await vi.runAllTimersAsync();
    await settled;
  });

  it('includes the HTTP status when the refresh-token exchange fails', async () => {
    vi.spyOn(apis, 'authPostTokensRefreshToken').mockRejectedValueOnce(
      Object.assign(new Error('boom'), {
        context: { response: { status: 401 } },
      }),
    );
    const ws = new BeamWebSocket();

    await expect(ws.connect(connectParams())).rejects.toThrow(
      /Failed to obtain access token for WebSocket connection \(HTTP 401/,
    );
  });

  it('rejects a pending connect when disconnect() is called', async () => {
    MockWebSocket.behavior = 'never';
    const ws = new BeamWebSocket();
    const p = ws.connect(connectParams());
    await vi.advanceTimersByTimeAsync(0);
    ws.disconnect();
    await expect(p).rejects.toThrow(/disconnected before it opened/);
  });
});
