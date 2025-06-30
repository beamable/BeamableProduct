import { GroupMemberInfo } from './GroupMemberInfo';
import { GroupScoreBinding } from './GroupScoreBinding';
import { GroupUserMember } from './GroupUserMember';
import { InFlightMessage } from './InFlightMessage';

export type GroupUser = { 
  allGroups: GroupUserMember[]; 
  gamerTag: bigint | string; 
  member: GroupMemberInfo; 
  updated: bigint | string; 
  inFlight?: InFlightMessage[]; 
  scores?: GroupScoreBinding[]; 
};
