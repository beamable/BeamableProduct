import type { AnnouncementView } from '@/__generated__/schemas';

export const REFRESHABLE_SERVICES = ['announcements'];

export interface RefreshableServiceMap {
  'announcements.refresh': AnnouncementsRefresh;
}

interface DelayParams {
  delay: number;
}

export interface AnnouncementsRefresh extends DelayParams {
  scopes: string[];
  data: AnnouncementView[];
}
