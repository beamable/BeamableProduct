/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { BeamoPullRequestActorPostManifestRequest } from './BeamoPullRequestActorPostManifestRequest';
import type { PullRequestAclWidening } from './PullRequestAclWidening';
import type { PullRequestComment } from './PullRequestComment';
import type { PullRequestStatus } from './enums/PullRequestStatus';

export type ProposedManifest = { 
  aclWidenings?: PullRequestAclWidening[]; 
  comments?: PullRequestComment[]; 
  created?: bigint | string; 
  id?: string; 
  lastApplyError?: string | null; 
  proposed?: BeamoPullRequestActorPostManifestRequest; 
  resolvedAt?: bigint | string | null; 
  resolvedByAccountId?: bigint | string | null; 
  resultManifestId?: string | null; 
  sourceScopeId?: string | null; 
  status?: PullRequestStatus; 
  submittedByAccountId?: bigint | string; 
};
