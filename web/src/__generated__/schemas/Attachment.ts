import { EntitlementGenerator } from './EntitlementGenerator';

export type Attachment = { 
  id: bigint | string; 
  state: string; 
  wrapped: EntitlementGenerator; 
  target?: bigint | string; 
};
