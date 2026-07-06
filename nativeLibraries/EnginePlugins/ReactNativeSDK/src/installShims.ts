/**
 * Browser-global shims the Beamable Web SDK's *browser* build assumes but that
 * don't exist in the Hermes / React Native runtime. Importing this module installs
 * them as a side effect. It is kept separate from `fake-indexeddb` so the root
 * `polyfills.ts` can guarantee these run first (fake-indexeddb needs DOMException +
 * structuredClone to already exist).
 */
/// <reference path="./modules.d.ts" />
import 'react-native-url-polyfill/auto';
import structuredClonePolyfill from '@ungap/structured-clone';
import AsyncStorage from '@react-native-async-storage/async-storage';

const g = globalThis as any;

// AsyncStorage key prefix under which the persistent `localStorage` shim mirrors
// its entries. Namespaced so it never collides with the `beam_<pid>_*` token keys
// written by RNTokenStorage.
const LS_PREFIX = 'beam_ls:';
// Backing store for the synchronous localStorage shim. Declared here so
// hydrateLocalStorage() can populate it before the SDK reads it.
const lsStore = new Map<string, string>();

// DOMException — required by fake-indexeddb; missing in Hermes.
if (typeof g.DOMException === 'undefined') {
  g.DOMException = class DOMException extends Error {
    constructor(message?: string, name?: string) {
      super(message);
      this.name = name ?? 'Error';
    }
  };
}

// structuredClone — required by fake-indexeddb v6; missing in Hermes. Must be a
// real structured clone (handles BigInt/Date/Map/Set/circular refs): the SDK's
// JSON parser turns large numeric IDs (gamertag, etc.) into BigInt values, which
// a naive JSON round-trip cannot serialize ("Do not know how to serialize a BigInt").
if (typeof g.structuredClone === 'undefined') {
  g.structuredClone = structuredClonePolyfill;
}

// localStorage — used by the SDK for its realm-config marker (`beam_cid`/`beam_pid`).
// Reads are synchronous (the SDK calls getItem() synchronously inside
// Beam.connect()), so an in-memory Map is the source of truth. Writes also write
// through to AsyncStorage so the marker SURVIVES app restarts: if it didn't,
// Beam.connect() would see the cid "change" from '' → the real cid on every cold
// start, call tokenStorage.clear() (wiping the persisted guest tokens), and create a
// brand-new guest player each launch. Call hydrateLocalStorage() before Beam.init()
// to load persisted entries first.
if (typeof g.localStorage === 'undefined') {
  g.localStorage = {
    getItem: (key: string) => (lsStore.has(key) ? lsStore.get(key)! : null),
    setItem: (key: string, value: string) => {
      lsStore.set(key, String(value));
      void AsyncStorage.setItem(LS_PREFIX + key, String(value));
    },
    removeItem: (key: string) => {
      lsStore.delete(key);
      void AsyncStorage.removeItem(LS_PREFIX + key);
    },
    clear: () => {
      const keys = Array.from(lsStore.keys());
      lsStore.clear();
      void AsyncStorage.multiRemove(keys.map((k) => LS_PREFIX + k));
    },
    key: (index: number) => Array.from(lsStore.keys())[index] ?? null,
    get length() {
      return lsStore.size;
    },
  };
}

/**
 * Loads the persisted `localStorage` entries from AsyncStorage into the
 * synchronous in-memory store. MUST be awaited before `Beam.init()` so the SDK's
 * realm-config marker (`beam_cid` / `beam_pid`) is readable synchronously and the
 * SDK does not mistake a fresh launch for a realm change. Idempotent and never
 * overwrites a value already set in-memory this session.
 */
export async function hydrateLocalStorage(): Promise<void> {
  const allKeys = await AsyncStorage.getAllKeys();
  const lsKeys = allKeys.filter((k) => k.startsWith(LS_PREFIX));
  if (lsKeys.length === 0) return;
  const entries = await AsyncStorage.multiGet(lsKeys);
  for (const [prefixedKey, value] of entries) {
    const key = prefixedKey.slice(LS_PREFIX.length);
    if (value !== null && !lsStore.has(key)) lsStore.set(key, value);
  }
}

// BroadcastChannel — referenced by the SDK's browser token storage. No-op.
if (typeof g.BroadcastChannel === 'undefined') {
  g.BroadcastChannel = class {
    onmessage: ((e: unknown) => void) | null = null;
    onmessageerror: ((e: unknown) => void) | null = null;
    postMessage() {}
    close() {}
    addEventListener() {}
    removeEventListener() {}
  };
}
