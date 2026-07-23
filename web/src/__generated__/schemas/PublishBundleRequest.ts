/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { BeamoBundleActorServiceReference } from './BeamoBundleActorServiceReference';
import type { BeamoBundleActorServiceStorageReference } from './BeamoBundleActorServiceStorageReference';
import type { BundlePeerDep } from './BundlePeerDep';
import type { PortalExtensionReference } from './PortalExtensionReference';

export type PublishBundleRequest = { 
  peerDependencies?: Record<string, BundlePeerDep>; 
  portalExtensionReferences?: PortalExtensionReference[]; 
  serviceReferences?: BeamoBundleActorServiceReference[]; 
  storageReferences?: BeamoBundleActorServiceStorageReference[]; 
  tag?: string | null; 
};
