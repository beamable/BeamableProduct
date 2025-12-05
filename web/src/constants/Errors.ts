/** Optional data for a {@link BeamError}. */
export interface BeamErrorOptions {
  /** Original error, if any. */
  cause?: unknown;
  /** Extra context for logs/analytics. */
  context?: Record<string, unknown>;
}

/**
 * Standard error thrown by the Beam SDK.
 * * `cause` – underlying error.
 * * `context` – arbitrary debug info.
 */
export class BeamError extends Error {
  readonly cause?: unknown;
  readonly context?: Record<string, unknown>;

  constructor(message: string, options: BeamErrorOptions = {}) {
    super(message);
    this.name = 'BeamError';
    this.cause = options.cause;
    this.context = options.context;

    // Ensure the prototype chain is set correctly for instanceof checks
    Object.setPrototypeOf(this, new.target.prototype);

    // Capture the stack trace if available
    if (Error.captureStackTrace) {
      Error.captureStackTrace(this, new.target);
    }
  }

  /** Checks if the given error is a BeamError or any of its subclasses. */
  static is(err: unknown): err is BeamError {
    return err instanceof BeamError;
  }

  /**
   * @returns A plain object suitable for logging or serializing.
   */
  toJSON(): Record<string, unknown> {
    return {
      name: this.name,
      message: this.message,
      context: this.context,
      ...(this.cause instanceof Error
        ? { cause: { name: this.cause.name, message: this.cause.message } }
        : { cause: this.cause }),
      stack: this.stack,
    };
  }
}

/** Error related to beam client web socket. */
export class BeamWebSocketError extends BeamError {
  constructor(message: string, options: BeamErrorOptions = {}) {
    super(message, options);
    this.name = 'BeamWebSocketError';
    // Ensure the prototype chain is set correctly for instanceof checks
    Object.setPrototypeOf(this, new.target.prototype);
  }
}

/** Error related to beam server web socket. */
export class BeamServerWebSocketError extends BeamError {
  constructor(message: string, options: BeamErrorOptions = {}) {
    super(message, options);
    this.name = 'BeamServerWebSocketError';
    // Ensure the prototype chain is set correctly for instanceof checks
    Object.setPrototypeOf(this, new.target.prototype);
  }
}

/** Error thrown when the Beam SDK fails to refresh the access token. */
export class RefreshAccessTokenError extends BeamError {
  constructor(message = 'Failed to refresh access token') {
    super(message);
    this.name = 'RefreshAccessTokenError';
    // Ensure the prototype chain is set correctly for instanceof checks
    Object.setPrototypeOf(this, new.target.prototype);
  }
}

/** Error thrown when there is no refresh token available to refresh access token. */
export class NoRefreshTokenError extends BeamError {
  constructor(message = 'No refresh token available') {
    super(message);
    this.name = 'NoRefreshTokenError';
    // Ensure the prototype chain is set correctly for instanceof checks
    Object.setPrototypeOf(this, new.target.prototype);
  }
}
