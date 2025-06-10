import { Project } from './Project';
import { RealmsBasicAccount } from './RealmsBasicAccount';

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
