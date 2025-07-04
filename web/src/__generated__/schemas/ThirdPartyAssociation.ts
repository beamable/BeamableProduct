export type ThirdPartyAssociation = { 
  appId: string; 
  meta: Record<string, string>; 
  name: string; 
  userAppId: string; 
  email?: string; 
  userBusinessId?: string; 
};
