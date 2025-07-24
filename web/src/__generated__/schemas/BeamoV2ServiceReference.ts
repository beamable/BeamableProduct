/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { BeamoV2ServiceComponent } from './BeamoV2ServiceComponent';
import type { BeamoV2ServiceDependencyReference } from './BeamoV2ServiceDependencyReference';

export type BeamoV2ServiceReference = { 
  archived?: boolean; 
  checksum?: string; 
  comments?: string | null; 
  components?: BeamoV2ServiceComponent[] | null; 
  containerHealthCheckPort?: number | null; 
  dependencies?: BeamoV2ServiceDependencyReference[] | null; 
  enabled?: boolean; 
  imageCpuArch?: string | null; 
  imageId?: string; 
  serviceName?: string; 
  templateId?: string; 
};
