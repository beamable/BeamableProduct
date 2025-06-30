import { AnnouncementAttachment } from './AnnouncementAttachment';
import { ClientDataEntry } from './ClientDataEntry';
import { PlayerReward } from './PlayerReward';

export type AnnouncementView = { 
  attachments: AnnouncementAttachment[]; 
  body: string; 
  channel: string; 
  clientDataList: ClientDataEntry[]; 
  id: string; 
  isClaimed: boolean; 
  isDeleted: boolean; 
  isRead: boolean; 
  summary: string; 
  title: string; 
  endDate?: string; 
  gift?: PlayerReward; 
  secondsRemaining?: bigint | string; 
  startDate?: string; 
  tags?: string[]; 
};
