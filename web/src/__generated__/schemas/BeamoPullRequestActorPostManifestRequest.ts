/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { PortalExtensionReference } from './PortalExtensionReference';
import type { SchemaReference } from './SchemaReference';
import type { ServiceReference } from './ServiceReference';
import type { ServiceStorageReference } from './ServiceStorageReference';

export type BeamoPullRequestActorPostManifestRequest = { 
  autoDeploy?: boolean; 
  comments?: string | null; 
  manifest?: ServiceReference[]; 
  portalExtensionReferences?: PortalExtensionReference[] | null; 
  references?: Record<string, string> | null; 
  schemaReferences?: SchemaReference[] | null; 
  schemaVersion?: number | null; 
  storageReferences?: ServiceStorageReference[]; 
};
