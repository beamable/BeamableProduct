/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { SegmentEvaluationStatus } from './SegmentEvaluationStatus';
import type { SegmentPropertyResponse } from './SegmentPropertyResponse';
import type { SegmentRealmStatus } from './SegmentRealmStatus';
import type { SegmentScope } from './enums/SegmentScope';
import type { SegmentState } from './enums/SegmentState';

export type SegmentStatusResponse = { 
  properties: Record<string, SegmentPropertyResponse>; 
  realms: SegmentRealmStatus[]; 
  scope: SegmentScope; 
  segmentId: string; 
  state: SegmentState; 
  version: number; 
  evaluating?: boolean; 
  evaluatingSince?: Date | null; 
  evaluation?: SegmentEvaluationStatus; 
  lastEvaluatedAt?: Date | null; 
  memberCount?: bigint | string | null; 
  memberCountAt?: Date | null; 
  propertiesRebuilding?: boolean; 
  realmId?: string | null; 
};
