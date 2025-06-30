import { CohortEntry } from './CohortEntry';
import { SessionUser } from './SessionUser';

export type GamerTag = { 
  platform: string; 
  tag: bigint | string; 
  added?: bigint | string; 
  alias?: string; 
  trials?: CohortEntry[]; 
  user?: SessionUser; 
};
