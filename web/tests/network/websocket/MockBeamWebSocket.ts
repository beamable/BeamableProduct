/**
 * Mock implementation of BeamWebSocket for testing.
 * Provides a simple connect/disconnect API without real networking.
 */
export interface MockBeamWebSocketOptions {
  /** If true, connect() will reject with the provided error. */
  connectShouldReject?: boolean;
  /** Error to use when rejecting connect(). */
  connectRejectError?: Error;
}

export class MockBeamWebSocket {
  public connectParams: {
    api: any;
    url: string;
    cid: string;
    pid: string;
    refreshToken: string;
  } | null = null;
  public isConnected = false;
  public disconnectCalled = false;
  private options: MockBeamWebSocketOptions;

  constructor(options: MockBeamWebSocketOptions = {}) {
    this.options = options;
  }

  /**
   * Simulate opening a WebSocket connection.
   * @returns Promise that resolves or rejects based on options.
   */
  async connect(param: {
    api: any;
    url: string;
    cid: string;
    pid: string;
    refreshToken: string;
  }): Promise<void> {
    this.connectParams = param;
    if (this.options.connectShouldReject) {
      const err =
        this.options.connectRejectError ??
        new Error('MockBeamWebSocket connect failed');
      return Promise.reject(err);
    }
    this.isConnected = true;
    return Promise.resolve();
  }

  /**
   * Simulate closing the WebSocket connection.
   */
  disconnect(): void {
    this.disconnectCalled = true;
    this.isConnected = false;
  }

  /** Alias for disconnect(). */
  dispose(): void {
    this.disconnect();
  }

  // --- message listeners (mirrors the real BeamWebSocket) ---

  private messageListeners = new Set<(event: MessageEvent) => void>();

  addListener(listener: (event: MessageEvent) => void): void {
    this.messageListeners.add(listener);
  }

  removeListener(listener: (event: MessageEvent) => void): void {
    this.messageListeners.delete(listener);
  }

  /** How many listeners are currently registered — lets a test assert `off` actually detached. */
  get listenerCount(): number {
    return this.messageListeners.size;
  }

  /**
   * Deliver a frame to every listener, as the real socket's `onmessage` does — including the
   * per-listener try/catch, so a throwing handler cannot starve the ones after it.
   *
   * @param context The notification context, e.g. 'segments.transition'.
   * @param payload The inner payload; serialized into `messageFull`, which is a JSON string
   *   *inside* the JSON frame exactly as the gateway sends it.
   */
  emit(context: string, payload: unknown): void {
    this.emitRaw(
      JSON.stringify({ context, messageFull: JSON.stringify(payload) }),
    );
  }

  /** Deliver an arbitrary frame body, for malformed-input cases. */
  emitRaw(data: string): void {
    const event = { data } as MessageEvent;
    for (const listener of [...this.messageListeners]) {
      try {
        listener(event);
      } catch {
        /* the real socket logs and continues */
      }
    }
  }
}
