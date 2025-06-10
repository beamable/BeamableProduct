import { ExternalIdentity } from './ExternalIdentity';

export type AccountPlayerView = { 
  deviceIds: string[]; 
  id: bigint | string; 
  scopes: string[]; 
  thirdPartyAppAssociations: string[]; 
  email?: string; 
  external?: ExternalIdentity[]; 
  language?: string; 
};
