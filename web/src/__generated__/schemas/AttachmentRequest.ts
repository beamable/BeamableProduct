export type AttachmentRequest = { 
  action: string; 
  symbol: string; 
  quantity?: number; 
  specialization?: string; 
  target?: bigint | string; 
};
