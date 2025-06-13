import { TokenResponse } from '@/__generated__/schemas';
import { TokenStorage } from '@/platform/types/TokenStorage';

/** A collection of utility functions for the Beam SDK. */
export class BeamUtils {
  /**
   * Saves the access token, refresh token, and expiration time from a token response to the token storage.
   * @param {TokenStorage} tokenStorage - The token storage instance to use.
   * @param {TokenResponse} tokenResponse - The token response object containing the tokens and expiration time.
   * @returns {Promise<void>} A promise that resolves when the tokens are saved.
   */
  static async saveToken(
    tokenStorage: TokenStorage,
    tokenResponse: TokenResponse,
  ): Promise<void> {
    await tokenStorage.setAccessToken(tokenResponse.access_token as string);
    await tokenStorage.setRefreshToken(tokenResponse.refresh_token as string);
    await tokenStorage.setExpiresIn(Number(tokenResponse.expires_in));
  }
}
