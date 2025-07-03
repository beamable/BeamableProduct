import { GET, POST, PUT, PATCH, DELETE } from '@/constants';

/** Defines the HTTP methods supported by the Beamable SDK. */
export type HttpMethod =
  | typeof GET
  | typeof POST
  | typeof PUT
  | typeof PATCH
  | typeof DELETE;
