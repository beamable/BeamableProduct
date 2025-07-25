/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { ExternalIdentity } from './ExternalIdentity';
import type { GamerTagAssociation } from './GamerTagAssociation';
import type { InFlightMessage } from './InFlightMessage';
import type { RoleMapping } from './RoleMapping';
import type { ThirdPartyAssociation } from './ThirdPartyAssociation';

export type Account = { 
  createdTimeMillis: bigint | string; 
  external: ExternalIdentity[]; 
  gamerTags: GamerTagAssociation[]; 
  id: bigint | string; 
  privilegedAccount: boolean; 
  thirdParties: ThirdPartyAssociation[]; 
  updatedTimeMillis: bigint | string; 
  country?: string; 
  deviceId?: string; 
  deviceIds?: string[]; 
  email?: string; 
  heartbeat?: bigint | string; 
  inFlight?: InFlightMessage[]; 
  language?: string; 
  password?: string; 
  realmId?: string; 
  roleString?: string; 
  roles?: RoleMapping[]; 
  userName?: string; 
  wasMigrated?: boolean; 
};
