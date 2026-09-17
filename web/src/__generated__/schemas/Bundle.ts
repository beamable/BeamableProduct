/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { BundleDep } from './BundleDep';
import type { PortalExtensionReference } from './PortalExtensionReference';
import type { SchemaReference } from './SchemaReference';
import type { ServiceReference } from './ServiceReference';
import type { ServiceStorageReference } from './ServiceStorageReference';

export type Bundle = { 
  bundleDependencies?: BundleDep[]; 
  checksum?: string; 
  name?: string; 
  portalExtensionReferences?: PortalExtensionReference[]; 
  publishedAt?: bigint | string; 
  publisherFullScope?: string | null; 
  schemaReferences?: SchemaReference[]; 
  serviceReferences?: ServiceReference[]; 
  storageReferences?: ServiceStorageReference[]; 
  yanked?: boolean; 
};
