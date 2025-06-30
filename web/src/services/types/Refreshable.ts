/**
 * Interface for objects that can be refreshed.
 * @template T - The type of the object to be refreshed.
 */
export interface Refreshable<T> {
  refresh(): Promise<T>;
}
