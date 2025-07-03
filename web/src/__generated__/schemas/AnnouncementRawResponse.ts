import { AnnouncementState } from './AnnouncementState';

export type AnnouncementRawResponse = { 
  announcements: Record<string, AnnouncementState>; 
};
