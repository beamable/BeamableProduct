/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { AccountActorExternalIdentity } from './AccountActorExternalIdentity';
import type { RealmAssociation } from './RealmAssociation';

export type AdminAccountUpdate = { 
  country?: string | null; 
  deviceId?: string | null; 
  external?: AccountActorExternalIdentity[] | null; 
  gamerTagAssoc?: RealmAssociation; 
  language?: string | null; 
  thirdParty?: string | null; 
  token?: string | null; 
  userName?: string | null; 
};
