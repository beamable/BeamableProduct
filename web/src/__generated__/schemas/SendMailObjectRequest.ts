import { AttachmentRequest } from './AttachmentRequest';
import { MailRewards } from './MailRewards';
import { PlayerReward } from './PlayerReward';

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
