import { ContentBase } from '@/contents/types/ContentBase';

export type ItemContent = ContentBase<{
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
