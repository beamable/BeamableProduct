import AsyncStorage from '@react-native-async-storage/async-storage';
import { ContentStorage } from '@/platform/types/ContentStorage';
import { BeamJsonUtils } from '@/utils/BeamJsonUtils';

/**
 * Content storage implementation for React Native, backed by AsyncStorage.
 *
 * Values are serialized with {@link BeamJsonUtils} so `BigInt` content IDs and
 * `Date` values round-trip correctly (AsyncStorage only stores strings — a plain
 * `JSON.stringify` would throw on the large numeric IDs the SDK produces).
 */
export class ReactNativeContentStorage extends ContentStorage {
  // Namespaces AsyncStorage keys so content never collides with tokens / config.
  private static readonly keyPrefix = `${ContentStorage.storeName}:`;

  static async open(): Promise<ReactNativeContentStorage> {
    return new ReactNativeContentStorage();
  }

  async set<T>(key: string, value: T): Promise<void> {
    this.ensureOpen();

    if (value === undefined) {
      await this.del(key); // undefined deletes the key
      return;
    }

    const data = JSON.stringify(value, BeamJsonUtils.replacer);
    await AsyncStorage.setItem(this.storageKey(key), data);
  }

  async get<T = unknown>(key: string): Promise<T | undefined> {
    this.ensureOpen();
    const data = await AsyncStorage.getItem(this.storageKey(key));
    if (data === null) return undefined;
    return BeamJsonUtils.parse(data) as T;
  }

  async has(key: string): Promise<boolean> {
    this.ensureOpen();
    const data = await AsyncStorage.getItem(this.storageKey(key));
    return data !== null;
  }

  async del(key: string): Promise<void> {
    this.ensureOpen();
    await AsyncStorage.removeItem(this.storageKey(key));
  }

  async clear(): Promise<void> {
    this.ensureOpen();
    const allKeys = await AsyncStorage.getAllKeys();
    const contentKeys = allKeys.filter((k) =>
      k.startsWith(ReactNativeContentStorage.keyPrefix),
    );
    if (contentKeys.length > 0) await AsyncStorage.multiRemove(contentKeys);
  }

  private storageKey(key: string): string {
    return ReactNativeContentStorage.keyPrefix + key;
  }
}
