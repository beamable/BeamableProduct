/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { ServiceDependencyReference } from './ServiceDependencyReference';

export type ServiceStatus = { 
  imageId: string; 
  isCurrent: boolean; 
  running: boolean; 
  serviceName: string; 
  serviceDependencyReferences?: ServiceDependencyReference[]; 
};
