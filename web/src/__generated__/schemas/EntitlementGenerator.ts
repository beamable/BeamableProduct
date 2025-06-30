import { EntitlementClaimWindow } from './EntitlementClaimWindow';

export type EntitlementGenerator = { 
  action: string; 
  symbol: string; 
  claimWindow?: EntitlementClaimWindow; 
  params?: Record<string, string>; 
  quantity?: number; 
  specialization?: string; 
};
