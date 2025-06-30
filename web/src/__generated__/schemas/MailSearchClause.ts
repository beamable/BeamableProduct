export type MailSearchClause = { 
  name: string; 
  onlyCount: boolean; 
  categories?: string[]; 
  forSender?: bigint | string; 
  limit?: number; 
  start?: bigint | string; 
  states?: string[]; 
};
