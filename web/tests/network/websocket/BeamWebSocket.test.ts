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

  /** Deliver a frame, as a real socket would. */
  emit(data: string) {
    this.onmessage?.({ data } as any);
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

  it('sends a session-start frame as the first message after open', async () => {
    const sendSpy = vi.spyOn(MockWebSocket.prototype, 'send');
    const ws = new BeamWebSocket();

    const connectPromise = ws.connect({
      requester: fakeRequester,
      cid: 'cid-1',
      pid: 'pid-2',
      refreshToken: 'refresh-123',
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

  // --- message listeners ---

  const connectParams = {
    requester: fakeRequester,
    cid: 'cid-1',
    pid: 'pid-2',
    refreshToken: 'refresh-123',
    apiUrl: 'http://localhost:8080',
  };

  async function connected() {
    const ws = new BeamWebSocket();
    const promise = ws.connect(connectParams);
    await vi.runAllTimersAsync();
    await promise;
    return ws;
  }

  it('delivers frames to a registered listener', async () => {
    const ws = await connected();
    const received: string[] = [];
    ws.addListener((e) => received.push(e.data));

    ((ws as any).socket as MockWebSocket).emit('{"hello":1}');

    expect(received).toEqual(['{"hello":1}']);
  });

  it('keeps delivering after a reconnect replaces the underlying socket', async () => {
    // The reason listeners live on the wrapper. reconnect() builds a brand-new WebSocket, so a
    // listener attached to `rawSocket` would be bound to the discarded object and silently stop
    // firing — a subscription that dies on the first dropped connection.
    const ws = await connected();
    const received: string[] = [];
    ws.addListener((e) => received.push(e.data));

    const first = (ws as any).socket as MockWebSocket;
    first.close(1006, 'network blip');
    await vi.runAllTimersAsync();

    const second = (ws as any).socket as MockWebSocket;
    expect(second).not.toBe(first);

    second.emit('{"after":"reconnect"}');
    expect(received).toEqual(['{"after":"reconnect"}']);
  });

  it('isolates listeners from each other when one throws', async () => {
    // Each subscription used to have its own addEventListener, so the DOM isolated their failures.
    // Iterating our own list removes that isolation, and without the per-listener guard a single
    // bad handler would abort the loop and starve every listener registered after it.
    const ws = await connected();
    const reached: string[] = [];
    ws.addListener(() => {
      throw new Error('bad handler');
    });
    ws.addListener(() => reached.push('second'));

    expect(() =>
      ((ws as any).socket as MockWebSocket).emit('{"x":1}'),
    ).not.toThrow();
    expect(reached).toEqual(['second']);
  });

  it('removeListener detaches, and survives being called twice', async () => {
    const ws = await connected();
    const received: string[] = [];
    const listener = (e: MessageEvent) => received.push(e.data);
    ws.addListener(listener);
    ws.removeListener(listener);

    ((ws as any).socket as MockWebSocket).emit('{"x":1}');

    expect(received).toEqual([]);
    expect(() => ws.removeListener(listener)).not.toThrow();
  });

  it('a removed listener stays removed across a reconnect', async () => {
    // Removal has to follow the re-attachment, or `off` would look like it worked until the next
    // dropped connection brought the handler back.
    const ws = await connected();
    const received: string[] = [];
    const listener = (e: MessageEvent) => received.push(e.data);
    ws.addListener(listener);
    ws.removeListener(listener);

    ((ws as any).socket as MockWebSocket).close(1006, 'blip');
    await vi.runAllTimersAsync();
    ((ws as any).socket as MockWebSocket).emit('{"x":1}');

    expect(received).toEqual([]);
  });
});
