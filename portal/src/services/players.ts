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
    readonly deviceIds: Array<string>;
    readonly updatedTimeMillis: number;
    readonly gamerTags: Array<GamerTag>;
    readonly thirdParties: Array<ThirdPartyAssociation>;
}

export class PlayerData {
    readonly email: string;
    readonly createdTimeMillis: number;
    readonly id: number;
    readonly deviceIds: Array<string>;
    readonly updatedTimeMillis: number;
    readonly gamerTags: Array<GamerTag>;
    readonly thirdParties: Array<ThirdPartyAssociation>;
    private defaultRealmId: string;

    constructor(
        {email, createdTimeMillis, id, deviceIds, updatedTimeMillis, gamerTags, thirdParties}: PlayerDataInterface,
        realmId: string
    ) {
        this.email = email;
        this.createdTimeMillis = createdTimeMillis;
        this.id = id;
        this.deviceIds = deviceIds ?? [];
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

    getNonExistingGamerTagForRealmMessage() {
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
        const searchReq = http.request(`/basic/accounts/search?query=${query}&page=1&pagesize=30`, void 0, 'get');  
        const deviceReq = http.request(`/basic/accounts?deviceId=${encodeURIComponent(term)}`, void 0, 'get').catch(err => {
            // don't do anything on a failure- it doesn't matter.
        }); 

        const response = await searchReq;
        let result: PlayerData[] = [];
        for(let n = 0; n < response.data.accounts.length; n++) {
            const playerData = this.convertToPlayer(response.data.accounts[n]);
            result.push(playerData);
        }

        try {
            const deviceResponse = await deviceReq;
            result.push(this.convertToPlayer(deviceResponse.data));
        } catch (err){
            // swallow the error, its totally fine.
        }
        
        return result;
    }

    @roleGuard(['admin', 'developer'])
    async findPlayer(emailOrDbid: string, swallowError:boolean = true): Promise<PlayerData> {
        try {
            this.playerError.update(_ => undefined);

            return await this.getPlayer(emailOrDbid);
        } catch (err){
            console.error(`failed to find player query=[${emailOrDbid}]`, err);

            this.playerError.update(_ => err);
            if (swallowError){
                return (undefined as unknown) as PlayerData;
            } else {
                throw err;
            }
        }
    }

    public async getPlayer(emailOrDbid: string): Promise<PlayerData> {
        const { http, router } = this.app;
        const query = encodeURIComponent(emailOrDbid);
        const response = await http.request(`/basic/accounts/find?query=${query}`, void 0, 'get');
        var playerData = this.convertToPlayer(response.data);

        if (!playerData.gamerTagForRealm()) throw {
            error: 'No gamertag',
            message: 'The player does not have a gamertag in the current realm. The player exists, but has never logged into this realm.'
        };
        return playerData;
    }

    convertToPlayer(response: any): PlayerData {
        const { router } = this.app;
        const player: PlayerDataInterface = {
            ...response,
            // we need to roll the old device id field into the new deviceIds array if it exists.
            deviceIds: (response.deviceId && response.deviceId.length
                ? [...response.deviceIds, response.deviceId]
                : [...response.deviceIds]).filter(d => d)
        };
        const playerData = new PlayerData(player, router.getRealmId());
        return playerData;
    }

    async forgetUser(player: PlayerData): Promise<PlayerData> {
        const { http, router } = this.app;

        const url = `/object/accounts/${player.id}/admin/forget`;
        const response = await http.request(url, null, 'delete');

        return new PlayerData(response.data as PlayerDataInterface, router.getRealmId());
    }

    async getPersonallyIdentifiableInformation(emailOrDbid: string): Promise<any> {
        const { http, router } = this.app;

        const query = encodeURIComponent(emailOrDbid);
        const response = await http.request(`/basic/accounts/get-personally-identifiable-information?query=${query}`, void 0, 'get');
        return response.data
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

    public async addDeviceId(player: PlayerData, deviceId: string): Promise<PlayerData> {
        const { http } = this.app;
        if (!deviceId || !deviceId.length) throw 'Cannot save an empty device id';
        const url = `/object/accounts/${player.id}`;
        const request = {
            deviceId: deviceId
        };
        const response = await http.request(url, request, 'put');
        return this.convertToPlayer(response.data);
    }

    public async removeDeviceId(player: PlayerData, deviceId: string): Promise<PlayerData> {
        const { http, router } = this.app;
        const url = `/object/accounts/${player.id}/me/devices`;
        const request = {
            deviceIds: [deviceId]
        };

        await http.request(url, request, 'delete'); // delete returns an empty response
        return await this.getPlayer(player.id.toString());
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
