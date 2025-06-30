import { SupportedFederation } from './SupportedFederation';

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
