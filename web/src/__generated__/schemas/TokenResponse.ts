export type TokenResponse = { 
  expires_in: bigint | string; 
  token_type: string; 
  access_token?: string; 
  challenge_token?: string; 
  refresh_token?: string; 
  scopes?: string[]; 
};
