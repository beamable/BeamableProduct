import { JobActivityView } from './JobActivityView';

export type JobActivityViewCursorPagedResult = { 
  nextCursor?: string | null; 
  records?: JobActivityView[]; 
};
