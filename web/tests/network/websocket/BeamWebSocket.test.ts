import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { BeamWebSocket } from '@/network/websocket/BeamWebSocket';
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

// mock WebSocket implementation
class MockWebSocket {
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
    // simulate async connection establishment
    setTimeout(() => {
      this.readyState = MockWebSocket.OPEN;
      this.onopen?.();
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
    OriginalWS = globalThis.WebSocket;
    (globalThis.WebSocket as any) = MockWebSocket;
    vi.useFakeTimers();
  });

  afterEach(() => {
    // restore the real WebSocket (if one existed)
    globalThis.WebSocket = OriginalWS;
    vi.useRealTimers();
    vi.clearAllMocks();
  });

  it('connects successfully and resolves the promise', async () => {
    const ws = new BeamWebSocket();

    const connectPromise = ws.connect({
      requester: fakeRequester,
      cid: 'cid-1',
      pid: 'pid-2',
      refreshToken: 'refresh-123',
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
    });

    await expect(p).rejects.toThrow(
      'Failed to obtain access token for WebSocket connection',
    );
  });

  it('reconnects when the socket closes unexpectedly', async () => {
    const ws = new BeamWebSocket();

    const connectPromise = ws.connect({
      requester: fakeRequester,
      cid: 'cid-1',
      pid: 'pid-2',
      refreshToken: 'refresh-123',
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
});
