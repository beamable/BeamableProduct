import { ExternalIdentity } from './ExternalIdentity';
import { RoleMapping } from './RoleMapping';

export type AccountPortalView = { 
  id: bigint | string; 
  scopes: string[]; 
  thirdPartyAppAssociations: string[]; 
  email?: string; 
  external?: ExternalIdentity[]; 
  language?: string; 
  roleString?: string; 
  roles?: RoleMapping[]; 
};
