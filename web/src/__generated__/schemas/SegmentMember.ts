/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { SegmentEntered } from './SegmentEntered';
import type { MembershipSource } from './enums/MembershipSource';

export type SegmentMember = { 
  accountId: bigint | string; 
  entered?: SegmentEntered; 
  playerId?: bigint | string | null; 
  source?: MembershipSource; 
};
