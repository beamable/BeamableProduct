/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { BeamoV2BundleOrigin } from './BeamoV2BundleOrigin';
import type { BeamoV2ServiceDependencyReference } from './BeamoV2ServiceDependencyReference';

export type BeamoV2ServiceStatus = { 
  imageId?: string; 
  isCurrent?: boolean; 
  origin?: BeamoV2BundleOrigin; 
  running?: boolean; 
  serviceDependencyReferences?: BeamoV2ServiceDependencyReference[] | null; 
  serviceName?: string; 
};
