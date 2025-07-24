/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { ServiceStatus } from './ServiceStatus';
import type { ServiceStorageStatus } from './ServiceStorageStatus';

export type GetStatusResponse = { 
  isCurrent: boolean; 
  services: ServiceStatus[]; 
  storageStatuses?: ServiceStorageStatus[]; 
};
