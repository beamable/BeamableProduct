import { BeamoV2FederationType } from './enums/BeamoV2FederationType';

export type BeamoV2SupportedFederation = { 
  nameSpace?: string | null; 
  settings?: any | null; 
  type?: BeamoV2FederationType; 
};
