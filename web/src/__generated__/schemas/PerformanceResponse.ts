import { DatabaseMeasurements } from './DatabaseMeasurements';
import { PANamespace } from './PANamespace';
import { PASlowQuery } from './PASlowQuery';
import { PASuggestedIndex } from './PASuggestedIndex';

export type PerformanceResponse = { 
  databaseMeasurements: DatabaseMeasurements; 
  indexes: PASuggestedIndex[]; 
  namespaces: PANamespace[]; 
  queries: PASlowQuery[]; 
};
