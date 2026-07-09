// Shared storage contract for portal extensions.
//
// This module is INTERFACE ONLY. It declares the shape of `context.storage`
// that extensions compile against — the portal host owns the implementation
// and builds the concrete object at mount time (the same split as the rest of
// the context: `navigate`, `config`, `beam`, etc. are all host-provided; the
// toolkit only types them). See the portal's `extensionStorage.ts`.
//
// The store has three tiers:
//   - `session` — sessionStorage; lives for the tab/session.
//   - `local`   — localStorage / IndexedDB; per-device, survives reloads.
//   - `user`    — durable, server-backed. DEFERRED — rejects until it ships.
//
// Every value is isolated by the extension and the signed-in account (applied
// by the host, never author-controllable). On top of that the author chooses a
// `scope` (`pid` per realm, default, or `cid` across the whole org) and a
// `mount` policy (`all` — shared by every mount of this extension, default —
// or `instance` — per mount site). Values may carry a TTL, evaluated lazily at
// read time. All tiers are asynchronous so code written against `local` today
// is a drop-in for `user` when it lands.
//
// Import via:  import type { ExtensionStorage } from '@beamable/portal-toolkit';
// (or use `context.storage` directly, and the `useStoredState` React hook.)

/**
 * Whose data a value belongs to.
 * - `pid` (default): specific to the current realm/project.
 * - `cid`: shared across every realm under the whole customer org.
 */
export type StorageScope = 'pid' | 'cid';

/**
 * How a value relates to the extension's mount sites.
 * - `all` (default): one value shared by every mount of this extension.
 * - `instance`: a distinct value per mount site, so the same bundle mounted
 *   twice keeps two independent values.
 */
export type StorageMount = 'all' | 'instance';

/**
 * Scope selector for a store. Both dimensions are independent and default to
 * per-realm (`pid`), shared-across-mounts (`all`).
 */
export interface ScopeOptions {
  /** `pid` (default) → this realm; `cid` → shared across the whole org. */
  scope?: StorageScope;
  /** `all` (default) → shared by every mount; `instance` → per mount site. */
  mount?: StorageMount;
}

/** Options for a write. */
export interface SetOptions {
  /**
   * Optional lifetime in milliseconds from the moment of writing. Omitted →
   * the value never expires. Evaluated lazily on read: a read after the TTL
   * has elapsed returns `null` and drops the entry.
   */
  ttl?: number;
}

/**
 * A key/value store bound to a single (tier, extension, account, scope,
 * mount). Every method is asynchronous.
 */
export interface ExtensionStore {
  /** Read a value. Returns `null` if absent or TTL-expired. */
  get<T>(key: string): Promise<T | null>;
  /** Write a JSON-serializable value, optionally with a TTL. */
  set<T>(key: string, value: T, opts?: SetOptions): Promise<void>;
  /** Delete a value. */
  remove(key: string): Promise<void>;
  /** Author keys that are live (non-expired) in this scope. */
  keys(): Promise<string[]>;
  /** Delete every value in this scope (this extension + account + scope only). */
  clear(): Promise<void>;
  /**
   * Observe a key. The handler fires on every write/remove in this runtime,
   * and on cross-tab changes when the host reports them. Returns the
   * unsubscribe function.
   */
  subscribe<T>(key: string, handler: (value: T | null) => void): () => void;
}

/**
 * A storage tier. Calling its methods directly uses the defaults
 * (`scope: 'pid'`, `mount: 'all'`); {@link scope} returns a store bound to a
 * different scope and/or mount policy.
 */
export interface TierStore extends ExtensionStore {
  scope(opts: ScopeOptions): ExtensionStore;
}

/** The `storage` object the host hands to extensions on `ExtensionContext`. */
export interface ExtensionStorage {
  session: TierStore;
  local: TierStore;
  /**
   * Durable, server-backed tier. Deferred — designed for a drop-in landing.
   * Until the host ships it, its operations reject.
   */
  user: TierStore;
}
