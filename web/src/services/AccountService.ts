import { BeamApi } from '@/core/BeamApi';
import { AccountPlayerView } from '@/__generated__/schemas';

export class AccountService {
  constructor(private readonly api: BeamApi) {}

  /**
   * Retrieves the current player's account information.
   * @returns {Promise<AccountPlayerView>} A promise that resolves with the player's account view.
   * @example
   * ```ts
   * const playerAccount = await beam.account.getCurrentPlayer();
   * ```
   */
  async getCurrentPlayer(): Promise<AccountPlayerView> {
    const { body } = await this.api.accounts.getAccountsMe();
    return body;
  }
}
