/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { RealmSecretsOperation } from './enums/RealmSecretsOperation';

export type RealmSecretsAuditInfo = { 
  operation: RealmSecretsOperation; 
  timestamp: Date; 
  rc?: Record<string, string>; 
  secretKey?: string | null; 
};
