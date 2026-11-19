// Portal extension registration utilities.
// Import via:  import { Portal } from '@beamable/portal-toolkit';

import { Beam, BeamBase, BeamZoneSdk } from "@beamable/sdk";
import type { ExtensionStorage } from "./storage";

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
 * - A path without a leading `/` is **relative to the Host** the extension is
 *   mounted in — the realm or the zone of the page. The portal prefixes it with
 *   `/<cid>/games/<gid>/realms/<rid>/` for a realm extension and with
 *   `/<cid>/zones/<zid>/` for a zone extension.
 * - A path that starts with `./` is **relative to the hub** the instance renders
 *   in ({@link BaseExtensionContext.hub}): `./player-profile` in the Social hub is
 *   `social/player-profile`. A page included by several hubs links to its own
 *   pages this way, so the user stays in the hub they are in.
 *
 * @example
 *   context.navigate('analytics/dashboard');         // Host-relative
 *   context.navigate('./player-profile');            // hub-relative
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

/** How a site lays out its mounts. Set once, in the site's declaration; code can't change it. */
export type SitePresentation = 'stack' | 'tabs' | 'route-tabs';

/**
 * A site this extension renders and offers to others: one entry of
 * `beamable.sites` in its package.json, copied by the CLI into
 * `metadata.json`. Place it on screen with `<BeamExtensionSite site="<id>">`;
 * other extensions fill it by listing the id in a mount's `siteIds`. No code
 * is generated from it: the portal resolves the id at render.
 *
 * @example
 *   "sites": [
 *     { "id": "campaign-offer", "presentation": "stack",
 *       "description": "An offer a campaign lane sends with its message." }
 *   ]
 */
export interface SiteDeclaration {
  /**
   * Names one kind of place across a Host. Use a domain name
   * (`campaign-offer`, not `offer`) so two owners don't pick one id for
   * different things. A site has no version: a change old mounts can't follow
   * goes under a new id (`campaign-offer-v2`) rendered beside the old one.
   */
  id: string;
  /**
   * - `stack`: every mount, one after another.
   * - `tabs`: one tab per mount; the active tab is kept in the component.
   * - `route-tabs`: one tab per mount; the active tab follows the URL through
   *   each mount's relative `page`. At most one per page, and the owner's own
   *   page must end in `/*`.
   *
   * A tabs or route-tabs site with a single match renders as a stack.
   */
  presentation: SitePresentation;
  /** Shown by the CLI and in every error about the site. Optional. */
  description?: string;
}

/**
 * A single mount declaration from the extension's manifest. An extension
 * can declare multiple mounts via `Properties.mounts`; the portal mounts
 * the bundle once per matching entry and passes the matched entry on
 * `context.mount` so the extension can branch on `(id, site, args)` to
 * render different UI at each site.
 */
export interface ExtensionMountPoint {
  /**
   * Names the mount within its extension. Optional in the manifest: when
   * absent the portal derives it from the first site id, plus `:` and the
   * page when there is one (`extension-page:development/config`);
   * `unplaced:<page>` for an unplaced page and `hub:<page>` for a hub. Always
   * set on `context.mount`. Declare it only when your code branches on it.
   */
  id?: string;
  /**
   * The site ids this mount fills, e.g. `["campaign-offer"]`; a placed page
   * lists `["extension-page"]`. Bare ids: no `#`, no `@`, no parent. The mount
   * shows in every instance of these sites, on any page.
   *
   * Absent (or empty) on a mount with a `page`, the mount is an **unplaced
   * page**: it has no URL and no menu entry until a hub lists it in
   * {@link include}. Never set on a hub.
   */
  siteIds?: string[];
  /**
   * For `extension-page` (and `nav-left-low`) mounts, the URL pattern
   * relative to the Host (`development/player-profile/:playerId/*`); its first
   * segment names a declared hub. For a `route-tabs` site, the tab's path
   * relative to the route where the site renders (`stats`). Not used by
   * `stack` and `tabs` sites.
   *
   * An unplaced page's page has no hub (`config`): it is joined to the hub
   * that includes it. A hub's page is the hub id, one segment.
   *
   * On `context.mount`, an included page carries its effective page, in the
   * hub it renders in (`development/config`).
   */
  page?: string;
  /**
   * `true` on a hub mount: `page` is the hub id (one segment) and the nav
   * fields (`navLabel`, `navIcon`, `navDescription`, `navColor`) its entry in
   * the hub picker. A hub exists only when some extension declares one. Never
   * together with `siteIds`. The hub's mount renders at the hub root.
   */
  hub?: boolean;
  /**
   * Hub mounts only: the unplaced pages this hub shows, in order. On any other
   * mount it is ignored with a dev warning. See {@link HubInclude}.
   */
  include?: HubInclude[];
  /**
   * Set by the portal on `context.mount` and {@link CandidateMetadata.mount}:
   * the site id this instance renders in. Never written in a manifest.
   */
  site?: string;
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
  /**
   * Conditional visibility. Each key is a dot-path into the signed-in account
   * (e.g. `"account.role"`), each value a regex alternation
   * (`"developer|admin"`); the mount shows only when every filter matches.
   */
  filters?: Record<string, string>;
}

/**
 * One entry of a hub's {@link ExtensionMountPoint.include}: the unplaced pages
 * at or under `path`, by whole segments (`"player-profile"` brings the list and
 * `player-profile/:playerId/*`; `"*"` brings every one). A string is `{ path }`.
 * The object form relabels or regroups the page in this hub: `label`
 * (`navLabel`), `navGroup` and `order` (`navLabelOrder`); the icon always comes
 * from the page.
 *
 * Precedence: a page placed explicitly at the same URL wins over the include,
 * and between two includes of one hub the first wins.
 */
export type HubInclude = string | { path: string; label?: string; navGroup?: string; order?: number };

/**
 * A handle to one of this extension's own sites, accepted by
 * {@link ExtensionContext.getMountSiteCandidates}: either the site id, or the
 * site element itself (the `<BeamExtensionSite>`'s DOM element, e.g. a React
 * ref's `.current`).
 */
export type MountSiteHandle = string | HTMLElement;

/**
 * What the portal knows about an extension that could mount at a site.
 * Returned by {@link ExtensionContext.getMountSiteCandidates} and passed to a
 * `<BeamExtensionSite>`'s `resolve` function.
 */
export interface CandidateMetadata {
  /** The candidate extension's name / beam id. */
  beamId: string;
  /** The mount entry that matched this site: `id`, `site`, `siteIds`, `args`, nav*, … */
  mount: ExtensionMountPoint;
  /** The `@beamable/portal-toolkit` version the candidate was built against. */
  toolkitVersion: string;
  /** `true` for a locally-running dev build, `false` for a deployed one. */
  isLocal: boolean;
  /** The site ids this candidate itself declares in `beamable.sites`. */
  sites: string[];
}

/**
 * The runtime context the portal hands every extension on mount, parametrized over the
 * SDK type. The realm and zone variants ({@link ExtensionContext} /
 * {@link ZoneExtensionContext}) add their scope-specific identity field (`realm` / `zid`)
 * and bind `beam` to the matching SDK; everything else is shared.
 */
export interface BaseExtensionContext<TBeam> extends Map<any, any> {
  /**
   * The scope of the Host this extension is mounted in: `'realm'` for a realm extension
   * ({@link ExtensionContext}), `'zone'` for a zone extension ({@link ZoneExtensionContext}).
   * Narrow on it to reach `realm` or `zid`.
   */
  scope: 'realm' | 'zone';
  cid: string;
  beam: Promise<TBeam>;
  /**
   * URL params extracted by matching the extension's `mount.page` pattern
   * against the current path relative to the Host (the realm or zone of the
   * page). For example, an extension mounted at `mount.page = "players/:playerId"`
   * viewing `/.../realms/abc/players/xyz` (or `/.../zones/euc1/players/xyz`)
   * receives `{ playerId: "xyz" }`.
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
   * The hub this instance renders in: the first segment of its effective
   * page (`social` for `social/player-profile`), or of the current path for a
   * mount with no page of its own. A page included by two hubs gets each hub's
   * id in its own instance. `context.navigate('./…')` resolves against it.
   */
  hub: string;
  /**
   * The specific mount entry from the manifest that triggered this mount
   * call — see {@link ExtensionMountPoint}. `mount.id` (always set) and
   * `mount.site` tell the instances apart when the same bundle fills several
   * sites or pages. An included page's `mount.page` is its effective page, in
   * the hub it renders in.
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
   * Data supplied by the parent that mounted this extension, if any. Value
   * comes from the `siteData` prop on the parent's `<BeamExtensionSite>` or
   * `<BeamChildExtension>`. Snapshot at mount time — mutating the parent's
   * value after mount does NOT update this field for existing mounts (a
   * remount picks up the new value). For live-in-sync scenarios, the parent
   * should pass a store (writable/readable) through `siteData` and the
   * child subscribes to it.
   *
   * Typed as `unknown` — the mount site is a generic primitive and the
   * type system can't know a given site's contract. Validate at the receive
   * site with a companion type, a schema, or a defensive cast.
   *
   * Top-level (rather than nested under `mount`) because the value is
   * supplied by the parent, not by this extension's own manifest.
   */
  siteData?: unknown;
  /**
   * Per-extension persistent storage across two tiers (`session`, `local`) —
   * see {@link ExtensionStorage}. Every value is isolated by this extension
   * and the signed-in account; the author picks a `scope` (`pid`/`cid`) and
   * `mount` policy (`all`/`instance`), and may attach a TTL.
   *
   * @example
   *   // per-realm, this device, survives reloads:
   *   await context.storage.local.set('lastFilter', filter);
   *   const filter = await context.storage.local.get<Filter>('lastFilter');
   *
   *   // one value for the whole org, on this device:
   *   await context.storage.local.scope({ scope: 'cid' }).set('compact', true);
   */
  storage: ExtensionStorage;
  /**
   * Discover the extensions that could mount at one of THIS extension's own
   * sites: pass the site id (or the site element) and get a
   * reactive list of candidates with their metadata. Use it to build UI (e.g.
   * a "which view" dropdown) or to compute an `include` list for the matching
   * `<BeamExtensionSite>`. Reactive: `subscribe` to react as extensions
   * deploy, enable, or disable. See {@link CandidateMetadata}.
   *
   * @example
   *   const obs = context.getMountSiteCandidates('campaign-offer');
   *   const ids = obs.get().map((c) => c.beamId);   // e.g. to seed `include`
   */
  getMountSiteCandidates(site: MountSiteHandle): ExtensionObservable<CandidateMetadata[]>;
}

/**
 * Runtime context provided by the Beamable portal to a realm-scoped extension on mount.
 * Carries the current `realm` (pid) and a full {@link Beam} client SDK.
 */
export interface ExtensionContext extends BaseExtensionContext<Beam> {
  scope: 'realm';
  /** The current realm (pid) this extension is mounted under. */
  realm: string;
}

/**
 * Runtime context provided by the Beamable portal to a zone-scoped extension on mount.
 *
 * @remarks
 * A zone runs *above* realms, so there is no `realm` here — instead the extension gets
 * the `zid` it is scoped to and a {@link BeamZoneSdk} (customer realm/zone directory
 * queries only; no player/realm surface). To act within a realm, resolve a pid from
 * `beam.customer.getRealms()` and start a realm session with `Beam.init({ cid, pid })`.
 */
export interface ZoneExtensionContext extends BaseExtensionContext<BeamZoneSdk> {
  scope: 'zone';
  /** The zone (zid) this extension is mounted under. */
  zid: string;
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

/**
 * Options passed to {@link Portal.registerZoneExtension}. The zone-scoped analog of
 * {@link RegisterExtensionOptions}: identical shape, but `onMount` receives
 * the zone context ({@link ZoneExtensionContext}) and thus a
 * {@link BeamZoneSdk} instead of a full {@link Beam}.
 */
export interface RegisterZoneExtensionOptions {
  /** Unique name for the extension */
  beamId: string;
  /** Called when the portal mounts this zone extension into the DOM. */
  onMount: (container: HTMLElement, context: ZoneExtensionContext) => unknown | Promise<unknown>;
  /** Called when the portal is about to remove this extension from the DOM. */
  onUnmount: (instance: unknown) => void | Promise<void>;
}

/**
 * Shared shape published on `window[beamId]` by the register* helpers, parametrized over
 * the mount context type. The portal reads this back to invoke mount/unmount.
 */
export interface BaseWindowExtensionRegistration<TCtx> {
  /**
   * Discriminates which SDK/context the portal must build for this extension: `realm`
   * gets an {@link ExtensionContext} (full {@link Beam}); `zone` gets a
   * {@link ZoneExtensionContext} ({@link BeamZoneSdk}). The portal uses this to route the
   * extension to a compatible mount site and reject a mismatch.
   */
  scope: 'realm' | 'zone';
  mount: (targetElement: HTMLElement, context: TCtx) => unknown | Promise<unknown>;
  unmount: (instance: unknown) => void | Promise<void>;
}

/** Registration published by {@link Portal.registerExtension} (realm-scoped). */
export interface RealmWindowExtensionRegistration
  extends BaseWindowExtensionRegistration<ExtensionContext> {
  scope: 'realm';
}

/** Registration published by {@link Portal.registerZoneExtension} (zone-scoped). */
export interface ZoneWindowExtensionRegistration
  extends BaseWindowExtensionRegistration<ZoneExtensionContext> {
  scope: 'zone';
}

/**
 * Shape published on `window[beamId]`. A discriminated union on {@link
 * BaseWindowExtensionRegistration.scope} — narrow on `scope` to get the matching context
 * types. Exported so portal-side typings can reach the same shape.
 */
export type WindowExtensionRegistration =
  | RealmWindowExtensionRegistration
  | ZoneWindowExtensionRegistration;


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
  const registration: RealmWindowExtensionRegistration = {
    scope: 'realm',
    mount: (targetElement, context) => options.onMount(targetElement, context),
    unmount: (instance) => options.onUnmount(instance),
  };
  (window as unknown as Record<string, WindowExtensionRegistration>)[options.beamId] = registration;
}

/**
 * Registers a **zone-scoped** extension with the Beamable portal. The zone analog of
 * {@link registerExtension}: the mount context is a {@link ZoneExtensionContext}, so
 * `context.beam` is a {@link BeamZoneSdk} (customer realm/zone directory only) and there
 * is no `realm` — use `context.zid`.
 *
 * @example
 * ```ts
 * import { Portal } from '@beamable/portal-toolkit';
 *
 * Portal.registerZoneExtension({
 *   beamId: 'ZoneOverview',
 *   async onMount(container, context) {
 *     const beam = await context.beam;
 *     const realms = await beam.customer.getRealms();
 *     container.innerHTML = `<p>Zone ${context.zid}: ${realms.length} realms</p>`;
 *   },
 *   onUnmount(instance) {
 *     // clean up
 *   },
 * });
 * ```
 */
function registerZoneExtension(options: RegisterZoneExtensionOptions): void {
  const registration: ZoneWindowExtensionRegistration = {
    scope: 'zone',
    mount: (targetElement, context) => options.onMount(targetElement, context),
    unmount: (instance) => options.onUnmount(instance),
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
  registerZoneExtension,
} as const;
