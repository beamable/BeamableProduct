/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { BeamoV2PortalExtensionReference } from './BeamoV2PortalExtensionReference';
import type { BeamoV2ServiceReference } from './BeamoV2ServiceReference';
import type { BeamoV2ServiceStorageReference } from './BeamoV2ServiceStorageReference';

export type BeamoV2PostManifestRequest = { 
  autoDeploy?: boolean; 
  comments?: string | null; 
  manifest?: BeamoV2ServiceReference[]; 
  portalExtensionReferences?: BeamoV2PortalExtensionReference[] | null; 
  storageReferences?: BeamoV2ServiceStorageReference[]; 
};
