import { LeaderBoardView } from './LeaderBoardView';
import { MetadataView } from './MetadataView';
import { OrderRules } from './OrderRules';

export type LeaderboardDetails = { 
  fullName: string; 
  lbid: string; 
  numberOfEntries: number; 
  view: LeaderBoardView; 
  metaData?: MetadataView; 
  orules?: OrderRules; 
};
