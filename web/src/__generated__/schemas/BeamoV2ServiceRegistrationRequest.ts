import { BeamoV2SupportedFederation } from './BeamoV2SupportedFederation';

export type BeamoV2ServiceRegistrationRequest = { 
  federation?: BeamoV2SupportedFederation[] | null; 
  routingKey?: string | null; 
  trafficFilterEnabled?: boolean; 
};
