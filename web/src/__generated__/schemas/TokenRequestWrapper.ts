/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { ChallengeSolution } from './ChallengeSolution';
import type { ContextInfo } from './ContextInfo';

export type TokenRequestWrapper = { 
  grant_type: string; 
  challenge_solution?: ChallengeSolution; 
  client_id?: string; 
  code?: string; 
  context?: ContextInfo; 
  customerScoped?: boolean; 
  device_id?: string; 
  external_token?: string; 
  initProperties?: Record<string, string>; 
  password?: string; 
  provider_namespace?: string; 
  provider_service?: string; 
  redirect_uri?: string; 
  refresh_token?: string; 
  scope?: string[]; 
  third_party?: string; 
  token?: string; 
  username?: string; 
};
