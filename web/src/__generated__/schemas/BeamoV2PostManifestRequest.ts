import { BeamoV2ServiceReference } from './BeamoV2ServiceReference';
import { BeamoV2ServiceStorageReference } from './BeamoV2ServiceStorageReference';

export type BeamoV2PostManifestRequest = { 
  autoDeploy?: boolean; 
  comments?: string | null; 
  manifest?: BeamoV2ServiceReference[]; 
  storageReferences?: BeamoV2ServiceStorageReference[]; 
};
