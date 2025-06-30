import { JobDefinitionView } from './JobDefinitionView';

export type JobDefinitionViewCursorPagedResult = { 
  nextCursor?: string | null; 
  records?: JobDefinitionView[]; 
};
