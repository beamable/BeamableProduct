/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { ChallengeSolution } from './ChallengeSolution';

export type AttachExternalIdentityApiRequest = { 
  external_token: string; 
  provider_service: string; 
  challenge_solution?: ChallengeSolution; 
  provider_namespace?: string; 
};
