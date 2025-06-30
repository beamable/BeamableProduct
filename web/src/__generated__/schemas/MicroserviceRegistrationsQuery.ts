import { SupportedFederation } from './SupportedFederation';

export type MicroserviceRegistrationsQuery = { 
  federation?: SupportedFederation; 
  localOnly?: boolean; 
  routingKey?: string; 
  serviceName?: string; 
};
