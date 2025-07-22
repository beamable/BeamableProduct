import { BeamoV2DatabaseMeasurements } from './BeamoV2DatabaseMeasurements';
import { BeamoV2PANamespace } from './BeamoV2PANamespace';
import { BeamoV2PASlowQuery } from './BeamoV2PASlowQuery';
import { BeamoV2PASuggestedIndex } from './BeamoV2PASuggestedIndex';

export type BeamoV2StoragePerformance = { 
  databaseMeasurements: BeamoV2DatabaseMeasurements; 
  indexes: BeamoV2PASuggestedIndex[]; 
  namespaces: BeamoV2PANamespace[]; 
  queries: BeamoV2PASlowQuery[]; 
};
