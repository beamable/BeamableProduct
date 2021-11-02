import {BaseService} from './base';
import { Readable, Writable, get } from '../lib/stores';
import { roleGuard, networkFallback } from '../lib/decorators';

export interface StatCollection {
    readonly game: StatGroup;
    readonly client: StatGroup;
}

export interface StatGroup {
    readonly public: StatsMap;
    readonly private: StatsMap;
}

export interface StatsMap {
    [key: string]: any
}

export interface Stat {
    readonly name: string;
    readonly origin: string;
    readonly visibility: string;
    readonly value: string;
}

export interface ThirdPartyAssociation {
    readonly appId: string;
    readonly name: string;
    readonly userAppId: string;
    readonly userBusinessId: string;
    readonly meta: any;
}

export interface GamerTag {
    readonly gamerTag: number;
    readonly projectId: string;
}

export interface PlayerDataInterface {
    readonly email: string;
    readonly createdTimeMillis: number;
    readonly id: number;
    readonly deviceId: string;
    readonly updatedTimeMillis: number;
    readonly gamerTags: Array<GamerTag>;
    readonly thirdParties: Array<ThirdPartyAssociation>;
}

export class PlayerData {
    readonly email: string;
    readonly createdTimeMillis: number;
    readonly id: number;
    readonly deviceId: string;
    readonly updatedTimeMillis: number;
    readonly gamerTags: Array<GamerTag>;
    readonly thirdParties: Array<ThirdPartyAssociation>;
    private defaultRealmId: string;

    constructor(
        {email, createdTimeMillis, id, deviceId, updatedTimeMillis, gamerTags, thirdParties}: PlayerDataInterface,
        realmId: string
    ) {
        this.email = email;
        this.createdTimeMillis = createdTimeMillis;
        this.id = id;
        this.deviceId = deviceId ?? '';
        this.updatedTimeMillis = updatedTimeMillis;
        this.gamerTags = gamerTags;
        this.thirdParties = thirdParties;
        this.defaultRealmId = realmId;
    }

    gamerTagForRealm(realmId: string | undefined = undefined): number | undefined {
        if (realmId === undefined) {
            realmId = this.defaultRealmId;
        }

        return this.gamerTags.find((gamerTag: any) => gamerTag.projectId === realmId)?.gamerTag;
    }

    gamerTagForRealmMessage() {
        if (!this.gamerTagForRealm()) {
            return 'The player exists, but has never logged into this realm and has no gamertag. You can not select this account.'
        }
        return null
    }
}


export class PlayersService extends BaseService {
    public readonly emailOrDbid: Writable<string> = this.writable('players.emailOrDbid');
    public readonly playerError: Writable<string> = this.writable('players.playerError');
    public playerData: Writable<PlayerData> = this.writable('players.playerData')
    
    public readonly playerDataSearch: Readable<PlayerData[]> = this.derived(
        [this.app.router.realmId, this.emailOrDbid],
        (args: [string, string], set:(next: PlayerData[] | undefined) => void) => {
        const [realmId, emailOrDbid] = args;

        if (realmId && emailOrDbid){
            this.searchPlayers(emailOrDbid).then(set);
        } else {
            set(undefined);
        }
    });

    public readonly playerStats: Readable<Array<Stat>> = this.derived(this.playerData, (arg: PlayerData, set: any) => arg ? this.getAllStats(arg).then(set) : undefined )

    init() {
        this.app.router.typedUrlParameters.subscribe(value => {
            if (!value) return;
            this.emailOrDbid.set(value.playerQuery);
        });

        this.emailOrDbid.subscribe(value => {
            this.app.router.writeUrlParams(params => ({
                ...params,
                playerQuery: value
            }));
        });
    }

    @roleGuard(['admin', 'developer'])
    public async searchPlayers(term: string): Promise<PlayerData[]> {
        const { http, router } = this.app;
        const query = encodeURIComponent(term);
        const response = await http.request(`/basic/accounts/search?query=${query}&page=1&pagesize=30`, void 0, 'get');   
        let result: PlayerData[] = [];
        for(let n = 0; n < response.data.accounts.length; n++) {
            let playerData = response.data.accounts[n] as PlayerDataInterface;
            result.push(new PlayerData(playerData, router.getRealmId()));
        }
        return result;
    }


    async forgetUser(player: PlayerData): Promise<PlayerData> {
        const { http, router } = this.app;

        const url = `/object/accounts/${player.id}/admin/forget`;
        const response = await http.request(url, null, 'delete');

        return new PlayerData(response.data as PlayerDataInterface, router.getRealmId());
    }

    async updateEmail(player: PlayerData, newEmail: string): Promise<PlayerData> {
        const { http, router } = this.app;

        const request = {
            newEmail,
        };
        const url = `/object/accounts/${player.id}/admin/email`;
        const response = await http.request(url, request, 'put');

        return new PlayerData(response.data as PlayerDataInterface, router.getRealmId());
    }

    async updateDeviceId(player: PlayerData, newDeviceId: string): Promise<PlayerData> {
        const { http, router } = this.app;

        const request = newDeviceId && newDeviceId.length > 0 
            ? { deviceId: newDeviceId }
            : null;
        
        const url = `/object/accounts/${player.id}`;
        const response = await http.request(url, request, 'put');

        return new PlayerData(response.data as PlayerDataInterface, router.getRealmId());
    }

    async removeThirdParty(player: PlayerData, thirdParty: ThirdPartyAssociation): Promise<PlayerData> {
        const { http, router } = this.app;

        const request = {
            thirdParty: thirdParty.name,
            userAppId: thirdParty.userAppId
        };
        const url = `/object/accounts/${player.id}/admin/third-party`;
        const response = await http.request(url, request, 'delete');
        if (response.data.result === 'ok'){
            const nextThirdParties = player.thirdParties.filter( (tp: ThirdPartyAssociation) => tp.name !== thirdParty.name)
            var data = {
                ...player,
                thirdParties: nextThirdParties
            } as PlayerDataInterface

            return new PlayerData(data, router.getRealmId());
        } else {
            console.error(response);
            throw `third party removal response was not okay. response=[${response.data}] response.result=[${response.data.result}]`
        }
    }

    async getStats(player: PlayerData, origin: string, visibility: string): Promise<StatsMap>
    {
        const { http } = this.app;
        const gamerTag = player.gamerTagForRealm();
        const response = await http.request(`/object/stats/${origin}.${visibility}.player.${gamerTag}`, void 0, 'get');
        const statGroup = response.data as StatsMap;
        return statGroup;
    }

    async updateStats(player: PlayerData, stat: Stat) : Promise<Stat> {
        const { http } = this.app;

        let statKey: StatsMap = {};
        statKey[stat.name] = stat.value;

        const requestBody = {
            updates: [{
                objectId: `${stat.origin}.${stat.visibility}.player.${player.gamerTagForRealm()}`,
                set: statKey
            }]
        }

        await http.request(`/basic/stats/batch`, requestBody, 'post')
        return stat;
    }

    async getStatsArray(player: PlayerData, origin: string, visibility: string): Promise<Array<Stat>> {
        const map = await this.getStats(player, origin, visibility);

        const transformer: (a: any) => Stat = r => ({
            name: r[0],
            value: r[1],
            visibility,
            origin
        });

        const stats = Object.entries(map.stats).map(transformer);
        return stats;
    }

    @networkFallback()
    async getAllStats(player: PlayerData): Promise<Array<Stat>> {
        const publicVisibility = 'public';
        const privateVisibility = 'private';
        const gameOrigin = 'game';
        const clientOrigin = 'client';

        const results = await Promise.all([
            this.getStatsArray(player, gameOrigin, publicVisibility),
            this.getStatsArray(player, gameOrigin, privateVisibility),
            this.getStatsArray(player, clientOrigin, publicVisibility),
            this.getStatsArray(player, clientOrigin, privateVisibility)
        ])
        return results.flat();
    }
}

export default PlayersService;
