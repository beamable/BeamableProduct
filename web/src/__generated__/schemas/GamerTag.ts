/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CohortEntry } from './CohortEntry';
import type { SessionUser } from './SessionUser';

export type GamerTag = { 
  platform: string; 
  tag: bigint | string; 
  added?: bigint | string; 
  alias?: string; 
  trials?: CohortEntry[]; 
  user?: SessionUser; 
};
