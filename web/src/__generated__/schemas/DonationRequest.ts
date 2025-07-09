import { Currency } from './Currency';
import { DonationEntry } from './DonationEntry';

export type DonationRequest = { 
  currency: Currency; 
  playerId: bigint | string; 
  progress: DonationEntry[]; 
  satisfied: boolean; 
  timeRequested: bigint | string; 
};
