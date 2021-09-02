import {BaseService} from './base';
import { Readable, Writable, get } from '../lib/stores';
import { PlayerData } from './players';

import { roleGuard, networkFallback } from '../lib/decorators';


export interface LeaderboardList {
    readonly nameList: Array<string>;
    readonly offset: number;
    readonly total: number;
}

interface LeaderboardDescriptor {
    readonly id: string;
    readonly name: string;
    readonly totalEntries: number;
}

interface LeaderboardPage {
    readonly fullName: string;
    readonly lbid: string;
    readonly numberOfEntries: number;
    readonly view: LeaderboardView;
}

interface LeaderboardView {
    readonly boardSize: number;
    readonly lbid: string;
    readonly radnkings: Array<LeaderboardRank>
}

export interface LeaderboardRank {
    readonly rank: number;
    readonly score: number;
    readonly gt: number;
    readonly stats: Array<LeaderboardStat>
}

export interface PlayerLeaderboard {
    readonly id: string;
    readonly boardSize: number;
    readonly rank: LeaderboardRank;
}

export interface LeaderboardStat {
    readonly name: string;
    readonly value: any;
}

interface LeaderboardResponse {
    readonly result: string;
    readonly data: any;
}

export class LeaderboardService extends BaseService {

    public readonly currentLeaderboard: Writable<LeaderboardDescriptor> = this.writable('leaderboards.current')

    public readonly leaderboardList: Readable<LeaderboardList> = this.derived(this.app.realms.realm, (arg: any, set: (a:LeaderboardList)=>void) => {
        if (arg){
            this.fetchLeaderboardList().then(set);
        }
    });

    readonly playerLeaderboardRefresh: Writable<number> = this.writable('leaderboards.player.refresh', 0);

    public readonly playerLeaderboards: Readable<Array<PlayerLeaderboard>> = this.derived(
        [this.app.players.playerData, this.playerLeaderboardRefresh], (args: [PlayerData, number], set: (a:Array<PlayerLeaderboard>) => void) => {
        if (args[0]){
            this.fetchPlayerRanks(args[0]).then(set);
        }
    });

    @roleGuard(['admin', 'developer'])
    @networkFallback()
    async fetchLeaderboardList(): Promise<LeaderboardList> {
        const { http } = this.app;

        const response = await http.request(`basic/leaderboards/list`, void 0, 'get');
        const leaderboards = response.data as LeaderboardList;
        return leaderboards;
    }

    @networkFallback()
    async fetchPlayerRanks(player: PlayerData): Promise<Array<PlayerLeaderboard>> {
        const { http } = this.app;
        const dbid = player.gamerTagForRealm();
        const response = await http.request(`basic/leaderboards/player?dbid=${dbid}`, void 0, 'get');
        return response.data.lbs.map((lb:any) => {
            const playerLeaderboard: PlayerLeaderboard = {
                id: lb.lbId,
                boardSize: lb.boardSize,
                rank: lb.rankings[0]
            }
            return playerLeaderboard;
        });
    }

    public async fetchLeaderboardPage(leaderboardName: string, from: number, max: number): Promise<LeaderboardPage> {
        const { http } = this.app;
        const response = await http.request(`object/leaderboards/${leaderboardName}/details?from=${from}&max=${max}`, void 0, 'get');
        const page = response.data as LeaderboardPage;
        return page;
    }

    public async clearPlayer(dbid: number, leaderboard: LeaderboardDescriptor): Promise<LeaderboardResponse> {
        const response = await this.clearPlayerApi(dbid, leaderboard);
        if (dbid === get(this.app.players.playerData).gamerTagForRealm()){
            this.playerLeaderboardRefresh.update(n => n ? n + 1 : 1);
        }
        return response;
    }

    public async clearLeaderboard(leaderboard: LeaderboardDescriptor): Promise<LeaderboardResponse> {
        const { http } = this.app;
        const response = await http.request(`object/leaderboards/${leaderboard.id}/entries`, void 0, 'delete');
        return response.data as LeaderboardResponse;

    }

    public async editPlayerScore(lbId: string, player: PlayerData, score: Number) : Promise<LeaderboardResponse> {
        const { http } = this.app;
        const request = {
            id: player.gamerTagForRealm(),
            score: score
        }
        const response = await http.request(`object/leaderboards/${lbId}/entry`, request, 'put');
        if (player === get(this.app.players.playerData)){
           this.playerLeaderboardRefresh.update(n => n ? n + 1 : 1);
        }
        return response.data as LeaderboardResponse;
    }

    public async deleteLeaderboard(leaderboard: LeaderboardDescriptor): Promise<LeaderboardResponse> {
        const response = await this.deleteLeadboardApi(leaderboard);
        const current = get(this.currentLeaderboard);
        if (current && current.id == leaderboard.id){
            // uh oh, we just deleted the board we were looking at. Time to clear it.
            this.currentLeaderboard.set(undefined);
        }
        this.app.realms.org.update(o => o); // trigger a reflow of realm data which causes the leaderboards list store to reset.
        return response;
    }

    async clearPlayerApi(dbid: number, leaderboard: LeaderboardDescriptor): Promise<LeaderboardResponse> {
        const { http } = this.app;
        const response = await http.request(`object/leaderboards/${leaderboard.id}/entry?id=${dbid}`, void 0, 'delete');
        return response.data as LeaderboardResponse;
    }

    async deleteLeadboardApi(leaderboard: LeaderboardDescriptor): Promise<LeaderboardResponse> {
        const { http } = this.app;
        const response = await http.request(`object/leaderboards/${leaderboard.id}`, void 0, 'delete');
        return response.data as LeaderboardResponse;
    }

}