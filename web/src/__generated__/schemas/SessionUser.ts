export type SessionUser = { 
  email: string; 
  firstName: string; 
  gamerTag: bigint | string; 
  id: bigint | string; 
  lang: string; 
  lastName: string; 
  name: string; 
  username: string; 
  cid?: string; 
  heartbeat?: bigint | string; 
  password?: string; 
};
