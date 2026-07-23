/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { BeamoBasicServiceReference } from './BeamoBasicServiceReference';
import type { BeamoBasicServiceStorageReference } from './BeamoBasicServiceStorageReference';

export type ManifestView = { 
  checksum: string; 
  created: bigint | string; 
  id: string; 
  manifest: BeamoBasicServiceReference[]; 
  comments?: string; 
  createdByAccountId?: bigint | string; 
  storageReference?: BeamoBasicServiceStorageReference[]; 
};
