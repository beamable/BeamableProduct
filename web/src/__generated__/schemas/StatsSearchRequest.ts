import { StatsSearchCriteria } from './StatsSearchCriteria';

export type StatsSearchRequest = { 
  access: string; 
  criteria: StatsSearchCriteria[]; 
  domain: string; 
  objectType: string; 
};
