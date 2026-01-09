/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CloudStorage } from './CloudStorage';
import type { CustomCohortRule } from './CustomCohortRule';

export type Cohort = { 
  assigned: bigint | string; 
  name: string; 
  cloudData?: CloudStorage[]; 
  customRule?: CustomCohortRule[]; 
  pct?: number; 
  populationCap?: bigint | string; 
};
