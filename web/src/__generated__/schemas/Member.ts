import { GroupScoreBinding } from './GroupScoreBinding';

export type Member = { 
  gamerTag: bigint | string; 
  role: string; 
  canDemote?: boolean; 
  canKick?: boolean; 
  canPromote?: boolean; 
  scores?: GroupScoreBinding[]; 
};
