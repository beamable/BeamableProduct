/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { AnnouncementAttachment } from './AnnouncementAttachment';
import type { ClientDataEntry } from './ClientDataEntry';
import type { PlayerReward } from './PlayerReward';

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
