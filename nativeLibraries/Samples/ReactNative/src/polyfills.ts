/**
 * React Native polyfills for the Beamable Web SDK.
 *
 * The SDK's browser build expects a handful of web globals that don't exist in
 * the Hermes / React Native runtime. This module installs lightweight shims and
 * MUST be imported before any module that imports `@beamable/sdk` or
 * `fake-indexeddb` (we import it first in app/_layout.tsx and
 * src/beam/beamClient.ts).
 *
 * In particular, `fake-indexeddb` (loaded by beamClient.ts, right after this
 * module) needs both `DOMException` and `structuredClone`, so those must be
 * defined here first.
 */

// Spec-compliant URL / URLSearchParams. React Native's built-in URL mishandles
// `new URL(path, baseUrl)`, which the SDK uses to build every request URL — so
// without this, all SDK network calls hit a broken URL and fail.
import 'react-native-url-polyfill/auto';
import structuredClonePolyfill from '@ungap/structured-clone';

const g = globalThis as any;

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
// a naive JSON round-trip cannot serialize ("Do not know how to serialize a
// BigInt").
if (typeof g.structuredClone === 'undefined') {
  g.structuredClone = structuredClonePolyfill;
}

// localStorage — used by the SDK's default config + token storage
// (synchronous, in-memory; real token persistence uses RNTokenStorage).
if (typeof g.localStorage === 'undefined') {
  const store = new Map<string, string>();
  g.localStorage = {
    getItem: (key: string) => (store.has(key) ? store.get(key)! : null),
    setItem: (key: string, value: string) => {
      store.set(key, String(value));
    },
    removeItem: (key: string) => {
      store.delete(key);
    },
    clear: () => {
      store.clear();
    },
    key: (index: number) => Array.from(store.keys())[index] ?? null,
    get length() {
      return store.size;
    },
  };
}

// BroadcastChannel — referenced by the SDK's browser token storage (we override
// token storage, but the symbol may still be touched). No-op.
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
