import { BeamoV2SupportedFederation } from './BeamoV2SupportedFederation';

export type BeamoV2FederationRegistration = { 
  federation?: BeamoV2SupportedFederation[] | null; 
  routingKey?: string | null; 
  serviceName?: string; 
  trafficFilterEnabled?: boolean | null; 
  ttl?: Date | null; 
};
