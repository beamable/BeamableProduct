import { BeamoV2ServiceDependencyReference } from './BeamoV2ServiceDependencyReference';

export type BeamoV2ServiceStatus = { 
  imageId?: string; 
  isCurrent?: boolean; 
  running?: boolean; 
  serviceDependencyReferences?: BeamoV2ServiceDependencyReference[] | null; 
  serviceName?: string; 
};
