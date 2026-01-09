/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { DataDomain } from './DataDomain';
import type { ServiceLimits } from './ServiceLimits';

export type ServicePlan = { 
  dataDomain: DataDomain; 
  name: string; 
  created?: bigint | string; 
  limits?: ServiceLimits; 
  minCustomerStatusSaved?: string; 
};
