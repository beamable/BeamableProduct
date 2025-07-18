import { BeamoV2SupportedFederation } from './BeamoV2SupportedFederation';

export type BeamoV2ServiceRegistrationQuery = { 
  federation?: BeamoV2SupportedFederation; 
  localOnly?: boolean | null; 
  routingKey?: string | null; 
  serviceName?: string | null; 
};
