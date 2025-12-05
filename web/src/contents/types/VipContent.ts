import { ContentBase } from '@/contents/types/ContentBase';

export type VipContent = ContentBase<{
  currency: {
    data: string; // currency reference
  };
  tiers: {
    data: {
      name: string;
      qualifyThreshold: number;
      disqualifyThreshold: number;
      multipliers: {
        currency: string; // currency reference
        multiplier: number;
        roundToNearest: number;
      }[];
    }[];
  };
}>;
