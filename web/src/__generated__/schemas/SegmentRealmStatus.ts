/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { SegmentEvaluationStatus } from './SegmentEvaluationStatus';

export type SegmentRealmStatus = { 
  realmId: string; 
  evaluating?: boolean; 
  evaluatingSince?: Date | null; 
  evaluation?: SegmentEvaluationStatus; 
  lastEvaluatedAt?: Date | null; 
  memberCount?: bigint | string | null; 
};
