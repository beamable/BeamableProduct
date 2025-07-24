/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { ExternalIdentity } from './ExternalIdentity';
import type { GamerTagAssociation } from './GamerTagAssociation';

export type AccountUpdate = { 
  hasThirdPartyToken: boolean; 
  country?: string; 
  deviceId?: string; 
  external?: ExternalIdentity[]; 
  gamerTagAssoc?: GamerTagAssociation; 
  language?: string; 
  thirdParty?: string; 
  token?: string; 
  userName?: string; 
};
