/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { ExternalIdentity } from './ExternalIdentity';
import type { RoleMapping } from './RoleMapping';

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
