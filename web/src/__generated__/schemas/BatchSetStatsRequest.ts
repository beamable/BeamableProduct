import { StatUpdateRequest } from './StatUpdateRequest';

export type BatchSetStatsRequest = { 
  updates: StatUpdateRequest[]; 
};
