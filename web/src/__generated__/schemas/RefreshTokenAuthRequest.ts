import { AuthActorContextInfo } from './AuthActorContextInfo';

export type RefreshTokenAuthRequest = { 
  context?: AuthActorContextInfo; 
  customerId?: string | null; 
  realmId?: string | null; 
  refreshToken?: string | null; 
};
