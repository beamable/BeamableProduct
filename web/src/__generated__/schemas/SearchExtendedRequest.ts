import { StatsSearchCriteria } from './StatsSearchCriteria';

export type SearchExtendedRequest = { 
  access: string; 
  criteria: StatsSearchCriteria[]; 
  domain: string; 
  objectType: string; 
  statKeys: string[]; 
};
