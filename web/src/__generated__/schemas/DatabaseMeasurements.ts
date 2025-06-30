import { DatabaseMeasurement } from './DatabaseMeasurement';
import { Link } from './Link';

export type DatabaseMeasurements = { 
  databaseName: string; 
  links: Link[]; 
  end?: string; 
  granularity?: string; 
  groupId?: string; 
  hostId?: string; 
  measurements?: DatabaseMeasurement[]; 
  processId?: string; 
  start?: string; 
};
