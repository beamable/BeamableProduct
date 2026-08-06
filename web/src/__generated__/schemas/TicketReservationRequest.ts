/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { TagList } from './TagList';

export type TicketReservationRequest = { 
  matchTypes?: string[] | null; 
  maxWaitDurationSecs?: number; 
  players?: string[] | null; 
  tags?: Record<string, TagList> | null; 
  team?: string | null; 
  watchOnlineStatus?: boolean; 
};
