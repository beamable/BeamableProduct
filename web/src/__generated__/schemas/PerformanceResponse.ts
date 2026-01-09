/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { DatabaseMeasurements } from './DatabaseMeasurements';
import type { PANamespace } from './PANamespace';
import type { PASlowQuery } from './PASlowQuery';
import type { PASuggestedIndex } from './PASuggestedIndex';

export type PerformanceResponse = { 
  databaseMeasurements: DatabaseMeasurements; 
  indexes: PASuggestedIndex[]; 
  namespaces: PANamespace[]; 
  queries: PASlowQuery[]; 
};
