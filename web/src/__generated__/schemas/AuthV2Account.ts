/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { AuthV2ExternalIdentity } from './AuthV2ExternalIdentity';
import type { AuthV2RealmAssociation } from './AuthV2RealmAssociation';
import type { AuthV2RoleAssociation } from './AuthV2RoleAssociation';
import type { AuthV2ThirdPartyAssociation } from './AuthV2ThirdPartyAssociation';
import type { AuthV2ZoneRoleAssociation } from './AuthV2ZoneRoleAssociation';

export type AuthV2Account = { 
  country?: string | null; 
  createdTimeMillis?: bigint | string; 
  deviceIds?: string[] | null; 
  email?: string | null; 
  external?: AuthV2ExternalIdentity[] | null; 
  gamerTags?: AuthV2RealmAssociation[]; 
  id?: bigint | string; 
  language?: string | null; 
  passwordRaw?: string | null; 
  realmId?: string | null; 
  roleString?: string | null; 
  roles?: AuthV2RoleAssociation[] | null; 
  thirdParties?: AuthV2ThirdPartyAssociation[]; 
  updatedTimeMillis?: bigint | string; 
  userName?: string | null; 
  zoneRoles?: AuthV2ZoneRoleAssociation[] | null; 
};
