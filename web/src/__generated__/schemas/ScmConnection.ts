/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { ScmProviderType } from './enums/ScmProviderType';

export type ScmConnection = { 
  branchPrefix?: string; 
  createdAt?: Date; 
  createdBy?: string | null; 
  defaultRef?: string; 
  hasMyToken?: boolean; 
  id?: string; 
  name?: string; 
  providerType?: ScmProviderType; 
  updatedAt?: Date; 
  updatedBy?: string | null; 
  url?: string; 
  username?: string | null; 
  workspacePath?: string; 
  workspacePaths?: string[]; 
};
