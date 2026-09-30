/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { BundleDepRange } from './BundleDepRange';
import type { PortalExtensionReference } from './PortalExtensionReference';
import type { SchemaReference } from './SchemaReference';
import type { ServiceReference } from './ServiceReference';
import type { ServiceStorageReference } from './ServiceStorageReference';

export type PublishBundleRequest = { 
  bundleDependencies?: Record<string, BundleDepRange>; 
  portalExtensionReferences?: PortalExtensionReference[]; 
  schemaReferences?: SchemaReference[]; 
  serviceReferences?: ServiceReference[]; 
  storageReferences?: ServiceStorageReference[]; 
  tag?: string | null; 
};
