import { AnnouncementAttachment } from './AnnouncementAttachment';
import { LocalizationRef } from './LocalizationRef';
import { PlayerReward } from './PlayerReward';
import { PlayerStatRequirement } from './PlayerStatRequirement';

export type AnnouncementDto = { 
  body: LocalizationRef; 
  channel: string; 
  summary: LocalizationRef; 
  symbol: string; 
  title: LocalizationRef; 
  attachments?: AnnouncementAttachment[]; 
  clientData?: Record<string, string>; 
  end_date?: string; 
  gift?: PlayerReward; 
  start_date?: string; 
  stat_requirements?: PlayerStatRequirement[]; 
  tags?: string[]; 
};
