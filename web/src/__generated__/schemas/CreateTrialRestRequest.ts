/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { TrialCustomRule } from './TrialCustomRule';

export type CreateTrialRestRequest = { 
  cohortType: string; 
  cohorts: string; 
  name: string; 
  strat: string; 
  customRules?: TrialCustomRule[]; 
};
