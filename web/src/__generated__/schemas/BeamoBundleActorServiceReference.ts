/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { BeamoBundleActorServiceComponent } from './BeamoBundleActorServiceComponent';
import type { BeamoBundleActorServiceDependencyReference } from './BeamoBundleActorServiceDependencyReference';
import type { BundleOrigin } from './BundleOrigin';
import type { LogProvider } from './enums/LogProvider';

export type BeamoBundleActorServiceReference = { 
  archived?: boolean; 
  checksum?: string; 
  comments?: string | null; 
  components?: BeamoBundleActorServiceComponent[] | null; 
  containerHealthCheckPort?: number | null; 
  dependencies?: BeamoBundleActorServiceDependencyReference[] | null; 
  enabled?: boolean; 
  imageCpuArch?: string | null; 
  imageId?: string; 
  logProvider?: LogProvider; 
  origin?: BundleOrigin; 
  serviceName?: string; 
  templateId?: string; 
};
