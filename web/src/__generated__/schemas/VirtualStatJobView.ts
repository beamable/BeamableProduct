/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { DatasetImportError } from './DatasetImportError';
import type { VirtualStatJobChunkView } from './VirtualStatJobChunkView';
import type { VirtualStatJobCounts } from './VirtualStatJobCounts';
import type { VirtualStatJobNode } from './VirtualStatJobNode';
import type { StatsVisibility } from './enums/StatsVisibility';
import type { VirtualStatJobKind } from './enums/VirtualStatJobKind';
import type { VirtualStatJobState } from './enums/VirtualStatJobState';

export type VirtualStatJobView = { 
  chunks?: VirtualStatJobChunkView[]; 
  counts?: VirtualStatJobCounts; 
  createdAt?: Date; 
  createdBy?: string | null; 
  currentChunk?: number; 
  error?: DatasetImportError; 
  finishedAt?: Date | null; 
  id?: string; 
  importId?: string | null; 
  key?: string | null; 
  kind?: VirtualStatJobKind; 
  nodes?: VirtualStatJobNode[]; 
  playerId?: string | null; 
  startedAt?: Date | null; 
  state?: VirtualStatJobState; 
  visibility?: StatsVisibility; 
};
