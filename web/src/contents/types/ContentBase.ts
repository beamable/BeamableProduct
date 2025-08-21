/**
 * Base interface for content types.
 * This interface defines the common structure for all content types in the system.
 */
export interface ContentBase<T = unknown> {
  id: string;
  version: string;
  uri: string;
  tags: string[];
  createdAt?: bigint | string;
  properties: T;
}
