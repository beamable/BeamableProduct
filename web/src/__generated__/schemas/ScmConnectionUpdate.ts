/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { ScmProviderType } from './enums/ScmProviderType';

export type ScmConnectionUpdate = { 
  branchPrefix?: string | null; 
  defaultRef?: string | null; 
  id?: string; 
  name?: string; 
  providerType?: ScmProviderType; 
  url?: string; 
  username?: string | null; 
  workspacePath?: string | null; 
  workspacePaths?: string[] | null; 
};
