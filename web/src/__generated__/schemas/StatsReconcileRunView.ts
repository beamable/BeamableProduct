/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { DatasetImportError } from './DatasetImportError';
import type { StatsReconcileChunkView } from './StatsReconcileChunkView';
import type { StatsReconcileCounts } from './StatsReconcileCounts';
import type { StatsReconcileMapping } from './StatsReconcileMapping';
import type { StatsReconcileState } from './enums/StatsReconcileState';

export type StatsReconcileRunView = { 
  chunks?: StatsReconcileChunkView[]; 
  counts?: StatsReconcileCounts; 
  createdAt?: Date; 
  createdBy?: string | null; 
  currentChunk?: number; 
  error?: DatasetImportError; 
  events?: string[]; 
  finishedAt?: Date | null; 
  from?: string; 
  id?: string; 
  importId?: string | null; 
  mappings?: StatsReconcileMapping[]; 
  orderDependentStats?: string[]; 
  startedAt?: Date | null; 
  state?: StatsReconcileState; 
  to?: string; 
};
