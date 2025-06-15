import { HttpRequester } from '@/http/types/HttpRequester';
import { AccountsApi } from '@/__generated__/apis/AccountsApi';
import { AnnouncementsApi } from '@/__generated__/apis/AnnouncementsApi';
import { AuthApi } from '@/__generated__/apis/AuthApi';
import { BeamoApi } from '@/__generated__/apis/BeamoApi';
import { CalendarsApi } from '@/__generated__/apis/CalendarsApi';
import { CloudsavingApi } from '@/__generated__/apis/CloudsavingApi';
import { CommerceApi } from '@/__generated__/apis/CommerceApi';
import { ContentApi } from '@/__generated__/apis/ContentApi';
import { EventPlayersApi } from '@/__generated__/apis/EventPlayersApi';
import { EventsApi } from '@/__generated__/apis/EventsApi';
import { GroupsApi } from '@/__generated__/apis/GroupsApi';
import { GroupUsersApi } from '@/__generated__/apis/GroupUsersApi';
import { InventoryApi } from '@/__generated__/apis/InventoryApi';
import { LeaderboardsApi } from '@/__generated__/apis/LeaderboardsApi';
import { LobbyApi } from '@/__generated__/apis/LobbyApi';
import { MailApi } from '@/__generated__/apis/MailApi';
import { MailboxApi } from '@/__generated__/apis/MailboxApi';
import { MatchmakingApi } from '@/__generated__/apis/MatchmakingApi';
import { NotificationApi } from '@/__generated__/apis/NotificationApi';
import { PartyApi } from '@/__generated__/apis/PartyApi';
import { PaymentsApi } from '@/__generated__/apis/PaymentsApi';
import { PlayerApi } from '@/__generated__/apis/PlayerApi';
import { PlayerLobbyApi } from '@/__generated__/apis/PlayerLobbyApi';
import { PlayerPartyApi } from '@/__generated__/apis/PlayerPartyApi';
import { PlayerTicketApi } from '@/__generated__/apis/PlayerTicketApi';
import { PresenceApi } from '@/__generated__/apis/PresenceApi';
import { PushApi } from '@/__generated__/apis/PushApi';
import { RealmsApi } from '@/__generated__/apis/RealmsApi';
import { SchedulerApi } from '@/__generated__/apis/SchedulerApi';
import { SessionApi } from '@/__generated__/apis/SessionApi';
import { SocialApi } from '@/__generated__/apis/SocialApi';
import { StatsApi } from '@/__generated__/apis/StatsApi';
import { TournamentsApi } from '@/__generated__/apis/TournamentsApi';
import { TrialsApi } from '@/__generated__/apis/TrialsApi';

/**
 * Container for all generated API clients.
 * Used for making raw API calls.
 * Access each api client via `beam.api.<serviceName>.<method>`.
 */
export class BeamApi {
  constructor(private readonly requester: HttpRequester) {}

  get accounts(): AccountsApi {
    return new AccountsApi(this.requester);
  }

  get announcements(): AnnouncementsApi {
    return new AnnouncementsApi(this.requester);
  }

  get auth(): AuthApi {
    return new AuthApi(this.requester);
  }

  get beamo(): BeamoApi {
    return new BeamoApi(this.requester);
  }

  get calendars(): CalendarsApi {
    return new CalendarsApi(this.requester);
  }

  get cloudSaving(): CloudsavingApi {
    return new CloudsavingApi(this.requester);
  }

  get commerce(): CommerceApi {
    return new CommerceApi(this.requester);
  }

  get content(): ContentApi {
    return new ContentApi(this.requester);
  }

  get eventPlayers(): EventPlayersApi {
    return new EventPlayersApi(this.requester);
  }

  get events(): EventsApi {
    return new EventsApi(this.requester);
  }

  get groups(): GroupsApi {
    return new GroupsApi(this.requester);
  }

  get groupUsers(): GroupUsersApi {
    return new GroupUsersApi(this.requester);
  }

  get inventory(): InventoryApi {
    return new InventoryApi(this.requester);
  }

  get leaderboards(): LeaderboardsApi {
    return new LeaderboardsApi(this.requester);
  }

  get lobby(): LobbyApi {
    return new LobbyApi(this.requester);
  }

  get mail(): MailApi {
    return new MailApi(this.requester);
  }

  get mailbox(): MailboxApi {
    return new MailboxApi(this.requester);
  }

  get matchmaking(): MatchmakingApi {
    return new MatchmakingApi(this.requester);
  }

  get notification(): NotificationApi {
    return new NotificationApi(this.requester);
  }

  get party(): PartyApi {
    return new PartyApi(this.requester);
  }

  get payments(): PaymentsApi {
    return new PaymentsApi(this.requester);
  }

  get player(): PlayerApi {
    return new PlayerApi(this.requester);
  }

  get playerLobby(): PlayerLobbyApi {
    return new PlayerLobbyApi(this.requester);
  }

  get playerParty(): PlayerPartyApi {
    return new PlayerPartyApi(this.requester);
  }

  get playerTicket(): PlayerTicketApi {
    return new PlayerTicketApi(this.requester);
  }

  get presence(): PresenceApi {
    return new PresenceApi(this.requester);
  }

  get push(): PushApi {
    return new PushApi(this.requester);
  }

  get realms(): RealmsApi {
    return new RealmsApi(this.requester);
  }

  get scheduler(): SchedulerApi {
    return new SchedulerApi(this.requester);
  }

  get session(): SessionApi {
    return new SessionApi(this.requester);
  }

  get social(): SocialApi {
    return new SocialApi(this.requester);
  }

  get stats(): StatsApi {
    return new StatsApi(this.requester);
  }

  get tournaments(): TournamentsApi {
    return new TournamentsApi(this.requester);
  }

  get trials(): TrialsApi {
    return new TrialsApi(this.requester);
  }
}
