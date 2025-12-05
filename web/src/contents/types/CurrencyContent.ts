import { ContentBase } from '@/contents/types/ContentBase';

export type CurrencyContent = ContentBase<{
  startingAmount: {
    data: number;
  };
  icon: {
    data: {
      referenceKey: string;
      subObjectName: string;
    };
  };
  clientPermission: {
    data: {
      write_self: boolean;
    };
  };
  external?: {
    data: {
      service: string;
      namespace: string;
    };
  };
}>;
