/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { BeamoV2OrderDirection } from './enums/BeamoV2OrderDirection';

export type BeamoV2StartServiceLogsRequest = { 
  endTime?: Date | null; 
  filters?: string[] | null; 
  limit?: number | null; 
  order?: BeamoV2OrderDirection; 
  serviceName?: string; 
  startTime?: Date | null; 
};
