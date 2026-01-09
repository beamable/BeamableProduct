/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { SupportedFederation } from './SupportedFederation';

export type MicroserviceRegistrationRequest = { 
  serviceName: string; 
  federation?: SupportedFederation[]; 
  routingKey?: string; 
  trafficFilterEnabled?: boolean; 
};
