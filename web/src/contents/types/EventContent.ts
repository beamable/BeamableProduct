import { ContentBase } from '@/contents/types/ContentBase';

export type EventContent = ContentBase<{
  name: {
    data: string;
  };
  start_date: {
    data: string;
  };
  partition_size: {
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
  phases: {
    data: {
      name: string;
      duration_minutes: number;
      rules: {
        rule: string;
        value: string;
      }[];
    }[];
  };
  score_rewards: {
    data: {
      min: number;
      max?: number;
      currencies?: {
        id: string; // currency reference
        amount: number;
      }[];
      items?: {
        id: string; // item reference
        properties?: Record<string, string>;
      }[];
    }[];
  };
  rank_rewards: {
    data: {
      min: number;
      max?: number;
      currencies?: {
        id: string; // currency reference
        amount: number;
      }[];
      items?: {
        id: string; // item reference
        properties?: Record<string, string>;
      }[];
    }[];
  };
  stores: {
    data: string[]; // store reference
  };
  group_rewards: {
    data: {
      scoreRewards: {
        min: number;
        max?: number;
        currencies?: {
          id: string; // currency reference
          amount: number;
        }[];
        items?: {
          id: string; // item reference
          properties?: Record<string, string>;
        }[];
      }[];
    };
  };
  permissions: {
    data: {
      write_self: boolean;
    };
  };
  schedule?: {
    data: {
      description: string;
      activeFrom: string;
      definitions: {
        second: string[];
        minute: string[];
        hour: string[];
        dayOfWeek: string[];
        dayOfMonth: string[];
        month: string[];
        year: string[];
      }[];
    };
  };
}>;
