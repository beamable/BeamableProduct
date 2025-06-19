export type Token = { 
  cid: bigint | string; 
  created: bigint | string; 
  token: string; 
  type: string; 
  accountId?: bigint | string; 
  device?: string; 
  expiresMs?: bigint | string; 
  gamerTag?: bigint | string; 
  pid?: string; 
  platform?: string; 
  revoked?: boolean; 
  scopes?: string[]; 
};
