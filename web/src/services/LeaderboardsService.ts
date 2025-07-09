import { ApiService, type ApiServiceProps } from '@/services/types/ApiService';
import { BeamError } from '@/constants/Errors';
import { LeaderBoardView } from '@/__generated__/schemas';

export interface GetLeaderboardParams {
  /** The ID of the leaderboard to fetch. */
  id: string;
  /** Start rank of the leaderboard view.
   * @remarks This is ignored when focus is set
   */
  from?: number;
  /**
   * Maximum number of ranks to return in the leaderboard view.
   * @remarks When `focus` is set, includes the focused player plus up to `max/2` entries before and after. If one side has fewer entries, it’s truncated.
   */
  max?: number;
  /** Player (gamertag) to center the leaderboard view on. */
  focus?: bigint | string;
  /**
   * Player (gamertag) to use as an outlier in the leaderboard view.
   * @remarks This is used to include a specific player in the leaderboard view.
   */
  outlier?: bigint | string;
  /**
   * Include friends in the leaderboard.
   * @default false
   */
  includeFriends?: boolean;
  /**
   * Include group members in the leaderboard.
   * @default false
   */
  includeGuilds?: boolean;
}

export interface GetLeaderboardFriendsParams {
  /** The ID of the leaderboard to fetch friends rank for. */
  id: string;
}

export interface GetLeaderboardRanksParams {
  /** The ID of the leaderboard to fetch ranks for. */
  id: string;
  /** The IDs (gamertag) of the players to fetch ranks for. */
  playerIds: (bigint | string)[];
}

export interface SetLeaderboardScoreParams {
  /** The ID of the leaderboard to set the score for. */
  id: string;
  /** The score to set. */
  score: number;
  /**
   * Whether to increment the existing score instead of overwriting it.
   * @default false
   * */
  increment?: boolean;
  /** Additional stats to set for the leaderboard entry. */
  stats?: Record<string, string>;
}

export interface FreezeLeaderboardParams {
  /** The ID of the leaderboard to freeze. */
  id: string;
}

export class LeaderboardsService extends ApiService {
  /** @internal */
  constructor(props: ApiServiceProps) {
    super(props);
  }

  /**
   * Fetches a leaderboard view for the current player.
   * @example
   * ```ts
   * // client-side:
   * const leaderboards = beam.leaderboards;
   * // server-side:
   * const leaderboards = beamServer.leaderboards(playerId);
   * const leaderboardView = await leaderboards.get({
   *   id: 'leaderboard-id',
   *   from: 1, // optional, to start from a specific rank
   *   max: 10, // optional, to limit the number of ranks returned
   *   focus: 'playerGamertagOrId', // optional, to center the view on a specific player
   *   outlier: 'anotherPlayerGamertagOrId', // optional, to include another player as an outlier
   *   includeFriends: true, // optional, to include friends in the view
   *   includeGuilds: true, // optional, to include group members in the view
   * });
   * ```
   * @throws {BeamError} If the request fails or the leaderboard does not exist.
   */
  async get(params: GetLeaderboardParams): Promise<LeaderBoardView> {
    const {
      id,
      from,
      max,
      focus,
      outlier,
      includeFriends = false,
      includeGuilds = false,
    } = params;
    const { body } = await this.api.leaderboards.getLeaderboardViewByObjectId(
      id,
      focus,
      includeFriends,
      from,
      includeGuilds,
      max,
      outlier,
      this.accountId,
    );

    if (!this.player) return body.lb;

    this.player.leaderboards = { ...this.player.leaderboards, [id]: body.lb };
    this.player.leaderboardsParams = {
      ...this.player.leaderboardsParams,
      [id]: params,
    };
    return body.lb;
  }

  /**
   * Fetches the ranks of friends in a leaderboard for the current player.
   * @example
   * ```ts
   * // client-side:
   * const leaderboards = beam.leaderboards;
   * // server-side:
   * const leaderboards = beamServer.leaderboards(playerId);
   * const friendRanks = await leaderboards.getFriendRanks({
   *   id: 'leaderboard-id',
   * });
   * ```
   * @throws {BeamError} If the request fails or the leaderboard does not exist.
   */
  async getFriendRanks(
    params: GetLeaderboardFriendsParams,
  ): Promise<LeaderBoardView> {
    const { id } = params;
    const { body } =
      await this.api.leaderboards.getLeaderboardFriendsByObjectId(
        id,
        this.accountId,
      );
    return body.lb;
  }

  /**
   * Fetches the ranks of specific players in a leaderboard for the current player.
   * @example
   * ```ts
   * // client-side:
   * const leaderboards = beam.leaderboards;
   * // server-side:
   * const leaderboards = beamServer.leaderboards(playerId);
   * const ranks = await leaderboards.getRanks({
   *   id: 'leaderboard-id',
   *   playerIds: ['player1GamertagOrId', 'player2GamertagOrId'],
   * });
   * ```
   * @throws {BeamError} If the request fails or the leaderboard does not exist.
   */
  async getRanks(params: GetLeaderboardRanksParams): Promise<LeaderBoardView> {
    const { id, playerIds } = params;
    const { body } = await this.api.leaderboards.getLeaderboardRanksByObjectId(
      id,
      playerIds.join(','),
      this.accountId,
    );
    return body.lb;
  }

  /**
   * Sets the score for the current player in a leaderboard.
   * @example
   * ```ts
   * // client-side:
   * const leaderboards = beam.leaderboards;
   * // server-side:
   * const leaderboards = beamServer.leaderboards(playerId);
   * await leaderboards.setScore({
   *   id: 'leaderboard-id',
   *   score: 1000,
   *   increment: true, // optional, to increment the existing score
   *   stats: { key: 'value' }, // optional, additional stats to set
   * });
   * ```
   * @throws {BeamError} If the request fails or the leaderboard does not exist.
   */
  async setScore(params: SetLeaderboardScoreParams): Promise<void> {
    const { id, score, increment = false, stats } = params;
    await this.api.leaderboards.putLeaderboardEntryByObjectId(
      id,
      {
        id: this.accountId,
        score,
        increment,
        stats,
      },
      this.accountId,
    );

    if (!this.player) return;

    await this.get(this.player.leaderboardsParams[id]);
  }

  /**
   * Freezes a leaderboard, preventing further score updates.
   * @remarks This method can only be called by a game server using an admin account.
   * @example
   * ```ts
   * await beamServer.leaderboards(adminId).freeze({ id: 'leaderboard-id' });
   * ```
   * @throws {BeamError} If the request fails, the leaderboard does not exist,
   *   or this method is called on the client side.
   */
  async freeze(params: FreezeLeaderboardParams): Promise<void> {
    if (this.player) {
      throw new BeamError(
        'The freeze method is server-only and cannot be called from the client.',
      );
    }

    const { id } = params;
    await this.api.leaderboards.putLeaderboardFreezeByObjectId(
      id,
      this.accountId,
    );
  }
}
