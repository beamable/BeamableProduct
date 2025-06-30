import { PlayerListingView } from './PlayerListingView';

export type PlayerStoreView = { 
  listings: PlayerListingView[]; 
  symbol: string; 
  nextDeltaSeconds?: bigint | string; 
  secondsRemain?: bigint | string; 
  title?: string; 
};
