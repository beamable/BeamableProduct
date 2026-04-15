/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { ContentTagFilter } from './ContentTagFilter';
import type { PropertyFilterDTO } from './PropertyFilterDTO';
import type { TimeRange } from './TimeRange';

export type InventoryFiltersDTO = { 
  contentTagFilter?: ContentTagFilter; 
  createdAt?: TimeRange; 
  propertyFilters?: PropertyFilterDTO[]; 
  proxyIds?: string[]; 
  updatedAt?: TimeRange; 
};
