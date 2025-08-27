import { ContentBase } from '@/contents/types/ContentBase';

export type StoreContent = ContentBase<{
  title: {
    data: string;
  };
  listings: {
    links: string[]; // listing reference
  };
  showInactiveListings: {
    data: boolean;
  };
  activeListingLimit?: {
    data: number;
  };
}>;
