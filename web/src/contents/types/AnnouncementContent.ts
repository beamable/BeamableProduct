import { ContentBase } from '@/contents/types/ContentBase';

export type AnnouncementContent = ContentBase<{
  channel: {
    data: string;
  };
  title: {
    data: string;
  };
  summary: {
    data: string;
  };
  body: {
    data: string;
  };
  start_date?: {
    data: string;
  };
  end_date?: {
    data: string;
  };
  attachments: {
    data: {
      count: number;
      symbol: string;
      type: string;
    }[];
  };
  gift: {
    data: {
      description?: string;
      changeCurrencies?: {
        amount: number;
        symbol: string; // currency reference
      }[];
      addItems?: {
        properties?: Record<string, string>;
        symbol: string; // item reference
      }[];
      applyVipBonus?: boolean;
      callWebhooks?: {
        strategy: {
          invocationType: string;
          retryType: string;
        };
        webhookSymbol: string; // api reference
      }[];
    };
  };
  stat_requirements?: {
    data: {
      domain?: string;
      access?: string;
      stat: string;
      constraint: string;
      value: number;
    }[];
  };
  clientData?: {
    data: Record<string, string>;
  };
}>;
