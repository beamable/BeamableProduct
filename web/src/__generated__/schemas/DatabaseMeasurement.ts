import { DataPoint } from './DataPoint';

export type DatabaseMeasurement = { 
  dataPoints: DataPoint[]; 
  name: string; 
  units: string; 
};
