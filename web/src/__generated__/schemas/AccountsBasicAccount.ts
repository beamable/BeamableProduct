/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { AccountsBasicExternalIdentity } from './AccountsBasicExternalIdentity';
import type { AccountsBasicThirdPartyAssociation } from './AccountsBasicThirdPartyAssociation';
import type { GamerTagAssociation } from './GamerTagAssociation';
import type { InFlightMessage } from './InFlightMessage';
import type { RoleMapping } from './RoleMapping';
import type { ZoneRoleMapping } from './ZoneRoleMapping';

export type AccountsBasicAccount = { 
  createdTimeMillis: bigint | string; 
  external: AccountsBasicExternalIdentity[]; 
  gamerTags: GamerTagAssociation[]; 
  id: bigint | string; 
  privilegedAccount: boolean; 
  thirdParties: AccountsBasicThirdPartyAssociation[]; 
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
  zoneRoles?: ZoneRoleMapping[]; 
};
