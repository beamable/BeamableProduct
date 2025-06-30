import { GroupScoreBinding } from './GroupScoreBinding';
import { GroupType } from './enums/GroupType';

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
