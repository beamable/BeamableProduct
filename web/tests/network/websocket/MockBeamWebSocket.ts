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
}
