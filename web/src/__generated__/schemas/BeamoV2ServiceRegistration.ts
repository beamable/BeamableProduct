/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { BeamoV2SupportedFederation } from './BeamoV2SupportedFederation';

export type BeamoV2ServiceRegistration = { 
  beamoName?: string | null; 
  cid?: string; 
  federation?: BeamoV2SupportedFederation[] | null; 
  instanceCount?: number; 
  pid?: string; 
  routingKey?: string | null; 
  serviceName?: string; 
  startedById?: bigint | string | null; 
  trafficFilterEnabled?: boolean | null; 
};
