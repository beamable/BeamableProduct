export type ListTokensRequest = { 
  gamerTagOrAccountId: bigint | string; 
  page: number; 
  pageSize: number; 
  cid?: bigint | string; 
  pid?: string; 
};
