/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { DatasetImportState } from './enums/DatasetImportState';

export type ImportTransitionRecord = { 
  by: string; 
  from: DatasetImportState; 
  to: DatasetImportState; 
  at?: Date; 
  reason?: string | null; 
};
