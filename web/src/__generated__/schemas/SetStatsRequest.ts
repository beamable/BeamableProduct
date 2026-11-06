/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { StatReductions } from './StatReductions';
import type { StatsValue } from './StatsValue';

export type SetStatsRequest = { 
  add?: Record<string, StatsValue> | null; 
  emitAnalytics?: boolean; 
  reduce?: Record<string, StatReductions> | null; 
  set?: Record<string, StatsValue> | null; 
};
