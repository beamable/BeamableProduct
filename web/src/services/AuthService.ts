import { BeamApi } from '@/core/BeamApi';
import { TokenRequestWrapper, TokenResponse } from '@/__generated__/schemas';
import { getUserDeviceAndPlatform } from '@/utils/getUserDeviceAndPlatform';

export class AuthService {
  constructor(private readonly api: BeamApi) {}

  /**
   * Authenticates a guest user.
   * @returns {Promise<TokenResponse>} A promise that resolves with the token response.
   * @example
   * ```ts
   * const tokenResponse = await beam.auth.signInAsGuest();
   * ```
   */
  async signInAsGuest(): Promise<TokenResponse> {
    const { deviceType, platform } = getUserDeviceAndPlatform();
    const tokenRequest: TokenRequestWrapper = {
      grant_type: 'guest',
      context: {
        device: deviceType,
        platform,
      },
    };
    const { body } = await this.api.auth.postAuthToken(tokenRequest);
    return body;
  }

  /**
   * Requests a new access token using the stored refresh token.
   * @param {string} refreshToken - The refresh token to use.
   * @returns {Promise<TokenResponse>} A promise that resolves with the refreshed token response.
   * @example
   * ```ts
   * const tokenResponse = await beam.auth.refreshAuthToken("your-refresh-token");
   * ```
   */
  async refreshAuthToken(refreshToken: string): Promise<TokenResponse> {
    const tokenRequest: TokenRequestWrapper = {
      grant_type: 'refresh_token',
      refresh_token: refreshToken,
    };
    const { body } = await this.api.auth.postAuthToken(tokenRequest);
    return body;
  }
}
