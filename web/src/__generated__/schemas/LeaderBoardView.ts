import { RankEntry } from './RankEntry';

export type LeaderBoardView = { 
  boardSize: bigint | string; 
  lbId: string; 
  rankings: RankEntry[]; 
  rankgt?: RankEntry; 
};
