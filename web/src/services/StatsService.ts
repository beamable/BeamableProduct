import { BeamApi } from '@/core/BeamApi';
import { PlayerService } from '@/services/PlayerService';

interface StatsServiceProps {
  api: BeamApi;
  player: PlayerService;
}

export interface SetStatsParams {
  domainType?: 'client' | 'game';
  accessType: 'public' | 'private';
  stats: Record<string, string>;
}

export interface GetStatsParams {
  domainType?: 'client' | 'game';
  accessType: 'public' | 'private';
  stats?: string[];
}

export class StatsService {
  private readonly api: BeamApi;
  private readonly player: PlayerService;

  /** @internal */
  constructor(props: StatsServiceProps) {
    this.api = props.api;
    this.player = props.player;
  }

  /**
   * Sets a stats for the current player.
   * @example
   * ```ts
   * await beam.stats.set({
   *   accessType: 'private', // or 'public'
   *   stats: {
   *     CURRENT_LEVEL: '10',
   *     SCORE: '1000',
   *   },
   * });
   * ```
   */
  async set(params: SetStatsParams): Promise<void> {
    const { domainType = 'client', accessType, stats } = params;
    const objectId = `${domainType}.${accessType}.player.${this.player.id}`;
    await this.api.stats.postStatClientByObjectId(objectId, {
      set: { ...stats },
    });
    this.player.stats = {
      ...this.player.stats,
      ...stats,
    };
  }

  /**
   * Fetches stats for the current player.
   * @example
   * ```ts
   * // fetch specific private or public stats
   * const stats = await beam.stats.get({
   *   accessType: 'private', // or 'public'
   *   stats: ['CURRENT_LEVEL', 'SCORE'],
   * });
   * // or fetch all private or public stats
   * const allStats = await beam.stats.get({
   *   accessType: 'private', // or 'public'
   * });
   * ```
   */
  async get(params: GetStatsParams): Promise<Record<string, string>> {
    const { domainType = 'client', accessType, stats: keys = [] } = params;
    const objectId = `${domainType}.${accessType}.player.${this.player.id}`;
    const stats = keys.length > 0 ? keys.join(',') : undefined;
    const { body } = await this.api.stats.getStatClientByObjectId(
      objectId,
      stats,
    );
    this.player.stats = {
      ...this.player.stats,
      ...body.stats,
    };
    return body.stats;
  }
}
