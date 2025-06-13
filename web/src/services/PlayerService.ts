import { AccountPlayerView } from '@/__generated__/schemas';

/** A service for managing player-related data and operations. */
export class PlayerService {
  private playerAccount: AccountPlayerView;

  constructor() {
    // initialize playerAccount with default values
    this.playerAccount = {
      deviceIds: [],
      id: '0',
      scopes: [],
      thirdPartyAppAssociations: [],
      email: '',
      external: [],
      language: '',
    };
  }

  /**
   * @internal
   * Sets the current player's account information.
   * @param {AccountPlayerView} playerAccount - The player's account information.
   */
  set account(playerAccount: AccountPlayerView) {
    this.playerAccount = playerAccount;
  }

  /**
   * Retrieves the current player's account information.
   * @returns {AccountPlayerView} The player's account information.
   */
  get account(): AccountPlayerView {
    return this.playerAccount;
  }

  /**
   * Retrieves the current player's ID.
   * @returns {string} The player's ID.
   */
  get id(): string {
    return String(this.playerAccount.id);
  }
}
