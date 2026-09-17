/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { BeamoV2ManifestBundle } from './BeamoV2ManifestBundle';
import type { BeamoV2ServiceStatus } from './BeamoV2ServiceStatus';
import type { BeamoV2ServiceStorageStatus } from './BeamoV2ServiceStorageStatus';

export type BeamoV2GetStatusResponse = { 
  bundles?: BeamoV2ManifestBundle[]; 
  isCurrent?: boolean; 
  services?: BeamoV2ServiceStatus[]; 
  storageStatuses?: BeamoV2ServiceStorageStatus[] | null; 
};
