/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { ScmPublishCommit } from './ScmPublishCommit';
import type { ScmPublishExpectation } from './ScmPublishExpectation';

export type ScmPublishRequest = { 
  baseCommit?: string; 
  commits?: ScmPublishCommit[]; 
  expected?: ScmPublishExpectation; 
  pullRequest?: boolean; 
  pullRequestBody?: string | null; 
  title?: string; 
};
