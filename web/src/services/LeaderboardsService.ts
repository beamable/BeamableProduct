import { ApiService, type ApiServiceProps } from '@/services/types/ApiService';
import { BeamError } from '@/constants/Errors';
import type {
  LeaderboardAssignmentInfo,
  LeaderBoardView,
} from '@/__generated__/schemas';
import {
  leaderboardsGetAssignmentBasic,
  leaderboardsGetFriendsByObjectId,
  leaderboardsGetRanksByObjectId,
  leaderboardsGetViewByObjectId,
  leaderboardsPutEntryByObjectId,
  leaderboardsPutFreezeByObjectId,
} from '@/__generated__/apis';

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
  /** Whether to include friends in the leaderboard. */
  includeFriends?: boolean;
  /** Whether to include group members in the leaderboard. */
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
  constructor(props: ApiServiceProps) {
    super(props);
  }

  /** @internal */
  get serviceName(): string {
    return 'leaderboards';
  }

  /**
   * Fetches a leaderboard view for the current player.
   * @example
   * ```ts
   * // client-side:
   * const leaderboards = beam.leaderboards;
   * // server-side:
   * const leaderboards = beamServer.leaderboards(playerId);
   * // fetch a leaderboard view
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
    const { id, from, max, focus, outlier, includeFriends, includeGuilds } =
      params;
    const { body } = await leaderboardsGetViewByObjectId(
      this.requester,
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

    const lbId = this.splitByLastPart(id);
    this.player.leaderboards = { ...this.player.leaderboards, [lbId]: body.lb };
    this.player.leaderboardsParams = {
      ...this.player.leaderboardsParams,
      [lbId]: params,
    };
    return body.lb;
  }

  /**
   * Fetches the partitioned or cohorted leaderboard view for the current player.
   * @remarks This method is used to fetch a leaderboard that has been assigned to the current player.
   * @example
   * ```ts
   * // client-side:
   * const leaderboards = beam.leaderboards;
   * // server-side:
   * const leaderboards = beamServer.leaderboards(playerId);
   * // fetch the assigned leaderboard view
   * const assignedBoard = await leaderboards.getAssignedBoard({ id: 'leaderboard-id' });
   * ```
   * @throws {BeamError} If the assignment does not exist or the leaderboard cannot be fetched.
   */
  async getAssignedBoard(
    params: GetLeaderboardParams,
  ): Promise<LeaderBoardView> {
    const assignment = await this.getAssignment(params.id, true);
    if (!assignment) {
      throw new BeamError(
        `Leaderboard assignment not found for ID: ${params.id}`,
      );
    }

    return await this.get({
      ...params,
      id: assignment.leaderboardId,
    });
  }

  /**
   * Fetches the ranks of friends in a leaderboard for the current player.
   * @example
   * ```ts
   * // client-side:
   * const leaderboards = beam.leaderboards;
   * // server-side:
   * const leaderboards = beamServer.leaderboards(playerId);
   * // fetch the ranks of friends in a leaderboard
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
    const { body } = await leaderboardsGetFriendsByObjectId(
      this.requester,
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
   * // fetch the ranks of specific players in a leaderboard
   * const ranks = await leaderboards.getRanks({
   *   id: 'leaderboard-id',
   *   playerIds: ['player1GamertagOrId', 'player2GamertagOrId'],
   * });
   * ```
   * @throws {BeamError} If the request fails or the leaderboard does not exist.
   */
  async getRanks(params: GetLeaderboardRanksParams): Promise<LeaderBoardView> {
    const { id, playerIds } = params;
    const { body } = await leaderboardsGetRanksByObjectId(
      this.requester,
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
   * // set the score for the current player in a leaderboard
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
    const assignment = await this.getAssignment(id, true);
    if (!assignment) {
      throw new BeamError(`Leaderboard assignment not found for ID: ${id}`);
    }

    await leaderboardsPutEntryByObjectId(
      this.requester,
      assignment.leaderboardId,
      {
        id: this.accountId,
        score,
        increment,
        stats,
      },
      this.accountId,
    );

    if (!this.player) return;

    const lbId = assignment.leaderboardId;
    await this.get(
      this.player.leaderboardsParams[this.splitByLastPart(lbId)] ?? {
        id: lbId,
      },
    );
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
    await leaderboardsPutFreezeByObjectId(this.requester, id, this.accountId);
  }

  // Returns the leaderboard assignment for the current player, using a cached assignment if available, or joining the leaderboard if not.
  private async getAssignment(
    id: string,
    joinBoard: boolean,
  ): Promise<LeaderboardAssignmentInfo> {
    if (this.player) {
      const cachedAssignment =
        this.player.leaderboardsAssignments[`${this.player.id}:${id}`];

      if (cachedAssignment) return cachedAssignment;
    }

    const { body } = await leaderboardsGetAssignmentBasic(
      this.requester,
      id,
      joinBoard,
      this.accountId,
    );

    if (!this.player) return body;

    this.player.leaderboardsAssignments = {
      ...this.player.leaderboardsAssignments,
      [`${this.player.id}:${id}`]: body,
    };
    return body;
  }

  // Splits the leaderboard ID by the last occurrence of '#' to get the main ID.
  private splitByLastPart(id: string): string {
    const i = id.lastIndexOf('#');
    if (i === -1) return id;
    return id.slice(0, i);
  }
}
