/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CustomerActorExternalIdentity } from './CustomerActorExternalIdentity';
import type { CustomerActorThirdPartyAssociation } from './CustomerActorThirdPartyAssociation';
import type { RealmAssociation } from './RealmAssociation';
import type { RoleAssociation } from './RoleAssociation';

export type CustomerActorAccount = { 
  accountId?: bigint | string; 
  country?: string | null; 
  createdTimeMs?: bigint | string; 
  deviceIds?: string[] | null; 
  email?: string | null; 
  external?: CustomerActorExternalIdentity[] | null; 
  language?: string | null; 
  password?: string | null; 
  passwordRaw?: string | null; 
  realmAssociations?: RealmAssociation[]; 
  realmId?: string | null; 
  roleString?: string | null; 
  roles?: RoleAssociation[] | null; 
  thirdPartyAssociations?: CustomerActorThirdPartyAssociation[]; 
  updatedTimeMs?: bigint | string; 
  username?: string | null; 
};
