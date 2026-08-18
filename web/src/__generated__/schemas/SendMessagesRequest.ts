/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { SendMessageRecipient } from './SendMessageRecipient';

export type SendMessagesRequest = { 
  externalSystemTrackId: string; 
  federationId: string; 
  recipients: SendMessageRecipient[]; 
  analyticsTrackRef?: string | null; 
  delaySeconds?: bigint | string; 
  deliveryLimitTimeSeconds?: bigint | string; 
  extraDataFed?: any | null; 
};
