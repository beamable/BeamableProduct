/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { DatasetCsvOptions } from './DatasetCsvOptions';
import type { SetDatasetIdentityRequest } from './SetDatasetIdentityRequest';
import type { DatasetFormat } from './enums/DatasetFormat';

export type CreateDatasetRequest = { 
  format: DatasetFormat; 
  name: string; 
  csvOptions?: DatasetCsvOptions; 
  description?: string | null; 
  identity?: SetDatasetIdentityRequest; 
};
