/**
 * Represents a service that can be refreshed.
 * @template T The type of data returned by the refresh operation.
 */
export interface RefreshableService<T> {
  refresh(data?: T): Promise<T>;
}
