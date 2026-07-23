/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { BeamoBasicServiceComponent } from './BeamoBasicServiceComponent';
import type { BeamoBasicServiceDependencyReference } from './BeamoBasicServiceDependencyReference';

export type BeamoBasicServiceReference = { 
  archived: boolean; 
  arm: boolean; 
  checksum: string; 
  enabled: boolean; 
  imageId: string; 
  serviceName: string; 
  templateId: string; 
  comments?: string; 
  components?: BeamoBasicServiceComponent[]; 
  containerHealthCheckPort?: bigint | string; 
  dependencies?: BeamoBasicServiceDependencyReference[]; 
  imageCpuArch?: string; 
};
