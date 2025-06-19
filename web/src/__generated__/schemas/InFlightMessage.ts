export type InFlightMessage = { 
  body: string; 
  id: string; 
  method: string; 
  path: string; 
  service: string; 
  gamerTag?: bigint | string; 
  limitFailureRetries?: boolean; 
  shard?: string; 
};
