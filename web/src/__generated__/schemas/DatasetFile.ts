/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { DatasetFileError } from './DatasetFileError';
import type { FileSchema } from './FileSchema';
import type { DatasetFileSource } from './enums/DatasetFileSource';
import type { DatasetFileState } from './enums/DatasetFileState';

export type DatasetFile = { 
  datasetId: string; 
  s3Key: string; 
  sizeBytes: bigint | string; 
  source: DatasetFileSource; 
  checksum?: string | null; 
  createdAt?: Date; 
  createdBy?: string | null; 
  declaredSchema?: FileSchema; 
  discardedKey?: string | null; 
  error?: DatasetFileError; 
  id?: string; 
  label?: string | null; 
  originalName?: string | null; 
  partSizeBytes?: bigint | string; 
  schema?: FileSchema; 
  state?: DatasetFileState; 
  updatedAt?: Date; 
  uploadId?: string | null; 
};
