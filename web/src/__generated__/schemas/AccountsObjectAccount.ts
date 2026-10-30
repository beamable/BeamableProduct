/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { AccountsObjectExternalIdentity } from './AccountsObjectExternalIdentity';
import type { AccountsObjectThirdPartyAssociation } from './AccountsObjectThirdPartyAssociation';
import type { GamerTagAssociation } from './GamerTagAssociation';
import type { InFlightMessage } from './InFlightMessage';
import type { RoleMapping } from './RoleMapping';
import type { ZoneRoleMapping } from './ZoneRoleMapping';

export type AccountsObjectAccount = { 
  createdTimeMillis: bigint | string; 
  external: AccountsObjectExternalIdentity[]; 
  gamerTags: GamerTagAssociation[]; 
  id: bigint | string; 
  privilegedAccount: boolean; 
  thirdParties: AccountsObjectThirdPartyAssociation[]; 
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
