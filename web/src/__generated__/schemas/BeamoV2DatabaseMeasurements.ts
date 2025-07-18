import { BeamoV2DatabaseMeasurement } from './BeamoV2DatabaseMeasurement';
import { BeamoV2Link } from './BeamoV2Link';

export type BeamoV2DatabaseMeasurements = { 
  databaseName: string; 
  end?: Date | null; 
  granularity?: string | null; 
  groupId?: string | null; 
  hostId?: string | null; 
  links?: BeamoV2Link[]; 
  measurements?: BeamoV2DatabaseMeasurement[] | null; 
  processId?: string | null; 
  start?: Date | null; 
};
