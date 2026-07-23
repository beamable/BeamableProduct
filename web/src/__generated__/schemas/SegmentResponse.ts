/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { Rule } from './Rule';
import type { SegmentScope } from './enums/SegmentScope';
import type { SegmentState } from './enums/SegmentState';

export type SegmentResponse = { 
  description: string; 
  displayName: string; 
  scope: SegmentScope; 
  segmentId: string; 
  state: SegmentState; 
  version: number; 
  watchedKeys: string[]; 
  createdAt?: Date; 
  lastEvaluatedAt?: Date | null; 
  realmId?: string | null; 
  rule?: Rule; 
  ruleText?: string | null; 
};
