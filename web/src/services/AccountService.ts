import {
  AccountPlayerView,
  ChallengeSolution,
  EmailUpdateConfirmation,
  EmailUpdateRequest,
  PasswordUpdateConfirmation,
} from '@/__generated__/schemas';
import { ApiService, type ApiServiceProps } from '@/services/types/ApiService';
import {
  accountsDeleteExternalIdentityBasic,
  accountsDeleteMeThirdPartyBasic,
  accountsGetAvailableBasic,
  accountsGetAvailableExternalIdentityBasic,
  accountsGetAvailableThirdPartyBasic,
  accountsGetMeBasic,
  accountsPostEmailUpdateConfirmBasic,
  accountsPostEmailUpdateInitBasic,
  accountsPostExternalIdentityBasic,
  accountsPostPasswordUpdateConfirmBasic,
  accountsPostPasswordUpdateInitBasic,
  accountsPostRegisterBasic,
  accountsPutMeBasic,
} from '@/__generated__/apis';
import { CredentialStatus, ThirdPartyAuthProvider } from '@/services/enums';
import { BeamError } from '@/constants/Errors';

export interface EmailCredentialParams {
  email: string;
}

export interface UserCredentialParams {
  email: string;
  password: string;
}

export interface ThirdPartyCredentialParams {
  provider: ThirdPartyAuthProvider;
  token: string;
}

export interface ExternalIdentityCredentialParams {
  externalToken: string;
  providerService: string;
  providerNamespace: string;
  challengeHandler?: (challenge: string) => string | Promise<string>;
}

export interface ExternalIdentityParams {
  providerService: string;
  providerNamespace: string;
  externalUserId: string;
}

export class AccountService extends ApiService {
  constructor(props: ApiServiceProps) {
    super(props);
  }

  /** @internal */
  get serviceName(): string {
    return 'account';
  }

  /**
   * Fetches the current player's account information.
   * @example
   * ```ts
   * // client-side:
   * const playerAccount = await beam.account.current();
   * // server-side:
   * const playerAccount = await beamServer.account(playerId).current();
   * ```
   * @throws {BeamError} If the request fails.
   */
  async current(): Promise<AccountPlayerView> {
    const { body } = await accountsGetMeBasic(this.requester, this.accountId);

    if (!this.player) return body;

    this.player.account = body;
    return body;
  }

  /**
   * Registers a new account with the given email and password, and associates it with the current player.
   * @example
   * ```ts
   * // client-side:
   * const account = beam.account;
   * // server-side:
   * const account = beamServer.account(playerId);
   * // register a new account
   * const playerAccount = await account.addCredentials({
   *   email: 'player@example.com',
   *   password: 'password123',
   * });
   * ```
   * @throws {BeamError} If the registration fails (e.g., email already exists, invalid password).
   */
  async addCredentials(
    params: UserCredentialParams,
  ): Promise<AccountPlayerView> {
    const { body } = await accountsPostRegisterBasic(
      this.requester,
      {
        email: params.email,
        password: params.password,
      },
      this.accountId,
    );

    if (!this.player) return body;

    this.player.account = body;
    return body;
  }

  /**
   * Associates a third-party account with the current player's Beamable account.
   * @example
   * ```ts
   * // client-side:
   * const account = beam.account;
   * // server-side:
   * const account = beamServer.account(playerId);
   * // add third-party credentials
   * const playerAccount = await account.addThirdParty({
   *   provider: ThirdPartyAuthProvider.Google,
   *   token: 'google-auth-token',
   * });
   * ```
   * @throws {BeamError} If the association fails (e.g., invalid token, account already linked).
   */
  async addThirdParty(
    params: ThirdPartyCredentialParams,
  ): Promise<AccountPlayerView> {
    const { body } = await accountsPutMeBasic(
      this.requester,
      {
        hasThirdPartyToken: true, // default to true since we are adding a third-party token
        thirdParty: params.provider,
        token: params.token,
      },
      this.accountId,
    );

    if (!this.player) return body;

    this.player.account = body;
    return body;
  }

  /**
   * Links an external identity to the current player's Beamable account.
   * @example
   * ```ts
   * // client-side:
   * const account = beam.account;
   * // server-side:
   * const account = beamServer.account(playerId);
   * // link an external identity
   * const playerAccount = await account.addExternalIdentity({
   *   externalToken: 'acme-auth-token',
   *   providerService: acmeServiceClient.serviceName,
   *   providerNamespace: acmeServiceClient.federationIds.acme,
   *   // optional
   *   challengeHandler: (challenge) => {
   *     // Handle the challenge, e.g., by displaying a CAPTCHA or OTP to the user
   *     return prompt(challenge);
   *   },
   * });
   * ```
   * @throws {BeamError} If the linking fails (e.g., invalid token, account already linked).
   */
  async addExternalIdentity(
    params: ExternalIdentityCredentialParams,
  ): Promise<AccountPlayerView> {
    const attachExternalIdentity = async (
      accountId: string,
      externalToken: string,
      providerService: string,
      providerNamespace?: string,
      challengeSolution?: ChallengeSolution,
    ) => {
      const { body } = await accountsPostExternalIdentityBasic(
        this.requester,
        {
          external_token: externalToken,
          provider_service: providerService,
          provider_namespace: providerNamespace,
          challenge_solution: challengeSolution,
        },
        accountId,
      );

      switch (body.result) {
        case 'challenge': {
          const { challenge_token } = body;

          if (!params.challengeHandler)
            throw new BeamError(
              'A challenge was requested but no challenge handler provided',
            );

          if (!challenge_token)
            throw new BeamError('No challenge token returned');

          const solution = await params.challengeHandler(challenge_token);
          return await attachExternalIdentity(
            this.accountId,
            params.externalToken,
            params.providerService,
            params.providerNamespace,
            { challenge_token, solution },
          );
        }
        case 'ok':
        default:
          return await this.current();
      }
    };

    return await attachExternalIdentity(
      this.accountId,
      params.externalToken,
      params.providerService,
      params.providerNamespace,
    );
  }

  /**
   * Removes a third-party account association from the current player's Beamable account.
   * @example
   * ```ts
   * // client-side:
   * const account = beam.account;
   * // server-side:
   * const account = beamServer.account(playerId);
   * // remove third-party credentials
   * const playerAccount = await account.removeThirdParty({
   *   provider: ThirdPartyAuthProvider.Google,
   *   token: 'google-auth-token',
   * });
   * ```
   * @throws {BeamError} If the disassociation fails (e.g., invalid token, account not linked).
   */
  async removeThirdParty(
    params: ThirdPartyCredentialParams,
  ): Promise<AccountPlayerView> {
    const { body } = await accountsDeleteMeThirdPartyBasic(
      this.requester,
      {
        thirdParty: params.provider,
        token: params.token,
      },
      this.accountId,
    );

    if (!this.player) return body;

    this.player.account = body;
    return body;
  }

  /**
   * Removes an external identity association from the current player's Beamable account.
   * @example
   * ```ts
   * // client-side:
   * const account = beam.account;
   * // server-side:
   * const account = beamServer.account(playerId);
   * // remove an external identity
   * const playerAccount = await account.removeExternalIdentity({
   *   providerService: acmeServiceClient.serviceName,
   *   providerNamespace: acmeServiceClient.federationIds.acme,
   *   externalUserId: 'acme-user-id',
   * });
   * ```
   * @throws {BeamError} If the disassociation fails (e.g., invalid token, account not linked).
   */
  async removeExternalIdentity(
    params: ExternalIdentityParams,
  ): Promise<AccountPlayerView> {
    await accountsDeleteExternalIdentityBasic(
      this.requester,
      {
        provider_namespace: params.providerNamespace,
        provider_service: params.providerService,
        user_id: params.externalUserId,
      },
      this.accountId,
    );

    return await this.current();
  }

  /**
   * Checks the status of an email credential.
   * @example
   * ```ts
   * // client-side:
   * const account = beam.account;
   * // server-side:
   * const account = beamServer.account(playerId);
   * // check the status of an email credential
   * const status = await account.getEmailCredentialStatus({
   *   email: 'player@example.com',
   * });
   * if (status === CredentialStatus.NotAssigned) {
   *   console.log('Email credential not assigned.');
   * }
   * ```
   */
  async getEmailCredentialStatus(
    params: EmailCredentialParams,
  ): Promise<CredentialStatus> {
    try {
      const { body } = await accountsGetAvailableBasic(
        this.requester,
        params.email,
        this.accountId,
      );
      return body.available
        ? CredentialStatus.NotAssigned
        : CredentialStatus.Assigned;
    } catch {
      return CredentialStatus.Unknown;
    }
  }

  /**
   * Checks the status of a third-party credential.
   * @example
   * ```ts
   * // client-side:
   * const account = beam.account;
   * // server-side:
   * const account = beamServer.account(playerId);
   * // check the status of a third-party credential
   * const status = await account.getThirdPartyStatus({
   *   provider: ThirdPartyAuthProvider.Google,
   *   token: 'google-auth-token',
   * });
   * if (status === CredentialStatus.NotAssigned) {
   *   console.log('Google credential not assigned.');
   * }
   * ```
   */
  async getThirdPartyStatus(
    params: ThirdPartyCredentialParams,
  ): Promise<CredentialStatus> {
    try {
      const { body } = await accountsGetAvailableThirdPartyBasic(
        this.requester,
        params.provider,
        params.token,
        this.accountId,
      );
      return body.available
        ? CredentialStatus.NotAssigned
        : CredentialStatus.Assigned;
    } catch {
      return CredentialStatus.Unknown;
    }
  }

  /**
   * Checks the status of an external identity credential.
   * @example
   * ```ts
   * // client-side:
   * const account = beam.account;
   * // server-side:
   * const account = beamServer.account(playerId);
   * // check the status of an external identity credential
   * const status = await account.getExternalIdentityStatus({
   *   providerService: acmeServiceClient.serviceName,
   *   providerNamespace: acmeServiceClient.federationIds.acme,
   *   externalUserId: 'acme-user-id',
   * });
   * if (status === CredentialStatus.NotAssigned) {
   *   console.log('External identity credential not assigned.');
   * }
   * ```
   */
  async getExternalIdentityStatus(
    params: ExternalIdentityParams,
  ): Promise<CredentialStatus> {
    try {
      const { body } = await accountsGetAvailableExternalIdentityBasic(
        this.requester,
        params.providerService,
        params.externalUserId,
        params.providerNamespace,
        this.accountId,
      );
      return body.available
        ? CredentialStatus.NotAssigned
        : CredentialStatus.Assigned;
    } catch {
      return CredentialStatus.Unknown;
    }
  }

  /**
   * Initiates an email update process for the current player's account.
   * @example
   * ```ts
   * // client-side:
   * const account = beam.account;
   * // server-side:
   * const account = beamServer.account(playerId);
   * // initiate email update
   * await account.initiateEmailUpdate({
   *   email: 'new-email@example.com',
   * });
   * ```
   * @throws {BeamError} If the initiation fails (e.g., email already in use, invalid email).
   */
  async initiateEmailUpdate(params: EmailUpdateRequest): Promise<void> {
    await accountsPostEmailUpdateInitBasic(
      this.requester,
      { newEmail: params.newEmail },
      this.accountId,
    );
  }

  /**
   * Confirms an email update process for the current player's account using a provided confirmation code and password.
   * @example
   * ```ts
   * // client-side:
   * const account = beam.account;
   * // server-side:
   * const account = beamServer.account(playerId);
   * // confirm email update
   * await account.confirmEmailUpdate({
   *   code: 'confirmation-code',
   *   password: 'current-password',
   * });
   * ```
   * @throws {BeamError} If the confirmation fails (e.g., invalid code, incorrect password).
   */
  async confirmEmailUpdate(params: EmailUpdateConfirmation): Promise<void> {
    await accountsPostEmailUpdateConfirmBasic(
      this.requester,
      params,
      this.accountId,
    );
  }

  /**
   * Initiates a password update process for the current player's account.
   * @example
   * ```ts
   * // client-side:
   * const account = beam.account;
   * // server-side:
   * const account = beamServer.account(playerId);
   * // initiate password update
   * await account.initiatePasswordUpdate({
   *   email: 'player@example.com',
   * });
   * ```
   * @throws {BeamError} If the initiation fails (e.g., invalid email).
   */
  async initiatePasswordUpdate(params: EmailCredentialParams): Promise<void> {
    await accountsPostPasswordUpdateInitBasic(
      this.requester,
      {
        email: params.email,
        codeType: 'PIN', // default to PIN
      },
      this.accountId,
    );
  }

  /**
   * Confirms a password update process for the current player's account using a provided confirmation code and new password.
   * @example
   * ```ts
   * // client-side:
   * const account = beam.account;
   * // server-side:
   * const account = beamServer.account(playerId);
   * // confirm password update
   * await account.confirmPasswordUpdate({
   *   code: 'confirmation-code',
   *   newPassword: 'new-password',
   * });
   * ```
   * @throws {BeamError} If the confirmation fails (e.g., invalid code, invalid new password).
   */
  async confirmPasswordUpdate(
    params: PasswordUpdateConfirmation,
  ): Promise<void> {
    await accountsPostPasswordUpdateConfirmBasic(
      this.requester,
      params,
      this.accountId,
    );
  }
}
