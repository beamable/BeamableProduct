import { GroupStatus } from './GroupStatus';

export type GetStatusForGroupsResponse = { 
  contentId: string; 
  statuses: GroupStatus[]; 
};
