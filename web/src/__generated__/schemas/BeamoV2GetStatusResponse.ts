import { BeamoV2ServiceStatus } from './BeamoV2ServiceStatus';
import { BeamoV2ServiceStorageStatus } from './BeamoV2ServiceStorageStatus';

export type BeamoV2GetStatusResponse = { 
  isCurrent?: boolean; 
  services?: BeamoV2ServiceStatus[]; 
  storageStatuses?: BeamoV2ServiceStorageStatus[] | null; 
};
