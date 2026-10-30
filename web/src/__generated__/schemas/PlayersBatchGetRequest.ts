/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { StatsVisibility } from './enums/StatsVisibility';

export type PlayersBatchGetRequest = { 
  domain: string; 
  playerIds: (bigint | string)[]; 
  visibility: StatsVisibility; 
  keys?: string[] | null; 
};
