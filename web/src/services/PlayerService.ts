import { AccountPlayerView, AnnouncementView } from '@/__generated__/schemas';

/** A service for managing player-related data and operations. */
export class PlayerService {
  private playerAccount: AccountPlayerView;
  private playerAnnouncements: AnnouncementView[] = [];
  private playerStats: Record<string, string> = {};

  /** @internal */
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
   */
  set account(playerAccount: AccountPlayerView) {
    this.playerAccount = playerAccount;
  }

  /** Retrieves the current player's account information. */
  get account(): AccountPlayerView {
    return this.playerAccount;
  }

  /** Retrieves the current player's ID. */
  get id(): string {
    return String(this.playerAccount.id);
  }

  /**
   * @internal
   * Sets the current player's announcements.
   */
  set announcements(playerAnnouncements: AnnouncementView[]) {
    this.playerAnnouncements = playerAnnouncements;
  }

  /** Retrieves the current player's announcements. */
  get announcements(): AnnouncementView[] {
    return this.playerAnnouncements;
  }

  /**
   * @internal
   * Sets the current player's stats.
   */
  set stats(playerStats: Record<string, string>) {
    this.playerStats = playerStats;
  }

  /** Retrieves the current player's stats. */
  get stats(): Record<string, string> {
    return this.playerStats;
  }
}
