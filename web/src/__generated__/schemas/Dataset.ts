/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { DatasetCsvOptions } from './DatasetCsvOptions';
import type { DatasetIdentity } from './DatasetIdentity';
import type { DatasetSchema } from './DatasetSchema';
import type { DatasetFormat } from './enums/DatasetFormat';
import type { DatasetState } from './enums/DatasetState';

export type Dataset = { 
  format: DatasetFormat; 
  name: string; 
  createdAt?: Date; 
  createdBy?: string | null; 
  csvOptions?: DatasetCsvOptions; 
  description?: string | null; 
  id?: string; 
  identity?: DatasetIdentity; 
  schema?: DatasetSchema; 
  schemaVersion?: number; 
  state?: DatasetState; 
  updatedAt?: Date; 
};
