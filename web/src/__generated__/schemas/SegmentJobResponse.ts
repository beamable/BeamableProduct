/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { SegmentJobKind } from './enums/SegmentJobKind';
import type { SegmentJobState } from './enums/SegmentJobState';

export type SegmentJobResponse = { 
  attempts: number; 
  changed: bigint | string; 
  createdAt: Date; 
  id: string; 
  kind: SegmentJobKind; 
  players: bigint | string; 
  segmentId: string; 
  state: SegmentJobState; 
  updatedAt: Date; 
  version: bigint | string; 
  cause?: string | null; 
  error?: string | null; 
  realmId?: string | null; 
};
