import { AuthActorContextInfo } from './AuthActorContextInfo';

export type PasswordAuthRequest = { 
  context?: AuthActorContextInfo; 
  customerId?: string | null; 
  email?: string | null; 
  password?: string | null; 
  realmId?: string | null; 
  scope?: string | null; 
};
