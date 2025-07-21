/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { GroupMemberInfo } from './GroupMemberInfo';
import type { GroupScoreBinding } from './GroupScoreBinding';
import type { GroupUserMember } from './GroupUserMember';
import type { InFlightMessage } from './InFlightMessage';

export type GroupUser = { 
  allGroups: GroupUserMember[]; 
  gamerTag: bigint | string; 
  member: GroupMemberInfo; 
  updated: bigint | string; 
  inFlight?: InFlightMessage[]; 
  scores?: GroupScoreBinding[]; 
};
