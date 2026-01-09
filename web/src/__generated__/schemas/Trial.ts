/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { Cohort } from './Cohort';
import type { CustomCohortRule } from './CustomCohortRule';

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
