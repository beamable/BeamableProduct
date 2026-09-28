/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { SegmentFlowBucketResponse } from './SegmentFlowBucketResponse';
import type { SegmentFlowTotals } from './SegmentFlowTotals';
import type { FlowBucketUnit } from './enums/FlowBucketUnit';

export type SegmentFlowResponse = { 
  bucket: FlowBucketUnit; 
  buckets: SegmentFlowBucketResponse[]; 
  coverageStartsAt: Date; 
  from: Date; 
  maxWindowDays: number; 
  memberCount: bigint | string; 
  requestedFrom: Date; 
  segmentId: string; 
  to: Date; 
  totals: SegmentFlowTotals; 
  truncated: boolean; 
  lastEvaluatedAt?: Date | null; 
};
