/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { DatabaseMeasurement } from './DatabaseMeasurement';
import type { Link } from './Link';

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
