import { BeamoV2OrderDirection } from './enums/BeamoV2OrderDirection';

export type BeamoV2StartServiceLogsRequest = { 
  endTime?: Date | null; 
  filters?: string[] | null; 
  limit?: number | null; 
  order?: BeamoV2OrderDirection; 
  serviceName?: string; 
  startTime?: Date | null; 
};
