import { CloudStorage } from './CloudStorage';
import { CohortEntry } from './CohortEntry';

export type GetPlayerTrialsResponse = { 
  cohortData: CloudStorage[]; 
  trials: CohortEntry[]; 
};
