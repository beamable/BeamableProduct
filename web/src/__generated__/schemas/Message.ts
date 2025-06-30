import { Attachment } from './Attachment';
import { MailRewards } from './MailRewards';
import { PlayerReward } from './PlayerReward';

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
