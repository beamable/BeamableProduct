/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { AccountActorExternalIdentity } from './AccountActorExternalIdentity';
import type { RoleMappingView } from './RoleMappingView';

export type AccountActorAccountPortalView = { 
  id: bigint | string; 
  email?: string | null; 
  external?: AccountActorExternalIdentity[] | null; 
  language?: string | null; 
  roleString?: string | null; 
  roles?: RoleMappingView[] | null; 
  scopes?: string[]; 
  thirdPartyAppAssociations?: string[]; 
};
