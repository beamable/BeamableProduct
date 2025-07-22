import { BeamoV2SupportedFederation } from './BeamoV2SupportedFederation';

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
