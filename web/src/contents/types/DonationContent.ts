import { ContentBase } from '@/contents/types/ContentBase';

export type DonationContent = ContentBase<{
  requestCooldownSecs: {
    data: number;
  };
  allowedCurrencies: {
    data: string[]; // currency reference
  };
}>;
