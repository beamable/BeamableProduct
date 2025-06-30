export type MakeDonationRequest = { 
  amount: bigint | string; 
  recipientId: bigint | string; 
  autoClaim?: boolean; 
};
