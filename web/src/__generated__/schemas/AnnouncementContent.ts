import { AnnouncementAttachment } from './AnnouncementAttachment';
import { PlayerReward } from './PlayerReward';
import { PlayerStatRequirement } from './PlayerStatRequirement';

export type AnnouncementContent = { 
  body: string; 
  channel: string; 
  summary: string; 
  symbol: string; 
  title: string; 
  attachments?: AnnouncementAttachment[]; 
  clientData?: Record<string, string>; 
  end_date?: string; 
  gift?: PlayerReward; 
  start_date?: string; 
  stat_requirements?: PlayerStatRequirement[]; 
  tags?: string[]; 
};
