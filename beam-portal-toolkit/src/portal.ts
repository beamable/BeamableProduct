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
  args?: Record<string, unknown>;
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

  (window as unknown as Record<string, unknown>)[options.beamId] = {
    mount: (targetElement: HTMLElement, context: ExtensionContext) => {
      return options.onMount(targetElement, context);
    },
    unmount: (instance: unknown) => {
      return options.onUnmount(instance);
    },
  };

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
