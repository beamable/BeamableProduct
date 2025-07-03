import { RankEntryStat } from './RankEntryStat';

export type RankEntry = { 
  columns: Record<string, bigint | string>; 
  gt: bigint | string; 
  rank: bigint | string; 
  score?: number; 
  stats?: RankEntryStat[]; 
};
