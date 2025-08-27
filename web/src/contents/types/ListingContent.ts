import { ContentBase } from '@/contents/types/ContentBase';

export type ListingContent = ContentBase<{
  price: {
    data: {
      type: string;
      symbol: string;
      amount: number;
    };
  };
  offer: {
    data: {
      titles?: string[];
      descriptions?: string[];
      obtainCurrency: {
        symbol: string; // currency reference
        amount: number;
      }[];
      obtainItems: {
        contentId: string; // item reference
        properties: {
          name: string;
          value: string;
        }[];
      }[];
    };
  };
  activePeriod?: {
    data: {
      start: string;
      end?: string;
    };
  };
  purchaseLimit?: {
    data: number;
  };
  cohortRequirements?: {
    data: {
      trial: string;
      cohort: string;
      constraint: string;
    }[];
  };
  playerStatRequirements?: {
    data: {
      domain?: string;
      access?: string;
      stat: string;
      constraint: string;
      value: number;
    }[];
  };
  offerRequirements?: {
    data: {
      offerSymbol: string; // listing reference
      purchases: {
        constraint: string;
        value: number;
      };
    }[];
  };
  clientData?: {
    data: Record<string, string>;
  };
  activeDurationSeconds?: {
    data: number;
  };
  activeDurationCoolDownSeconds?: {
    data: number;
  };
  activeDurationPurchaseLimit?: {
    data: number;
  };
  buttonText?: {
    data: string;
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
  scheduleInstancePurchaseLimit?: {
    data: number;
  };
}>;
