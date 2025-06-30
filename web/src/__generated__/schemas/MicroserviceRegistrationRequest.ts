import { SupportedFederation } from './SupportedFederation';

export type MicroserviceRegistrationRequest = { 
  serviceName: string; 
  federation?: SupportedFederation[]; 
  routingKey?: string; 
  trafficFilterEnabled?: boolean; 
};
