import type {
  TokenRequestWrapper,
  TokenResponse,
} from '@/__generated__/schemas';
import { getUserDeviceAndPlatform } from '@/utils/getUserDeviceAndPlatform';
import { ApiService, type ApiServiceProps } from '@/services/types/ApiService';
import { authPostTokenBasic } from '@/__generated__/apis';

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
   * const tokenResponse = await beam.auth.loginAsGuest();
   * ```
   * @throws {BeamError} If the authentication fails.
   */
  async loginAsGuest(): Promise<TokenResponse> {
    const { deviceType, platform } = getUserDeviceAndPlatform();
    const tokenRequest: TokenRequestWrapper = {
      grant_type: 'guest',
      context: {
        device: deviceType,
        platform,
      },
    };
    const { body } = await authPostTokenBasic(this.requester, tokenRequest);
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
    const { body } = await authPostTokenBasic(this.requester, tokenRequest);
    return body;
  }
}
