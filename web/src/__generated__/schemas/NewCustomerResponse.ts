import { TokenResponse } from './TokenResponse';

export type NewCustomerResponse = { 
  activationPending: boolean; 
  cid: bigint | string; 
  name: string; 
  pid: string; 
  projectName: string; 
  token: TokenResponse; 
  alias?: string; 
};
