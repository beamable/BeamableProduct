import type { AnnouncementView } from '@/__generated__/schemas';

export interface ContextMap {
  'announcements.refresh': AnnouncementsRefresh;
}

interface DelayParams {
  delay: number;
}

export interface AnnouncementsRefresh extends DelayParams {
  scopes: string[];
  data: AnnouncementView[];
}
