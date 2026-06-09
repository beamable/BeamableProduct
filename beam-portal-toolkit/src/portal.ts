// Portal extension registration utilities.
// Import via:  import { Portal } from '@beamable/portal-toolkit';

import { Beam, BeamBase } from "@beamable/sdk";

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

/**
 * A read-only snapshot of the portal URL at the moment the extension is
 * mounted. Captures `pathname`, `search`, and `hash`. To observe URL changes
 * after mount, extensions can read `window.location` directly or listen for
 * `popstate` — `context.location` is intentionally a snapshot, not a live ref.
 */
export interface ExtensionLocation {
  pathname: string;
  search: string;
  hash: string;
}

/**
 * Navigate the host portal to another route.
 *
 * Path resolution follows two rules:
 * - A path that starts with `/` is treated as **absolute** — the extension
 *   has constructed the full URL itself (e.g. `/<cid>/games/<gid>/realms/<rid>/foo`).
 * - A path without a leading `/` is **realm-relative** — the portal will
 *   prefix it with `/<cid>/games/<gid>/realms/<rid>/` automatically.
 *
 * @example
 *   context.navigate('analytics/dashboard');         // realm-relative
 *   context.navigate('/auth/login');                 // absolute
 *   context.navigate('players/abc', { replace: true });
 */
export type ExtensionNavigate = (path: string, opts?: { replace?: boolean }) => void;

/**
 * A read-only observable of portal-wide state. Call {@link get} for the
 * current value and {@link subscribe} to be notified on every change —
 * the handler receives the new value and the returned function is the
 * unsubscribe. Use these to react to portal-level settings (date range,
 * theme, etc.) without polling.
 *
 * @example
 *   // Render once with the current date range, then re-render on changes.
 *   const initial = context.config.dateRange.get();
 *   const unsubscribe = context.config.dateRange.subscribe((next) => {
 *     console.log('date range is now', next);
 *   });
 *   // …later, on extension unmount:
 *   unsubscribe();
 */
export interface ExtensionObservable<T> {
  get(): T;
  subscribe(handler: (value: T) => void): () => void;
}

/**
 * The portal-wide state extensions can react to. Every entry is a
 * read-only observable — extensions never write back through this channel.
 *
 * - `dateRange`: the active range selection from the portal's TopBar.
 *   Values: `'7d' | '30d' | '90d' | 'custom'`. Extensions that filter by
 *   time should subscribe here so the whole portal stays in sync.
 * - `timezone`: the user's preferred IANA zone, or the literal `'local'`
 *   sentinel (resolve to `Intl.DateTimeFormat().resolvedOptions().timeZone`
 *   when displaying dates).
 * - `theme`: `'dark'` or `'light'`. Useful for picking chart palettes,
 *   conditional images, etc. Theme tokens already inherit through the
 *   shadow boundary, so most styling needs no explicit branching.
 * - `account`: the logged-in user identity, or `null` when unauthenticated.
 *   Carries `{ id, email, role }`.
 */
export interface ExtensionConfig {
  dateRange: ExtensionObservable<string>;
  timezone: ExtensionObservable<string>;
  theme: ExtensionObservable<'dark' | 'light'>;
  account: ExtensionObservable<{ id: string; email: string; role: string } | null>;
}

/**
 * A single mount declaration from the extension's manifest. An extension
 * can declare multiple mounts via `Properties.mounts`; the portal mounts
 * the bundle once per matching entry and passes the matched entry on
 * `context.mount` so the extension can branch on `(selector, args)` to
 * render different UI at each site.
 */
export interface ExtensionMountPoint {
  page: string;
  selector: string;
  navLabel?: string;
  navGroup?: string;
  navGroupOrder?: number;
  navLabelOrder?: number;
  navIcon?: string;
  /**
   * Short tagline used by the portal hub picker as the dropdown subtitle.
   * Only consumed for hub-declaration mounts (single-segment `page`) — the
   * portal ignores it on sub-page mounts. Keep it under ~30 characters so
   * it fits one line in the dropdown.
   */
  navDescription?: string;
  /**
   * CSS color for the hub-picker badge background. Any value `background`
   * accepts works — hex (`#13D63D`), CSS variable (`var(--color-beam-accent)`),
   * or a gradient. Only consumed for hub-declaration mounts. When unset,
   * the portal falls back to its default accent.
   */
  navColor?: string;
  args?: Record<string, unknown>;
}

/**
 * Tone vocabulary for sidebar nav badges. Mirrors the host portal's
 * `badge-pill` color palette: `info` (cyan), `warning` (amber),
 * `error` (red), `accent` (purple).
 */
export type BadgeTone = 'info' | 'warning' | 'error' | 'accent';

/**
 * A sidebar nav badge value. `value` is a count or a short label like
 * `"LIVE"` / `"PENDING"`. Pass `null` to clear an existing badge.
 *
 * String values default to `accent` tone unless the extension supplies
 * one — count tones default to `info`.
 */
export interface BadgeValue {
  value: number | string;
  tone?: BadgeTone;
}

/**
 * Limited context handed to `getBadge`. Deliberately narrower than
 * {@link ExtensionContext} — the badge pull runs *outside* the extension's
 * mount lifecycle (no shadow DOM, no React tree, no URL params) so the
 * extension must never reach for `params` / `location` / `navigate` /
 * `mount` here.
 */
export interface BadgeContext {
  realm: string;
  cid: string;
  beam: Promise<Beam>;
  config: ExtensionConfig;
}

/**
 * Runtime context provided by the Beamable portal to every extension on mount.
 */
export interface ExtensionContext extends Map<any, any> {
  realm: string;
  cid: string;
  beam: Promise<Beam>;
  /**
   * URL params extracted by matching the extension's `mount.page` pattern
   * against the current realm-relative path. For example, an extension
   * mounted at `mount.page = "players/:playerId"` viewing
   * `/.../realms/abc/players/xyz` receives `{ playerId: "xyz" }`.
   * Empty object when the mount pattern has no params (or for nested child
   * extensions mounted via `BeamExtensionSite`).
   */
  params: Record<string, string>;
  /**
   * Read-only snapshot of the URL at mount time — see {@link ExtensionLocation}.
   */
  location: ExtensionLocation;
  /**
   * Navigate the host portal — see {@link ExtensionNavigate}.
   */
  navigate: ExtensionNavigate;
  /**
   * The specific mount entry from the manifest that triggered this mount
   * call — see {@link ExtensionMountPoint}. Read `mount.selector` or
   * `mount.args` to branch when the same bundle is bound to multiple
   * mount sites.
   */
  mount: ExtensionMountPoint;
  /**
   * Portal-wide read-only state exposed as observables —
   * see {@link ExtensionConfig}. Extensions subscribe here to react to
   * settings shared across the whole portal (active date range, theme,
   * timezone preference, logged-in account).
   */
  config: ExtensionConfig;
  /**
   * Push the extension's sidebar nav-item badge. Pass `null` to clear.
   * Only valid while this extension is mounted — the value sticks in the
   * portal's badge store after unmount until the next page load. Pair with
   * the pull-side `getBadge` (on `Portal.registerExtension`) for cases where
   * the badge must appear even when the user hasn't visited the page yet.
   */
  updateBadge: (value: BadgeValue | null) => void;
}

/**
 * Options passed to {@link Portal.registerExtension}.
 */
export interface RegisterExtensionOptions {
  /**
   * Unique name for the extension
   */
  beamId: string;

  /**
   * Called when the portal mounts this extension into the DOM.
   *
   * Render your UI into `container`. The portal guarantees that
   * `container` is attached to the document before this callback fires.
   *
   * @returns some unique instance object for the service. The same instance will be given to the unmount call later.
   * @param container - The `HTMLElement` allocated for this extension.
   * @param context   - Runtime context supplied by the portal.
   */
  onMount: (container: HTMLElement, context: ExtensionContext) => unknown | Promise<unknown>;

  /**
   * Called when the portal is about to remove this extension from the DOM.
   *
   * Use this to tear down event listeners, timers, framework instances, etc.
   * The portal waits for a returned `Promise` to settle before removing
   * `container` from the DOM.
   * @param instance - The instance object returned by the corresponding `onMount` call.
   */
  onUnmount: (instance: unknown) => void | Promise<void>;

  /**
   * Optional sidebar nav-badge supplier. Invoked once per page-load by
   * the portal when the extension's nav item first scrolls into view.
   * MUST NOT depend on the extension's React tree or shadow DOM (the
   * extension may not be mounted yet) — only read from the supplied
   * {@link BadgeContext}.
   *
   * Return `null` to clear / suppress the badge.
   *
   * For live updates while the extension is mounted, use the push channel
   * on the mount context: {@link ExtensionContext.updateBadge}.
   *
   * @example
   *   Portal.registerExtension({
   *     beamId: 'tickets',
   *     onMount(...) { ... },
   *     onUnmount(...) { ... },
   *     getBadge: async (context) => {
   *       const beam = await context.beam;
   *       const n = await beam.tickets.getUnreadCount();
   *       return n > 0 ? { value: n, tone: 'warning' } : null;
   *     },
   *   });
   */
  getBadge?: (context: BadgeContext) => Promise<BadgeValue | null>;
}

/**
 * Shape published on `window[beamId]` by {@link Portal.registerExtension}.
 * The portal reads this back to invoke mount/unmount and (separately, with
 * a narrower context) the badge pull. Exported so portal-side typings can
 * reach the same shape.
 */
export interface WindowExtensionRegistration {
  mount: (targetElement: HTMLElement, context: ExtensionContext) => unknown | Promise<unknown>;
  unmount: (instance: unknown) => void | Promise<void>;
  getBadge?: (context: BadgeContext) => Promise<BadgeValue | null>;
}


// ---------------------------------------------------------------------------
// Implementation
// ---------------------------------------------------------------------------

/**
 * Registers an extension with the Beamable portal.
 *
 * @example
 * ```ts
 * import { Portal } from '@beamable/portal-toolkit';
 *
 * Portal.registerExtension({
 *   beamId: 'MyExtension',
 *   onMount(container, context) {
 *     container.innerHTML = `<p>Hello from ${context.realm}</p>`;
 *   },
 *   onUnmount(instance) {
 *     // clean up
 *   },
 * });
 * ```
 */
function registerExtension(options: RegisterExtensionOptions): void {
  const registration: WindowExtensionRegistration = {
    mount: (targetElement, context) => options.onMount(targetElement, context),
    unmount: (instance) => options.onUnmount(instance),
    ...(options.getBadge ? { getBadge: options.getBadge } : {}),
  };
  (window as unknown as Record<string, WindowExtensionRegistration>)[options.beamId] = registration;
}

// ---------------------------------------------------------------------------
// Namespace export
// ---------------------------------------------------------------------------

/**
 * Top-level namespace for portal utilities.
 *
 * @example
 * ```ts
 * import { Portal } from '@beamable/portal-toolkit';
 * Portal.registerExtension({ ... });
 * ```
 */
export const Portal = {
  registerExtension,
} as const;
