/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { GroupScoreBinding } from './GroupScoreBinding';
import type { GroupType } from './enums/GroupType';

export type GroupCreate = { 
  enrollmentType: string; 
  maxSize: number; 
  name: string; 
  requirement: bigint | string; 
  type: GroupType; 
  clientData?: string; 
  group?: bigint | string; 
  scores?: GroupScoreBinding[]; 
  tag?: string; 
  time?: bigint | string; 
};
