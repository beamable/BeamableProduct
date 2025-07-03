import { GroupMetaData } from './GroupMetaData';
import { GroupType } from './enums/GroupType';

export type GroupMembershipResponse = { 
  group: GroupMetaData; 
  member: boolean; 
  subGroups: (bigint | string)[]; 
  type: GroupType; 
  gamerTag?: bigint | string; 
};
