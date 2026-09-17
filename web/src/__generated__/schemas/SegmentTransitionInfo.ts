/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { TransitionCause } from './enums/TransitionCause';
import type { TransitionKind } from './enums/TransitionKind';

export type SegmentTransitionInfo = { 
  cause: TransitionCause; 
  kind: TransitionKind; 
  playerId: bigint | string; 
  ruleVersion: number; 
  segmentId: string; 
  timestamp: Date; 
};
