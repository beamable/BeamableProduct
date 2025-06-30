import { AttachmentProperty } from './AttachmentProperty';

export type AnnouncementAttachment = { 
  count: number; 
  symbol: string; 
  properties?: AttachmentProperty[]; 
  type?: string; 
};
