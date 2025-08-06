import type {
  AccountService,
  AnnouncementsService,
  AuthService,
  LeaderboardsService,
  StatsService,
} from '@/services';

export const SERVICE_KEYS = [
  'account',
  'announcements',
  'auth',
  'leaderboards',
  'stats',
] as const;

export type ServiceMap = {
  account: AccountService;
  announcements: AnnouncementsService;
  auth: AuthService;
  leaderboards: LeaderboardsService;
  stats: StatsService;
};

export type ServiceKey = keyof ServiceMap;

export type BeamServiceType = { [K in ServiceKey]: ServiceMap[K] };

export type BeamServerServiceType = {
  [K in ServiceKey]: (userId: string) => ServiceMap[K];
};
