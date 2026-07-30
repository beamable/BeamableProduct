/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { BeamoBasicServiceReference } from './BeamoBasicServiceReference';
import type { BeamoBasicServiceStorageReference } from './BeamoBasicServiceStorageReference';

export type PostManifestRequest = { 
  manifest: BeamoBasicServiceReference[]; 
  autoDeploy?: boolean; 
  comments?: string; 
  storageReferences?: BeamoBasicServiceStorageReference[]; 
};
