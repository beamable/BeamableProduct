import { ContentBase } from '@/contents/types/ContentBase';

export type SKUContent = ContentBase<{
  description: {
    data: string;
  };
  realPrice: {
    data: number;
  };
  productIds: {
    data: {
      itunes?: string;
      googleplay?: string;
      steam?: number;
    };
  };
}>;
