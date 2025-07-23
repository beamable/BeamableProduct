import { BeamoV2DataPoint } from './BeamoV2DataPoint';

export type BeamoV2DatabaseMeasurement = { 
  name: string; 
  units: string; 
  dataPoints?: BeamoV2DataPoint[]; 
};
