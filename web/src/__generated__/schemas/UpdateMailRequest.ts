export type UpdateMailRequest = { 
  mailId: bigint | string; 
  acceptAttachments?: boolean; 
  body?: string; 
  category?: string; 
  expires?: string; 
  state?: string; 
  subject?: string; 
};
