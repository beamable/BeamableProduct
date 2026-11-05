/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { AnalyticsSchemaFindingView } from './AnalyticsSchemaFindingView';
import type { JsonNode } from './JsonNode';
import type { AnalyticsSchemaFindingsStatus } from './enums/AnalyticsSchemaFindingsStatus';

export type AnalyticsSchemaFindingsEvent = { 
  count?: bigint | string; 
  document?: JsonNode; 
  event?: string; 
  findings?: AnalyticsSchemaFindingView[]; 
  firstSeen?: Date; 
  lastSeen?: Date; 
  presence?: Record<string, number> | null; 
  status?: AnalyticsSchemaFindingsStatus; 
};
