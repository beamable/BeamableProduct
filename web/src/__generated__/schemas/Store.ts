import { Listing } from './Listing';

export type Store = { 
  listings: Listing[]; 
  symbol: string; 
  activeListingLimit?: number; 
  choose?: number; 
  refreshTime?: number; 
  showInactiveListings?: boolean; 
  title?: string; 
};
