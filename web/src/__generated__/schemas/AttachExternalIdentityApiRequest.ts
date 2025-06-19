import { ChallengeSolution } from './ChallengeSolution';

export type AttachExternalIdentityApiRequest = { 
  external_token: string; 
  provider_service: string; 
  challenge_solution?: ChallengeSolution; 
  provider_namespace?: string; 
};
