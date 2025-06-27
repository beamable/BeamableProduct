export type CreateDonationRequest = { 
  amount: bigint | string; 
  currencyId: string; 
  config?: string; 
};
