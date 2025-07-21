/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { TournamentCurrencyReward } from './TournamentCurrencyReward';

export type PlayerStatus = { 
  contentId: string; 
  lastUpdateCycle: number; 
  playerId: bigint | string; 
  stage: number; 
  tier: number; 
  tournamentId: string; 
  unclaimedRewards: TournamentCurrencyReward[]; 
  groupId?: bigint | string; 
};
