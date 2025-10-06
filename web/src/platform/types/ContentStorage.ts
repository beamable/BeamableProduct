import { BeamError } from '@/constants/Errors';

/**
 * Abstraction for managing and storing Beamable contents.
 */
export abstract class ContentStorage {
  /** The name of the content storage. */
  protected static readonly storeName = 'beam_content_store';
  protected closed = false;

  /** Opens a content storage. */
  static async open(): Promise<ContentStorage> {
    throw new BeamError(
      'Use a concrete implementation: BrowserContentStorage or NodeContentStorage.',
    );
  }

  /** Sets a value in the content storage. */
  abstract set<T>(key: string, value: T): Promise<void>;

  /** Gets a value from the content storage. */
  abstract get<T = unknown>(key: string): Promise<T | undefined>;

  /** Checks if a value exists in the content storage. */
  abstract has(key: string): Promise<boolean>;

  /** Deletes a value from the content storage. */
  abstract del(key: string): Promise<void>;

  /** Clears the entire content storage. */
  abstract clear(): Promise<void>;

  /** Closes the content storage. */
  close(): void {
    this.closed = true;
  }

  /** Ensures the content storage is open. */
  protected ensureOpen(): void {
    if (this.closed) {
      throw new BeamError('ContentStorage is closed');
    }
  }
}
