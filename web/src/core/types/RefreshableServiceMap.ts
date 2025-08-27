import type { AnnouncementView } from '@/__generated__/schemas';
import type { ContentManifestChecksum, RefreshableService } from '@/services';

/**
 * @internal
 * The `REFRESHABLE_SERVICES` array lists the names of services that support refresh operations.
 * This array is used to dynamically register and manage refreshable services within the SDK.
 */
export const REFRESHABLE_SERVICES = ['announcements', 'content'];

/** The `RefreshableServiceMap` defines the types for refreshable services, mapping a service key to its refresh data type. */
export interface RefreshableServiceMap {
  'announcements.refresh': AnnouncementsRefresh;
  'content.refresh': ContentRefresh;
}

/**
 * The `RefreshableRegistry` maps a service key to its `RefreshableService` instance.
 */
export type RefreshableRegistry = {
  [K in keyof RefreshableServiceMap]: RefreshableService<
    RefreshableServiceMap[K]['data']
  >;
};

interface DelayRefresh {
  delay: number;
}

export interface AnnouncementsRefresh extends DelayRefresh {
  scopes: string[];
  data: AnnouncementView[];
}

export interface ContentRefresh extends DelayRefresh {
  scopes: string[];
  data: ContentManifestChecksum;
}
