import { TrialCustomRule } from './TrialCustomRule';

export type CreateTrialRestRequest = { 
  cohortType: string; 
  cohorts: string; 
  name: string; 
  strat: string; 
  customRules?: TrialCustomRule[]; 
};
