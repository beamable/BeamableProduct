import { ExternalIdentity } from './ExternalIdentity';
import { GamerTagAssociation } from './GamerTagAssociation';

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
