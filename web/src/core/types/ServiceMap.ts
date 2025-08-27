import type {
  AccountService,
  AnnouncementsService,
  AuthService,
  ContentService,
  LeaderboardsService,
  StatsService,
} from '@/services';

/**
 * The `SERVICE_KEYS` array lists the names of services that are available in the SDK.
 * This array is used to dynamically register and manage services within the SDK.
 */
export const SERVICE_KEYS = [
  'account',
  'announcements',
  'auth',
  'content',
  'leaderboards',
  'stats',
] as const;

/**
 * The `ServiceMap` maps service keys to their corresponding service class instances.
 * This is used for type-safe access to services (e.g., `beam.account`).
 */
export type ServiceMap = {
  account: AccountService;
  announcements: AnnouncementsService;
  auth: AuthService;
  content: ContentService;
  leaderboards: LeaderboardsService;
  stats: StatsService;
};

/** The `ServiceKey` type represents a valid key for accessing services from the `ServiceMap`. */
export type ServiceKey = keyof ServiceMap;

/**
 * The `BeamServiceType` represents the type of the `clientServices` object,
 * providing typed access to all client-side services.
 */
export type BeamServiceType = { [K in ServiceKey]: ServiceMap[K] };

/**
 * The `BeamServerServiceType` represents the type of the `serverServices` object,
 * providing typed access to all server-side services.
 */
export type BeamServerServiceType = {
  [K in ServiceKey]: (userId: string) => ServiceMap[K];
};
