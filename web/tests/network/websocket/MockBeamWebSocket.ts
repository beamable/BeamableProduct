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
  /** Options applied to instances constructed without explicit options (e.g. by `Beam`). */
  static defaultOptions: MockBeamWebSocketOptions = {};
  /** The most recently constructed instance. */
  static lastInstance: MockBeamWebSocket | null = null;

  public connectParams: Record<string, any> | null = null;
  public isConnected = false;
  public disconnectCalled = false;
  private options: MockBeamWebSocketOptions;

  constructor(options?: MockBeamWebSocketOptions) {
    this.options = options ?? MockBeamWebSocket.defaultOptions;
    MockBeamWebSocket.lastInstance = this;
  }

  /**
   * Simulate opening a WebSocket connection.
   * @returns Promise that resolves or rejects based on options.
   */
  async connect(param: Record<string, any>): Promise<void> {
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
