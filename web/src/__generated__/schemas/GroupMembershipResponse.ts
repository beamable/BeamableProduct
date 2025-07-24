/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { GroupMetaData } from './GroupMetaData';
import type { GroupType } from './enums/GroupType';

export type GroupMembershipResponse = { 
  group: GroupMetaData; 
  member: boolean; 
  subGroups: (bigint | string)[]; 
  type: GroupType; 
  gamerTag?: bigint | string; 
};
