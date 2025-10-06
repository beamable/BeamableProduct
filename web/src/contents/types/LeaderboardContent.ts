import { ContentBase } from '@/contents/types/ContentBase';

export type LeaderboardContent = ContentBase<{
  permissions: {
    data: {
      write_self: boolean;
    };
  };
  partitioned?: {
    data: boolean;
  };
  max_entries?: {
    data: number;
  };
  cohortSettings?: {
    data: {
      cohorts: {
        id: string;
        description?: string;
        statRequirements: {
          domain?: string;
          access?: string;
          stat: string;
          constraint: string;
          value: number;
        }[];
      }[];
    };
  };
}>;
