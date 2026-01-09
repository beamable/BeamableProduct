/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { BeamoV2DatabaseMeasurements } from './BeamoV2DatabaseMeasurements';
import type { BeamoV2PANamespace } from './BeamoV2PANamespace';
import type { BeamoV2PASlowQuery } from './BeamoV2PASlowQuery';
import type { BeamoV2PASuggestedIndex } from './BeamoV2PASuggestedIndex';

export type BeamoV2StoragePerformance = { 
  databaseMeasurements: BeamoV2DatabaseMeasurements; 
  indexes: BeamoV2PASuggestedIndex[]; 
  namespaces: BeamoV2PANamespace[]; 
  queries: BeamoV2PASlowQuery[]; 
};
