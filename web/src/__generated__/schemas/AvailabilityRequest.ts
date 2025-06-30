import { GroupType } from './enums/GroupType';

export type AvailabilityRequest = { 
  type: GroupType; 
  name?: string; 
  subGroup?: boolean; 
  tag?: string; 
};
