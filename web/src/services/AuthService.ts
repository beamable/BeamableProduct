import { TokenRequestWrapper, TokenResponse } from '@/__generated__/schemas';
import { getUserDeviceAndPlatform } from '@/utils/getUserDeviceAndPlatform';
import { ApiService, type ApiServiceProps } from '@/services/types/ApiService';

export interface RefreshTokenParams {
  refreshToken: string;
}

export class AuthService extends ApiService {
  /** @internal */
  constructor(props: ApiServiceProps) {
    super(props);
  }

  /**
   * Authenticates a guest user.
   * @example
   * ```ts
   * const tokenResponse = await beam.auth.signInAsGuest();
   * ```
   * @throws {BeamError} If the authentication fails.
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
   * @example
   * ```ts
   * const tokenResponse = await beam.auth.refreshAuthToken({ refreshToken: "your-refresh-token" });
   * ```
   * @throws {BeamError} If the refresh token is invalid or the request fails.
   */
  async refreshAuthToken(params: RefreshTokenParams): Promise<TokenResponse> {
    const tokenRequest: TokenRequestWrapper = {
      grant_type: 'refresh_token',
      refresh_token: params.refreshToken,
    };
    const { body } = await this.api.auth.postAuthToken(tokenRequest);
    return body;
  }
}
