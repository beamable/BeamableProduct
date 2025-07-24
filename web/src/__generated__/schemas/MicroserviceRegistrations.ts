/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { SupportedFederation } from './SupportedFederation';

export type MicroserviceRegistrations = { 
  cid: string; 
  instanceCount: number; 
  pid: string; 
  serviceName: string; 
  beamoName?: string; 
  federation?: SupportedFederation[]; 
  routingKey?: string; 
  startedById?: bigint | string; 
  trafficFilterEnabled?: boolean; 
};
