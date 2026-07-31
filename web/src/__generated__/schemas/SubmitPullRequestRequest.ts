/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { BeamoPullRequestActorPostManifestRequest } from './BeamoPullRequestActorPostManifestRequest';
import type { PullRequestAclWidening } from './PullRequestAclWidening';

export type SubmitPullRequestRequest = { 
  aclWidenings?: PullRequestAclWidening[]; 
  comment?: string | null; 
  proposed?: BeamoPullRequestActorPostManifestRequest; 
  sourceScopeId?: string | null; 
};
