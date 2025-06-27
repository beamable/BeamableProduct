import { ObjectRequest } from './ObjectRequest';

export type ObjectRequests = { 
  playerId?: bigint | string; 
  request?: ObjectRequest[]; 
};
