export type RoleChangeRequest = { 
  gamerTag: bigint | string; 
  role: string; 
  subGroup?: bigint | string; 
};
