/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { RankEntryStat } from './RankEntryStat';

export type RankEntry = { 
  columns: Record<string, bigint | string>; 
  gt: bigint | string; 
  rank: bigint | string; 
  score?: number; 
  stats?: RankEntryStat[]; 
};
