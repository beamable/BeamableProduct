import { SupportedFederation } from './SupportedFederation';

export type SupportedFederationRegistration = { 
  serviceName: string; 
  trafficFilterEnabled: boolean; 
  federation?: SupportedFederation[]; 
  routingKey?: string; 
};
