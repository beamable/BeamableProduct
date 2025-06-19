import { Cohort } from './Cohort';
import { CustomCohortRule } from './CustomCohortRule';

export type Trial = { 
  active: boolean; 
  assigned: bigint | string; 
  cohorts: Cohort[]; 
  ctype: string; 
  name: string; 
  strategy: string; 
  ttype: string; 
  activated?: bigint | string; 
  created?: bigint | string; 
  customRules?: CustomCohortRule[]; 
  scheduleStart?: bigint | string; 
};
