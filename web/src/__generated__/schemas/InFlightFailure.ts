import { InFlightMessage } from './InFlightMessage';

export type InFlightFailure = { 
  id: string; 
  inFlightMessage: InFlightMessage; 
  lastError: string; 
  serviceName: string; 
  serviceObjectId: string; 
  timestamp: bigint | string; 
};
