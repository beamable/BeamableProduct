import { AccountPlayerView } from '@/__generated__/schemas';
import { ApiService, type ApiServiceProps } from '@/services/types/ApiService';

export class AccountService extends ApiService {
  /** @internal */
  constructor(props: ApiServiceProps) {
    super(props);
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
    const { body } = await this.api.accounts.getAccountsMe(
      this.accountId === '0' ? undefined : this.accountId,
    );
    return body;
  }
}
