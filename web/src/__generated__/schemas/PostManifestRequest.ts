/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { ServiceReference } from './ServiceReference';
import type { ServiceStorageReference } from './ServiceStorageReference';

export type PostManifestRequest = { 
  manifest: ServiceReference[]; 
  autoDeploy?: boolean; 
  comments?: string; 
  storageReferences?: ServiceStorageReference[]; 
};
