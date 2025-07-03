import { DataDomain } from './DataDomain';
import { ServiceLimits } from './ServiceLimits';

export type ServicePlan = { 
  dataDomain: DataDomain; 
  name: string; 
  created?: bigint | string; 
  limits?: ServiceLimits; 
  minCustomerStatusSaved?: string; 
};
