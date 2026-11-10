/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { DatasetImportError } from './DatasetImportError';
import type { IdMappingCounts } from './IdMappingCounts';
import type { IdPartError } from './IdPartError';
import type { DatasetImportIdPartState } from './enums/DatasetImportIdPartState';

export type DatasetImportIdPart = { 
  importId: string; 
  outputKey: string; 
  partNo: number; 
  sourceKey: string; 
  attempts?: number; 
  counts?: IdMappingCounts; 
  createdAt?: Date; 
  doneAt?: Date | null; 
  error?: DatasetImportError; 
  errors?: IdPartError[]; 
  id?: string; 
  outputBytes?: bigint | string | null; 
  sourceBytes?: bigint | string | null; 
  state?: DatasetImportIdPartState; 
  updatedAt?: Date; 
};
