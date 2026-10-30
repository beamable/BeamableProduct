/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { AccountActorExternalIdentity } from './AccountActorExternalIdentity';
import type { AccountActorThirdPartyAssociation } from './AccountActorThirdPartyAssociation';
import type { RealmAssociation } from './RealmAssociation';
import type { RoleAssociation } from './RoleAssociation';
import type { ZoneRoleAssociation } from './ZoneRoleAssociation';

export type AccountActorAccount = { 
  country?: string | null; 
  createdTimeMillis?: bigint | string; 
  deviceIds?: string[] | null; 
  email?: string | null; 
  external?: AccountActorExternalIdentity[] | null; 
  gamerTags?: RealmAssociation[]; 
  id?: bigint | string; 
  language?: string | null; 
  passwordRaw?: string | null; 
  realmId?: string | null; 
  roleString?: string | null; 
  roles?: RoleAssociation[] | null; 
  thirdParties?: AccountActorThirdPartyAssociation[]; 
  updatedTimeMillis?: bigint | string; 
  userName?: string | null; 
  zoneRoles?: ZoneRoleAssociation[] | null; 
};
