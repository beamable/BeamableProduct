export type TransferRequest = { 
  recipientPlayer: bigint | string; 
  currencies?: Record<string, bigint | string>; 
  transaction?: string; 
};
