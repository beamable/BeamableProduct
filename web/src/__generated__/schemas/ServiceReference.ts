/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { ServiceComponent } from './ServiceComponent';
import type { ServiceDependencyReference } from './ServiceDependencyReference';

export type ServiceReference = { 
  archived: boolean; 
  arm: boolean; 
  checksum: string; 
  enabled: boolean; 
  imageId: string; 
  serviceName: string; 
  templateId: string; 
  comments?: string; 
  components?: ServiceComponent[]; 
  containerHealthCheckPort?: bigint | string; 
  dependencies?: ServiceDependencyReference[]; 
  imageCpuArch?: string; 
};
