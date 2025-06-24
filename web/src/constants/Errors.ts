/** Error thrown when web socket failed to connect. */
export class BeamWebSocketError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'BeamWebSocketError';
    Object.setPrototypeOf(this, new.target.prototype);
  }
}

/** Error thrown when the Beam SDK fails to refresh the access token. */
export class RefreshAccessTokenError extends Error {
  constructor(message = 'Failed to refresh access token') {
    super(message);
    this.name = 'RefreshAccessTokenError';
    Object.setPrototypeOf(this, new.target.prototype);
  }
}

/** Error thrown when there is no refresh token available to refresh access token. */
export class NoRefreshTokenError extends Error {
  constructor(message = 'No refresh token available') {
    super(message);
    this.name = 'NoRefreshTokenError';
    Object.setPrototypeOf(this, new.target.prototype);
  }
}
