/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { StatsActorStatsSearchCriteria } from './StatsActorStatsSearchCriteria';
import type { StatsVisibility } from './enums/StatsVisibility';

export type StatsActorStatsSearchRequest = { 
  domain: string; 
  itemType: string; 
  visibility: StatsVisibility; 
  criteria?: StatsActorStatsSearchCriteria[]; 
  limit?: number; 
  offset?: number; 
};
