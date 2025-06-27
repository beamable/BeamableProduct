import { ExternalIdentity } from './ExternalIdentity';
import { GamerTagAssociation } from './GamerTagAssociation';
import { InFlightMessage } from './InFlightMessage';
import { RoleMapping } from './RoleMapping';
import { ThirdPartyAssociation } from './ThirdPartyAssociation';

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
