/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { Attachment } from './Attachment';
import type { MailRewards } from './MailRewards';
import type { PlayerReward } from './PlayerReward';

export type Message = { 
  attachments: Attachment[]; 
  category: string; 
  id: bigint | string; 
  receiverGamerTag: bigint | string; 
  senderGamerTag: bigint | string; 
  sent: bigint | string; 
  state: string; 
  body?: string; 
  bodyRef?: bigint | string; 
  claimedTimeMs?: bigint | string; 
  expires?: bigint | string; 
  playerRewards?: PlayerReward; 
  rewards?: MailRewards; 
  subject?: string; 
};
