/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { CallerInfo } from './CallerInfo';
import type { SegmentAuditChange } from './SegmentAuditChange';
import type { SegmentationOperation } from './enums/SegmentationOperation';

export type SegmentAuditInfo = { 
  caller: CallerInfo; 
  operation: SegmentationOperation; 
  timestamp: Date; 
  changes?: Record<string, SegmentAuditChange> | null; 
  segmentId?: string | null; 
};
