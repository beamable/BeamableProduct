/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { TournamentCurrencyReward } from './TournamentCurrencyReward';

export type TournamentEntry = { 
  currencyRewards: TournamentCurrencyReward[]; 
  playerId: bigint | string; 
  rank: bigint | string; 
  score: number; 
  stage: number; 
  stageChange: number; 
  tier: number; 
  nextStageChange?: number; 
  previousStageChange?: number; 
};
