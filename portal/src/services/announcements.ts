import {BaseService} from './base';
import { Readable } from '../lib/stores';
import { PlayerData } from './players';

import { networkFallback } from '../lib/decorators';


interface Announcement {
    readonly symbol: string;
    readonly channel: string;
    readonly title: string;
    readonly summary: string;
    readonly body: string;
    readonly start_date: string;
    readonly end_date: string;
    readonly attachments: Array<AnnouncementAttachment>;
}

interface AnnouncementAttachment {
    readonly symbol: string;
    readonly count: number;
    readonly type: string;
}

interface PlayerAnnouncement {
    readonly isRead: boolean;
    readonly isClaimed: boolean;
    readonly isDeleted: boolean;
    readonly secondsRemaining: number;

    readonly announcement: Announcement;
}

export class AnnouncementsService extends BaseService {

    public readonly playerAnnouncements: Readable<Array<PlayerAnnouncement>> = this.derived(
        this.app.players.playerData,
        (arg:PlayerData, set:(arg: Array<PlayerAnnouncement>) => void) => {

        if (arg){
            this.fetchPlayerAnnouncements(arg).then(set);
        }
    });

    public async fetchAllAnnouncements(): Promise<Array<Announcement>> {
        const { http } = this.app;
        const response = await http.request(`basic/announcements/content`, void 0, 'get');
        const announcements = response.data.content as Array<Announcement>;
        return announcements;
    }

    @networkFallback()
    public async fetchPlayerAnnouncements(player: PlayerData): Promise<Array<PlayerAnnouncement>> {
        const { http } = this.app;
        const response = await http.request(`/object/announcements/${player.gamerTagForRealm()}?include_deleted=true`, void 0, 'get');

        const d: Array<PlayerAnnouncement> = response.data.announcements.map((announcement:any) => {
            const { isRead, isClaimed, isDeleted, secondsRemaining} = announcement;
            const result: PlayerAnnouncement = {
                isRead,
                isClaimed,
                isDeleted,
                secondsRemaining,
                announcement: {...announcement}
            }
            return result;
        });

        return d;
    }
}