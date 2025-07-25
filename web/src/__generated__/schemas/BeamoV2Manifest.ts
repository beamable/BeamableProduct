/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { BeamoV2ServiceReference } from './BeamoV2ServiceReference';
import type { BeamoV2ServiceStorageReference } from './BeamoV2ServiceStorageReference';

export type BeamoV2Manifest = { 
  checksum?: string; 
  comments?: string | null; 
  created?: bigint | string; 
  createdByAccountId?: bigint | string | null; 
  id?: string; 
  serviceReferences?: BeamoV2ServiceReference[]; 
  storageGroupId?: string | null; 
  storageReferences?: BeamoV2ServiceStorageReference[]; 
};
