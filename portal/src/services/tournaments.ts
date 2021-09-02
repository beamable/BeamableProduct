import {BaseService} from './base';
import {Readable, Writable} from "../lib/stores";
import { PlayerData } from './players';
import { networkFallback } from 'lib/decorators';

export interface PlayerStatus {
  readonly contentId: string;
  readonly tournamentId: string;
  readonly playerId: number;
  readonly tier: number;
  readonly stage: number;
  readonly rank: number;
  readonly score: number;
  readonly lastUpdateCycle: number;
  readonly unclaimedRewards: Array<TournamentReward>;
}

export interface TournamentReward {
  readonly symbol: string;
  readonly amount: number;
}

export interface PlayerStatusUpdate {
  readonly score?: number;
  readonly tier?: number;
  readonly stage?: number;
}

export class TournamentService extends BaseService {
  readonly playerStatusRefresh: Writable<number> = this.writable('tournaments.player.refresh', 0);

  public readonly playerStatus: Readable<Array<PlayerStatus>> = this.derived(
    [this.app.players.playerData, this.playerStatusRefresh], (args: [PlayerData, number], set: (a:Array<PlayerStatus>) => void) => {
      if (args[0]){
        this.fetchPlayerStatuses(args[0]).then(set);
      }
    }
  );

  @networkFallback()
  async fetchPlayerStatuses(player: PlayerData): Promise<Array<PlayerStatus>> {
    const { http } = this.app;
    let playerId = player.gamerTagForRealm();
    let url = `basic/tournaments/admin/player?playerId=${playerId}`
    let response = await http.request(url, void 0, 'get');
    return response.data.statuses;
  }

  async updatePlayerStatus(player: PlayerStatus, update: PlayerStatusUpdate): Promise<void> {
    const { http } = this.app;
    const body = {
      playerId: player.playerId,
      tournamentId: player.tournamentId,
      update: update
    }

    const url = `basic/tournaments/admin/player`;
    await http.request(url, body, 'put');
  }
}