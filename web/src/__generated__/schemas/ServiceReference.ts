/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { BundleOrigin } from './BundleOrigin';
import type { ServiceComponent } from './ServiceComponent';
import type { ServiceDependencyReference } from './ServiceDependencyReference';
import type { LogProvider } from './enums/LogProvider';

export type ServiceReference = { 
  archived?: boolean; 
  checksum?: string; 
  comments?: string | null; 
  components?: ServiceComponent[] | null; 
  containerHealthCheckPort?: number | null; 
  dependencies?: ServiceDependencyReference[] | null; 
  enabled?: boolean; 
  imageCpuArch?: string | null; 
  imageId?: string; 
  logProvider?: LogProvider; 
  origin?: BundleOrigin; 
  serviceName?: string; 
  templateId?: string; 
};
