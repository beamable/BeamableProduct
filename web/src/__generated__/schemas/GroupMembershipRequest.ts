import { GroupType } from './enums/GroupType';

export type GroupMembershipRequest = { 
  group: bigint | string; 
  type: GroupType; 
  score?: bigint | string; 
  subGroup?: bigint | string; 
  successor?: bigint | string; 
};
