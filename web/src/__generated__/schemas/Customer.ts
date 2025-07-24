/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { Project } from './Project';
import type { RealmsBasicAccount } from './RealmsBasicAccount';

export type Customer = { 
  accounts: RealmsBasicAccount[]; 
  cid: bigint | string; 
  name: string; 
  projects: Project[]; 
  activationStatus?: string; 
  alias?: string; 
  contact?: string; 
  created?: bigint | string; 
  crm_link?: string; 
  image?: string; 
  paymentStatus?: string; 
  updated?: bigint | string; 
};
