/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { AnnouncementAttachment } from './AnnouncementAttachment';
import type { LocalizationRef } from './LocalizationRef';
import type { PlayerReward } from './PlayerReward';
import type { PlayerStatRequirement } from './PlayerStatRequirement';

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
