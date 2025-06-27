import { CloudStorage } from './CloudStorage';
import { CustomCohortRule } from './CustomCohortRule';

export type Cohort = { 
  assigned: bigint | string; 
  name: string; 
  cloudData?: CloudStorage[]; 
  customRule?: CustomCohortRule[]; 
  pct?: number; 
  populationCap?: bigint | string; 
};
