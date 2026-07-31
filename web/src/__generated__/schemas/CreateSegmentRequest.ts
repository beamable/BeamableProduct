/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { Rule } from './Rule';
import type { SegmentScope } from './enums/SegmentScope';

export type CreateSegmentRequest = { 
  segmentId: string; 
  description?: string | null; 
  displayName?: string | null; 
  excludes?: (bigint | string)[] | null; 
  includes?: (bigint | string)[] | null; 
  rule?: Rule; 
  scope?: SegmentScope; 
};
