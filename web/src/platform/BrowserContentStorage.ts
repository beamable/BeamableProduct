import { ContentStorage } from '@/platform/types/ContentStorage';

/** Content storage implementation for browsers, using IndexedDB. */
export class BrowserContentStorage extends ContentStorage {
  private readonly db: IDBDatabase;

  private constructor(db: IDBDatabase) {
    super();
    this.db = db;
    // Auto-close the database on upgrade from another tab and mark as closed.
    this.db.onversionchange = () => this.close();
  }

  static async open(): Promise<BrowserContentStorage> {
    const dbName = 'beam-content-db';
    const version = 1;
    const db = await openIndexedDB(dbName, ContentStorage.storeName, version);
    return new BrowserContentStorage(db);
  }

  async set<T>(key: string, value: T): Promise<void> {
    if (value === undefined) {
      await this.del(key); // undefined deletes the key
      return;
    }

    const store = this.store('readwrite');
    await toPromise(store.put(value, key));
  }

  async get<T = unknown>(key: string): Promise<T | undefined> {
    const store = this.store('readonly');
    const res = await toPromise(store.get(key));
    return res as T | undefined;
  }

  async has(key: string): Promise<boolean> {
    const store = this.store('readonly');
    const count = await toPromise(store.count(key));
    return count > 0;
  }

  async del(key: string): Promise<void> {
    const store = this.store('readwrite');
    await toPromise(store.delete(key));
  }

  async clear(): Promise<void> {
    const store = this.store('readwrite');
    await toPromise(store.clear());
  }

  close(): void {
    if (!this.closed) {
      super.close();
      this.db.close();
    }
  }

  // Helper to get a transaction + store
  private store(mode: IDBTransactionMode = 'readonly'): IDBObjectStore {
    this.ensureOpen();
    const tx = this.db.transaction(ContentStorage.storeName, mode);
    return tx.objectStore(ContentStorage.storeName);
  }
}

function openIndexedDB(
  dbName: string,
  storeName: string,
  version: number,
): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(dbName, version);

    request.onupgradeneeded = () => {
      const db = request.result;
      if (!db.objectStoreNames.contains(storeName)) {
        // Create an object store for this database
        db.createObjectStore(storeName);
      }
    };

    request.onsuccess = () => resolve(request.result);
    request.onerror = () =>
      reject(request.error ?? new Error('Failed to open IndexedDB'));
  });
}

function toPromise<T>(req: IDBRequest<T>): Promise<T> {
  return new Promise<T>((resolve, reject) => {
    req.onsuccess = () => resolve(req.result as T);
    req.onerror = () =>
      reject(req.error ?? new Error('IndexedDB request failed'));
  });
}
