/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { AttachmentRequest } from './AttachmentRequest';
import type { MailRewards } from './MailRewards';
import type { PlayerReward } from './PlayerReward';

export type SendMailObjectRequest = { 
  category: string; 
  senderGamerTag: bigint | string; 
  attachments?: AttachmentRequest[]; 
  body?: string; 
  bodyRef?: bigint | string; 
  expires?: string; 
  id?: bigint | string; 
  playerRewards?: PlayerReward; 
  rewards?: MailRewards; 
  subject?: string; 
};
