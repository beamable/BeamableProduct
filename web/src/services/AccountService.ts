import { BeamApi } from '@/core/BeamApi';
import { AccountPlayerView } from '@/__generated__/schemas';

interface AccountServiceProps {
  api: BeamApi;
}

export class AccountService {
  private readonly api: BeamApi;

  /** @internal */
  constructor(props: AccountServiceProps) {
    this.api = props.api;
  }

  /**
   * Fetches the current player's account information.
   * @example
   * ```ts
   * const playerAccount = await beam.account.current();
   * ```
   * @throws {BeamError} If the request fails.
   */
  async current(): Promise<AccountPlayerView> {
    const { body } = await this.api.accounts.getAccountsMe();
    return body;
  }
}
