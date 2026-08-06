/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { AuthV2ChallengeSolution } from './AuthV2ChallengeSolution';
import type { AuthV2ContextInfo } from './AuthV2ContextInfo';

export type AuthV2ExternalAuthRequest = { 
  challengeSolution?: AuthV2ChallengeSolution; 
  context?: AuthV2ContextInfo; 
  customerId?: string | null; 
  hasProviderNamespace?: boolean; 
  provider?: string | null; 
  providerNamespace?: string | null; 
  realmId?: string | null; 
  scope?: string | null; 
  token?: string | null; 
};
