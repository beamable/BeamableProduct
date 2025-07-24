/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { DonationRequest } from './DonationRequest';
import type { GroupRole } from './GroupRole';
import type { InFlightMessage } from './InFlightMessage';
import type { Member } from './Member';
import type { GroupType } from './enums/GroupType';

export type Group = { 
  created: bigint | string; 
  enrollmentType: string; 
  freeSlots: number; 
  id: bigint | string; 
  leader: bigint | string; 
  maxSize: number; 
  members: Member[]; 
  motd: string; 
  name: string; 
  requirement: bigint | string; 
  scores: Record<string, string>; 
  slogan: string; 
  subGroups: Group[]; 
  type: GroupType; 
  canDisband?: boolean; 
  canUpdateEnrollment?: boolean; 
  canUpdateMOTD?: boolean; 
  canUpdateSlogan?: boolean; 
  clientData?: string; 
  donations?: DonationRequest[]; 
  inFlight?: InFlightMessage[]; 
  maybeDonations?: Record<string, DonationRequest>; 
  roles?: GroupRole[]; 
  shard?: string; 
  tag?: string; 
  version?: number; 
};
