import type {
  ChallengeSolution,
  TokenRequestWrapper,
  TokenResponse,
} from '@/__generated__/schemas';
import { getUserDeviceAndPlatform } from '@/utils/getUserDeviceAndPlatform';
import { ApiService, type ApiServiceProps } from '@/services/types/ApiService';
import { authPostTokenBasic } from '@/__generated__/apis';
import { AccountService } from '@/services/AccountService';
import { CredentialStatus, ThirdPartyAuthProvider } from '@/services/enums';
import { BeamError } from '@/constants/Errors';
import { Beam } from '@/core/Beam';

export interface LoginWithEmailParams {
  email: string;
  password: string;
}

export interface LoginWithThirdPartyParams {
  provider: ThirdPartyAuthProvider;
  token: string;
}

export interface LoginWithExternalIdentityParams {
  externalToken: string;
  providerService: string;
  providerNamespace: string;
  challengeHandler?: (challenge: string) => string | Promise<string>;
}

export interface ExternalChallengeResponse {
  /** The user ID in the external system (wallet ID, OAuth ID, etc.) */
  user_id?: string;
  /** The challenge token associated with the external authentication. */
  challenge_token: string;
  /**
   * The time-to-live (TTL) of the challenge.
   * When provided, it indicates that the external authentication is pending verification.
   */
  challenge_ttl: number;
}

export interface ExternalAuthResponse {
  tokenResponse?: TokenResponse;
  challengeResponse?: ExternalChallengeResponse;
}

export interface RefreshTokenParams {
  refreshToken: string;
}

export class AuthService extends ApiService {
  constructor(props: ApiServiceProps) {
    super(props);
  }

  /** @internal */
  get serviceName(): string {
    return 'auth';
  }

  /**
   * Authenticates a guest user.
   * @example
   * ```ts
   * const tokenResponse = await beam.auth.loginAsGuest();
   * await beam.refresh(tokenResponse);
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
   * Authenticates a user with an email and password.
   * @example
   * ```ts
   * const tokenResponse = await beam.auth.loginWithEmail({
   *   email: "user@example.com",
   *   password: "password123"
   * });
   * await beam.refresh(tokenResponse);
   * ```
   * @throws {BeamError} If the authentication fails.
   */
  async loginWithEmail(params: LoginWithEmailParams): Promise<TokenResponse> {
    const { deviceType, platform } = getUserDeviceAndPlatform();
    const tokenRequest: TokenRequestWrapper = {
      grant_type: 'password',
      username: params.email,
      password: params.password,
      context: {
        device: deviceType,
        platform,
      },
    };
    const { body } = await authPostTokenBasic(this.requester, tokenRequest);
    return body;
  }

  /**
   * Authenticates a user with a third-party provider (e.g., Google, Facebook).
   * @example
   * ```ts
   * const tokenResponse = await beam.auth.loginWithThirdParty({
   *   provider: ThirdPartyAuthProvider.Google,
   *   token: "google-auth-token"
   * });
   * await beam.refresh(tokenResponse);
   * ```
   * @throws {BeamError} If the authentication fails.
   */
  async loginWithThirdParty(
    params: LoginWithThirdPartyParams,
  ): Promise<TokenResponse> {
    const { deviceType, platform } = getUserDeviceAndPlatform();
    const tokenRequest: TokenRequestWrapper = {
      grant_type: 'third_party',
      third_party: params.provider,
      token: params.token,
      context: {
        device: deviceType,
        platform,
      },
    };
    const { body } = await authPostTokenBasic(this.requester, tokenRequest);
    return body;
  }

  /**
   * Authenticates a user with an external identity.
   * @example
   * ```ts
   * const tokenResponse = await beam.auth.loginWithExternalIdentity({
   *   externalToken: 'acme-auth-token',
   *   providerService: acmeServiceClient.serviceName,
   *   providerNamespace: acmeServiceClient.federationIds.acme,
   *   // optional
   *   challengeHandler: (challenge) => {
   *     // Handle the challenge, e.g., by displaying a CAPTCHA or OTP to the user
   *     return prompt(challenge);
   *   },
   * });
   * await beam.refresh(tokenResponse);
   *
   * ```
   * @throws {BeamError} If the authentication fails.
   */
  async loginWithExternalIdentity(
    params: LoginWithExternalIdentityParams,
  ): Promise<TokenResponse> {
    const loginFn = async (
      externalToken: string,
      providerService: string,
      providerNamespace: string,
      challengeSolution?: ChallengeSolution,
    ): Promise<ExternalAuthResponse> => {
      const { deviceType, platform } = getUserDeviceAndPlatform();
      const tokenRequest: TokenRequestWrapper = {
        grant_type: 'external',
        external_token: externalToken,
        provider_service: providerService,
        provider_namespace: providerNamespace,
        challenge_solution: challengeSolution,
        context: {
          device: deviceType,
          platform,
        },
      };
      const { body } = await authPostTokenBasic(this.requester, tokenRequest);

      if (body.token_type === 'challenge') {
        // challenge_token should be present
        const challenge_token = body.challenge_token;
        if (!challenge_token) {
          throw new BeamError('No challenge token returned');
        }

        return {
          challengeResponse: {
            challenge_token,
            challenge_ttl: Number(body.expires_in),
          },
        };
      } else {
        return {
          tokenResponse: body,
        };
      }
    };

    const handleLoginFnResponse = async (response: ExternalAuthResponse) => {
      if (response.tokenResponse) return response.tokenResponse;

      if (!response.challengeResponse)
        throw new BeamError('Missing challenge response');

      if (!params.challengeHandler)
        throw new BeamError(
          'A challenge was requested but no challenge handler provided',
        );

      const challenge_token = response.challengeResponse.challenge_token;
      const solution = await params.challengeHandler(challenge_token);
      const nextResponse = await loginFn(
        params.externalToken,
        params.providerService,
        params.providerNamespace,
        { challenge_token, solution },
      );
      return await handleLoginFnResponse(nextResponse);
    };

    const response = await loginFn(
      params.externalToken,
      params.providerService,
      params.providerNamespace,
    );
    return await handleLoginFnResponse(response);
  }

  /**
   * Orchestrates a third-party login / linking flow
   * @remarks
   * This method handles the logic for logging in with a third-party provider
   * or linking a third-party account to an existing Beamable account.
   * This will only work in the client SDK and auto refresh the Beam instance.
   * @example
   * ```ts
   * await beam.auth.handleThirdPartyAuthFlow({
   *   provider: ThirdPartyAuthProvider.Google,
   *   token: 'google-auth-token',
   * });
   * ```
   * @throws {BeamError} If the authentication flow fails.
   */
  async handleThirdPartyAuthFlow(
    params: LoginWithThirdPartyParams,
  ): Promise<void> {
    if (!this.player) {
      throw new BeamError(
        '`handleThirdPartyAuthFlow` is only available in the client SDK.',
      );
    }

    const { provider } = params;
    const account = new AccountService({
      beam: this.beam,
      getPlayer: () => (this.beam as Beam).player,
    });

    // Lookup the credential status for this third-party token
    const status = await account.getThirdPartyStatus(params);
    switch (status) {
      // Unknown third-party token
      case CredentialStatus.Unknown:
        throw new BeamError(
          'Unknown credential status: invalid token or unconfigured third-party provider',
        );

      // Third-party token already assigned (switch user)
      case CredentialStatus.Assigned: {
        const tokenResponse = await this.loginWithThirdParty(params);
        await this.refreshBeam(tokenResponse);
        return;
      }

      // Third-party token free to use
      case CredentialStatus.NotAssigned: {
        // Do we already have same third-party provider on the current account?
        const linkedToCurrentUser =
          this.player.hasThirdPartyAssociation(provider);

        // Create a new guest if current user is already linked
        if (linkedToCurrentUser) {
          const guestTokenResponse = await this.loginAsGuest();
          await this.refreshBeam(guestTokenResponse);
          // New Beam context -> new AccountService bound to that context
          const freshAccount = new AccountService({
            beam: this.beam,
            getPlayer: () => (this.beam as Beam).player,
          });
          await freshAccount.addThirdParty(params);
          return;
        }

        // Simply link the provider to the current user
        await account.addThirdParty(params);
      }
    }
  }

  /**
   * Requests a new access token using the stored refresh token.
   * @example
   * ```ts
   * const tokenResponse = await beam.auth.refreshAuthToken({
   *   refreshToken: "your-refresh-token"
   * });
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

  /** Helper : refresh Beam if we’re inside the client SDK */
  private async refreshBeam(token: TokenResponse): Promise<void> {
    if (this.beam instanceof Beam) {
      await this.beam.refresh(token);
    }
  }
}
