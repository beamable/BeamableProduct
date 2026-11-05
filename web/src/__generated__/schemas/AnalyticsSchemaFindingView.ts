/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { AnalyticsRowsDeletion } from './AnalyticsRowsDeletion';
import type { AnalyticsFindingKind } from './enums/AnalyticsFindingKind';

export type AnalyticsSchemaFindingView = { 
  count?: bigint | string; 
  expected?: string | null; 
  firstSeen?: Date; 
  key?: string; 
  kind?: AnalyticsFindingKind; 
  lastSeen?: Date; 
  observed?: Record<string, bigint | string>; 
  resolvedAt?: Date | null; 
  resolvedByManifestId?: string | null; 
  rowsDeletion?: AnalyticsRowsDeletion; 
  sources?: string[]; 
};
