import { AuthActorContextInfo } from './AuthActorContextInfo';

export type GuestAuthRequest = { 
  context?: AuthActorContextInfo; 
  customerId?: string | null; 
  initProperties?: Record<string, string | null> | null; 
  realmId?: string | null; 
  scope?: string | null; 
};
