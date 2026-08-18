/**
 * A data-driven catalog of Beamable Web SDK features, exercised by the SDK
 * Explorer screen (app/sdk.tsx). Each action invokes a real SDK method on the
 * live `beam` instance and returns its result for display.
 *
 * Grouped by the 7 high-level services hanging off `beam.*`:
 *   auth · player · account · stats · content · announcements · leaderboards
 *
 * Notes:
 *  - "read" actions are safe to run on a fresh guest.
 *  - "write" actions mutate your account/realm (clearly labelled).
 *  - some actions need realm-specific input (a leaderboard id, a content id)
 *    or credentials — fill those in the Inputs panel on the screen.
 */
import { ThirdPartyAuthProvider } from '@beamable/sdk';
import type { Beam } from '@beamable/sdk';
import {
  socialGetMyBasic,
  inventoryGetItemsBasic,
  inventoryGetCurrencyBasic,
  commerceGetCatalogBasic,
  mailGetByObjectId,
  presencePostQuery,
  cloudsavingGetBasic,
  eventsGetRunningBasic,
  eventsGetContentBasic,
  tournamentsGetBasic,
  tournamentsGetMeBasic,
  sessionGetClientHistoryBasic,
  lobbiesGet,
  matchmakingGetTickets,
  notificationGetBasic,
  pushPostRegisterBasic,
  trialsGetBasic,
  playersGetStatsByPlayerId,
  playersGetMatchmakingTicketsByPlayerId,
  playersGetPresenceByPlayerId,
  groupsGetByObjectId,
  partiesGetById,
  calendarsGetByObjectId,
} from '@beamable/sdk/api';

/** Low-level API functions return an HttpResponse; unwrap the body for display. */
const body = <T>(p: Promise<{ body: T }>): Promise<T> => p.then((r) => r.body);

export interface SdkInputs {
  email: string;
  password: string;
  statKey: string;
  statValue: string;
  contentId: string;
  contentType: string;
  leaderboardId: string;
  score: string;
  /** Generic object id for id-addressed low-level endpoints (group/party/calendar). */
  objectId: string;
}

export type ActionKind = 'read' | 'write';

export interface SdkAction {
  label: string;
  kind: ActionKind;
  note?: string;
  /** Returns a value to display, or throws on failure. */
  run: (beam: Beam, input: SdkInputs) => Promise<unknown>;
}

export interface SdkGroup {
  key: string;
  title: string;
  blurb: string;
  /** 'high' = convenience `beam.*` service; 'low' = raw @beamable/sdk/api call. */
  layer?: 'high' | 'low';
  actions: SdkAction[];
}

async function firstAnnouncementId(beam: Beam): Promise<string | null> {
  const list = await beam.announcements.list();
  return list[0]?.id ?? null;
}

export const SDK_GROUPS: SdkGroup[] = [
  {
    key: 'auth',
    title: 'auth · AuthService',
    blurb: 'Authentication & session/token lifecycle.',
    actions: [
      {
        label: 'loginAsGuest()',
        kind: 'read',
        note: 'Returns a fresh guest token (does not switch the active session).',
        run: (b) => b.auth.loginAsGuest(),
      },
      {
        label: 'beam.refresh()',
        kind: 'write',
        note: 'Re-initializes the session with the stored token.',
        run: (b) => b.refresh().then(() => 'session refreshed'),
      },
      {
        label: 'loginWithEmail(email, password)',
        kind: 'write',
        note: 'Needs an existing email account (see account.addCredentials first).',
        run: (b, i) => b.auth.loginWithEmail({ email: i.email, password: i.password }),
      },
    ],
  },
  {
    key: 'player',
    title: 'player · PlayerService',
    blurb: 'Cached, synchronous view of the current player.',
    actions: [
      { label: 'player.id', kind: 'read', run: async (b) => b.player.id },
      { label: 'player.account', kind: 'read', run: async (b) => b.player.account },
      {
        label: "hasThirdPartyAssociation('google')",
        kind: 'read',
        run: async (b) => b.player.hasThirdPartyAssociation(ThirdPartyAuthProvider.Google),
      },
    ],
  },
  {
    key: 'account',
    title: 'account · AccountService',
    blurb: 'Account data, credentials, and linked identities.',
    actions: [
      { label: 'current()', kind: 'read', run: (b) => b.account.current() },
      {
        label: 'getEmailCredentialStatus(email)',
        kind: 'read',
        run: (b, i) => b.account.getEmailCredentialStatus({ email: i.email }),
      },
      {
        label: "getThirdPartyStatus('google')",
        kind: 'read',
        run: (b) =>
          b.account.getThirdPartyStatus({
            provider: ThirdPartyAuthProvider.Google,
            token: '',
          }),
      },
      {
        label: 'addCredentials(email, password)',
        kind: 'write',
        note: 'Attaches an email/password login to this guest account.',
        run: (b, i) => b.account.addCredentials({ email: i.email, password: i.password }),
      },
    ],
  },
  {
    key: 'stats',
    title: 'stats · StatsService',
    blurb: 'Key/value player stats (public or private).',
    actions: [
      {
        label: 'set(private: statKey = statValue)',
        kind: 'write',
        run: (b, i) =>
          b.stats
            .set({ accessType: 'private', stats: { [i.statKey]: i.statValue } })
            .then(() => 'ok'),
      },
      {
        label: 'get(private: [statKey])',
        kind: 'read',
        run: (b, i) => b.stats.get({ accessType: 'private', stats: [i.statKey] }),
      },
      {
        label: 'get(all private)',
        kind: 'read',
        run: (b) => b.stats.get({ accessType: 'private' }),
      },
    ],
  },
  {
    key: 'content',
    title: 'content · ContentService',
    blurb: 'Live content manifest & typed content objects.',
    actions: [
      {
        label: 'getManifestEntries()',
        kind: 'read',
        note: 'Lists content ids — copy one into the contentId input.',
        run: (b) => b.content.getManifestEntries(),
      },
      {
        label: 'getById({ id: contentId })',
        kind: 'read',
        run: (b, i) => b.content.getById({ id: i.contentId }),
      },
      {
        label: 'getByType({ type: contentType })',
        kind: 'read',
        run: (b, i) => b.content.getByType({ type: i.contentType as never }),
      },
    ],
  },
  {
    key: 'announcements',
    title: 'announcements · AnnouncementsService',
    blurb: 'Player inbox: list, read, claim, delete.',
    actions: [
      { label: 'list()', kind: 'read', run: (b) => b.announcements.list() },
      { label: 'refresh()', kind: 'read', run: (b) => b.announcements.refresh() },
      {
        label: 'markAsRead(first)',
        kind: 'write',
        note: 'No-op if the inbox is empty.',
        run: async (b) => {
          const id = await firstAnnouncementId(b);
          if (!id) return 'inbox empty';
          await b.announcements.markAsRead({ id });
          return `marked ${id} as read`;
        },
      },
      {
        label: 'claim(first)',
        kind: 'write',
        run: async (b) => {
          const id = await firstAnnouncementId(b);
          if (!id) return 'inbox empty';
          await b.announcements.claim({ id });
          return `claimed ${id}`;
        },
      },
    ],
  },
  {
    key: 'leaderboards',
    title: 'leaderboards · LeaderboardsService',
    blurb: 'Rankings & scores. Set a leaderboardId from your realm first.',
    actions: [
      {
        label: 'get({ id: leaderboardId })',
        kind: 'read',
        run: (b, i) => b.leaderboards.get({ id: i.leaderboardId }),
      },
      {
        label: 'getRanks(leaderboardId, [player.id])',
        kind: 'read',
        run: (b, i) =>
          b.leaderboards.getRanks({ id: i.leaderboardId, playerIds: [b.player.id] }),
      },
      {
        label: 'getAssignedBoard({ id: leaderboardId })',
        kind: 'read',
        run: (b, i) => b.leaderboards.getAssignedBoard({ id: i.leaderboardId }),
      },
      {
        label: 'setScore(leaderboardId, score)',
        kind: 'write',
        run: (b, i) =>
          b.leaderboards
            .setScore({ id: i.leaderboardId, score: Number(i.score) })
            .then(() => 'score set'),
      },
    ],
  },

  // ───────────────────────── Low-level API (@beamable/sdk/api) ─────────────
  // Raw generated REST bindings, called with `beam.requester`. Representative
  // (mostly read-only) player-facing calls. Server/admin modules (Payments,
  // Beamo, Realms, Customer, Billing, Scheduler) are intentionally omitted —
  // they return auth errors on a client guest token.
  {
    key: 'social',
    title: 'social · SocialApi',
    blurb: 'Friends & blocked players.',
    layer: 'low',
    actions: [
      { label: 'socialGetMyBasic()', kind: 'read', run: (b) => body(socialGetMyBasic(b.requester)) },
    ],
  },
  {
    key: 'inventory',
    title: 'inventory · InventoryApi',
    blurb: 'Player items & currencies.',
    layer: 'low',
    actions: [
      { label: 'inventoryGetItemsBasic()', kind: 'read', run: (b) => body(inventoryGetItemsBasic(b.requester)) },
      { label: 'inventoryGetCurrencyBasic()', kind: 'read', run: (b) => body(inventoryGetCurrencyBasic(b.requester)) },
    ],
  },
  {
    key: 'commerce',
    title: 'commerce · CommerceApi',
    blurb: 'Store catalog, offers & purchases.',
    layer: 'low',
    actions: [
      { label: 'commerceGetCatalogBasic()', kind: 'read', run: (b) => body(commerceGetCatalogBasic(b.requester)) },
    ],
  },
  {
    key: 'mail',
    title: 'mail · MailApi',
    blurb: 'Player mailbox.',
    layer: 'low',
    actions: [
      { label: 'mailGetByObjectId(player.id)', kind: 'read', run: (b) => body(mailGetByObjectId(b.requester, b.player.id)) },
    ],
  },
  {
    key: 'presence',
    title: 'presence · PresenceApi',
    blurb: 'Online status.',
    layer: 'low',
    actions: [
      { label: 'presencePostQuery([player.id])', kind: 'read', run: (b) => body(presencePostQuery(b.requester, { playerIds: [b.player.id] })) },
    ],
  },
  {
    key: 'cloudsaving',
    title: 'cloudsaving · CloudsavingApi',
    blurb: 'Player cloud save data.',
    layer: 'low',
    actions: [
      { label: 'cloudsavingGetBasic()', kind: 'read', run: (b) => body(cloudsavingGetBasic(b.requester)) },
    ],
  },
  {
    key: 'events',
    title: 'events · EventsApi',
    blurb: 'Live events.',
    layer: 'low',
    actions: [
      { label: 'eventsGetRunningBasic()', kind: 'read', run: (b) => body(eventsGetRunningBasic(b.requester)) },
      { label: 'eventsGetContentBasic()', kind: 'read', run: (b) => body(eventsGetContentBasic(b.requester)) },
    ],
  },
  {
    key: 'tournaments',
    title: 'tournaments · TournamentsApi',
    blurb: 'Tournaments & standings.',
    layer: 'low',
    actions: [
      { label: 'tournamentsGetBasic()', kind: 'read', run: (b) => body(tournamentsGetBasic(b.requester)) },
      { label: 'tournamentsGetMeBasic()', kind: 'read', run: (b) => body(tournamentsGetMeBasic(b.requester)) },
    ],
  },
  {
    key: 'sessions',
    title: 'sessions · SessionApi',
    blurb: 'Play session history.',
    layer: 'low',
    actions: [
      { label: 'sessionGetClientHistoryBasic()', kind: 'read', run: (b) => body(sessionGetClientHistoryBasic(b.requester)) },
    ],
  },
  {
    key: 'lobby',
    title: 'lobby · LobbyApi',
    blurb: 'Multiplayer lobbies.',
    layer: 'low',
    actions: [
      { label: 'lobbiesGet()', kind: 'read', run: (b) => body(lobbiesGet(b.requester)) },
    ],
  },
  {
    key: 'matchmaking',
    title: 'matchmaking · MatchmakingApi',
    blurb: 'Matchmaking tickets.',
    layer: 'low',
    actions: [
      { label: 'matchmakingGetTickets()', kind: 'read', run: (b) => body(matchmakingGetTickets(b.requester)) },
    ],
  },
  {
    key: 'notifications',
    title: 'notifications · NotificationApi',
    blurb: 'Realtime notification channel details.',
    layer: 'low',
    actions: [
      { label: 'notificationGetBasic()', kind: 'read', run: (b) => body(notificationGetBasic(b.requester)) },
    ],
  },
  {
    key: 'push',
    title: 'push · PushApi',
    blurb: 'Native push-token registration (FCM/APNS).',
    layer: 'low',
    actions: [
      {
        label: "pushPostRegisterBasic('fcm', demo-token)",
        kind: 'write',
        note: 'Registers a placeholder token; use a real device token in production.',
        run: (b) =>
          body(pushPostRegisterBasic(b.requester, { provider: 'fcm', token: 'demo-token' })),
      },
    ],
  },
  {
    key: 'trials',
    title: 'trials · TrialsApi',
    blurb: 'A/B trials the player is enrolled in.',
    layer: 'low',
    actions: [
      { label: 'trialsGetBasic()', kind: 'read', run: (b) => body(trialsGetBasic(b.requester)) },
    ],
  },
  {
    key: 'playerStats',
    title: 'players/stats · PlayerStatsApi',
    blurb: 'Stats via the player-id REST route.',
    layer: 'low',
    actions: [
      { label: 'playersGetStatsByPlayerId(player.id)', kind: 'read', run: (b) => body(playersGetStatsByPlayerId(b.requester, b.player.id)) },
    ],
  },
  {
    key: 'playerTicket',
    title: 'players/tickets · PlayerTicketApi',
    blurb: 'Matchmaking tickets via the player-id route.',
    layer: 'low',
    actions: [
      { label: 'playersGetMatchmakingTicketsByPlayerId(player.id)', kind: 'read', run: (b) => body(playersGetMatchmakingTicketsByPlayerId(b.requester, b.player.id)) },
    ],
  },
  {
    key: 'playerPresence',
    title: 'players/presence · PlayerApi',
    blurb: 'Presence via the player-id route.',
    layer: 'low',
    actions: [
      { label: 'playersGetPresenceByPlayerId(player.id)', kind: 'read', run: (b) => body(playersGetPresenceByPlayerId(b.requester, b.player.id)) },
    ],
  },
  {
    key: 'groups',
    title: 'groups · GroupsApi',
    blurb: 'Guilds/clans. Needs a group id in the objectId input.',
    layer: 'low',
    actions: [
      { label: 'groupsGetByObjectId(objectId)', kind: 'read', run: (b, i) => body(groupsGetByObjectId(b.requester, i.objectId)) },
    ],
  },
  {
    key: 'party',
    title: 'party · PartyApi',
    blurb: 'Parties. Needs a party id in the objectId input.',
    layer: 'low',
    actions: [
      { label: 'partiesGetById(objectId)', kind: 'read', run: (b, i) => body(partiesGetById(b.requester, i.objectId)) },
    ],
  },
  {
    key: 'calendars',
    title: 'calendars · CalendarsApi',
    blurb: 'Daily-reward calendars. Needs a calendar content id in objectId.',
    layer: 'low',
    actions: [
      { label: 'calendarsGetByObjectId(objectId)', kind: 'read', run: (b, i) => body(calendarsGetByObjectId(b.requester, i.objectId)) },
    ],
  },
];

export const SDK_ACTION_COUNT = SDK_GROUPS.reduce(
  (n, g) => n + g.actions.length,
  0,
);

export const DEFAULT_INPUTS: SdkInputs = {
  email: 'rn-demo@example.com',
  password: 'Passw0rd!',
  statKey: 'rn_demo_stat',
  statValue: '42',
  contentId: '',
  contentType: '',
  leaderboardId: '',
  score: '100',
  objectId: '',
};
