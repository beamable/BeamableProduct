import { ApiService, type ApiServiceProps } from '@/services/types/ApiService';

export interface SetStatsParams {
  /**
   * The type of domain for the stats.
   * @remarks 'client' for client-side stats, 'game' for game server stats.
   * @default 'client'
   */
  domainType?: 'client' | 'game';
  /**
   * The type of access for the stats.
   * @remarks 'public' for public stats, 'private' for private stats.
   */
  accessType: 'public' | 'private';
  /**
   * The stats to set for the current player.
   * @remarks The keys are stat names and the values are their corresponding values as strings.
   */
  stats: Record<string, string>;
  /** Whether to emit analytics for the stats change. */
  emitAnalytics?: boolean;
}

export interface GetStatsParams {
  /**
   * The type of domain for the stats.
   * @remarks 'client' for client-side stats, 'game' for game server stats.
   * @default 'client'
   */
  domainType?: 'client' | 'game';
  /**
   * The type of access for the stats.
   * @remarks 'public' for public stats, 'private' for private stats.
   */
  accessType: 'public' | 'private';
  /**
   * The specific stats to fetch.
   * @remarks If not provided, all stats for the specified domain and access will be fetched.
   */
  stats?: string[];
}

export class StatsService extends ApiService {
  /** @internal */
  constructor(props: ApiServiceProps) {
    super(props);
  }

  /**
   * Sets a stats for the current player.
   * @remarks Game domain stats can only be set by the game server.
   * @example
   * ```ts
   * const stats = {
   *  CURRENT_LEVEL: '10',
   *  SCORE: '1000',
   * };
   * // client-side: set stats for a client domain
   * await beam.stats.set({
   *   accessType: 'private', // or 'public'
   *   stats,
   * });
   * // server-side: set stats for a game domain
   * await beamServer.stats(playerId).set({
   *  domainType: 'game',
   *  accessType: 'private', // or 'public'
   *  stats,
   * });
   * ```
   */
  async set(params: SetStatsParams): Promise<void> {
    const { domainType = 'client', accessType, stats, emitAnalytics } = params;
    const objectId = `${domainType}.${accessType}.player.${this.playerIdOrThrow}`;

    // convert all stats values to string
    Object.keys(stats).forEach((key) => {
      stats[key] = String(stats[key]);
    });

    domainType === 'client'
      ? await this.api.stats.postStatClientByObjectId(
          objectId,
          { set: { ...stats }, emitAnalytics },
          this.playerIdOrThrow,
        )
      : await this.api.stats.postStatByObjectId(
          objectId,
          { set: { ...stats } },
          this.playerIdOrThrow,
        );

    if (!this.player) return;
    this.player.stats = {
      ...this.player.stats,
      ...stats,
    };
  }

  /**
   * Fetches stats for the current player.
   * @remarks Game domain stats can only be fetched by the game server.
   * @example
   * ```ts
   * // client-side: fetch specific private or public stats
   * const stats = await beam.stats.get({
   *   accessType: 'private', // or 'public'
   *   stats: ['CURRENT_LEVEL', 'SCORE'],
   * });
   * // client-side: or fetch all private or public stats
   * const allStats = await beam.stats.get({
   *   accessType: 'private', // or 'public'
   * });
   * // server-side: fetch stats for a game domain
   * const gameStats = await beamServer.stats(playerId).get({
   *  domainType: 'game',
   *  accessType: 'private', // or 'public'
   *  stats: ['CURRENT_LEVEL', 'SCORE'],
   * });
   * ```
   */
  async get(params: GetStatsParams): Promise<Record<string, string>> {
    const { domainType = 'client', accessType, stats: keys = [] } = params;
    const objectId = `${domainType}.${accessType}.player.${this.playerIdOrThrow}`;
    const stats = keys.length > 0 ? keys.join(',') : undefined;
    const { body } =
      domainType === 'client'
        ? await this.api.stats.getStatClientByObjectId(
            objectId,
            stats,
            this.playerIdOrThrow,
          )
        : await this.api.stats.getStatByObjectId(
            objectId,
            stats,
            this.playerIdOrThrow,
          );

    // convert all stats values to string
    Object.keys(body.stats).forEach((key) => {
      body.stats[key] = String(body.stats[key]);
    });

    if (this.player) {
      this.player.stats = {
        ...this.player.stats,
        ...body.stats,
      };
    }
    return body.stats;
  }
}
