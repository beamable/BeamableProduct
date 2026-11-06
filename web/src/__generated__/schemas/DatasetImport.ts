/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { DatasetImportError } from './DatasetImportError';
import type { DatasetImportOperation } from './DatasetImportOperation';
import type { DatasetImportRunner } from './DatasetImportRunner';
import type { ImportExecution } from './ImportExecution';
import type { ImportResult } from './ImportResult';
import type { ImportStageState } from './ImportStageState';
import type { ImportTransitionRecord } from './ImportTransitionRecord';
import type { DatasetImportState } from './enums/DatasetImportState';

export type DatasetImport = { 
  datasetId: string; 
  operation: DatasetImportOperation; 
  target: string; 
  createdAt?: Date; 
  createdBy?: string | null; 
  error?: DatasetImportError; 
  executions?: ImportExecution[]; 
  finishedAt?: Date | null; 
  id?: string; 
  result?: ImportResult; 
  runner?: DatasetImportRunner; 
  stages?: Record<string, ImportStageState>; 
  startedAt?: Date | null; 
  state?: DatasetImportState; 
  transitions?: ImportTransitionRecord[]; 
  updatedAt?: Date; 
};
