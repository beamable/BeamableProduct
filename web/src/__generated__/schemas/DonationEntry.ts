export type DonationEntry = { 
  amount: bigint | string; 
  playerId: bigint | string; 
  time: bigint | string; 
  claimed?: boolean; 
};
