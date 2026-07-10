// Hand-written React forwarders for components whose React surface differs
// substantially from the underlying web component's CEM-derived API.
// Auto-generated forwarders live in `react-components.ts`.
//
// Currently:
//   - `BeamTable<T>` — accepts `<BeamColumn>` / `<BeamTopRow>` /
//     `<BeamGroupHeader>` markers as children and exposes a generic row
//     type, neither of which the CEM models.
//   - `BeamChangeBar` — the codegen pulls only from CEM attributes, but
//     this component's primary data inputs (`changes`, `errors`) are Lit
//     `@property({ attribute: false })` fields and its outputs are custom
//     events (`wa-save`, `wa-discard`). The hand-written forwarder surfaces
//     both as typed React props.

import { createElement, type CSSProperties, type ComponentType, type ReactElement, type ReactNode, type Ref } from 'react';
import { hostComponent } from './react-host';

export interface BeamTableProps<T = unknown> {
  data: T[];
  pageSize?: number;
  pageSizeOptions?: number[];
  rowKey?: (row: T, index: number) => string | number;
  groupBy?: string;
  /** Pin these group keys to the top of the table, in this order. */
  groupOrder?: string[];
  emptyMessage?: string;
  loading?: boolean;
  loadingMessage?: string;
  defaultSort?: { key: string; direction: 'asc' | 'desc' };
  defaultCollapsed?: boolean;
  card?: boolean;
  tableTitle?: string;
  className?: string;
  style?: CSSProperties;

  onRowClick?: (row: T, index: number) => void;
  onSortChange?: (sort: { key: string; direction: 'asc' | 'desc' | null }) => void;
  onPageChange?: (page: number) => void;
  onGroupAction?: (groupKey: string) => void;

  /** `<BeamColumn>` / `<BeamTopRow>` / `<BeamGroupHeader>` marker children. */
  children?: ReactNode;
}

// Plain function component (not `forwardRef`). React 19 supports `ref` as a
// regular prop, and the generic `<T>` survives the dts bundler this way —
// `forwardRef`'s `ForwardRefExoticComponent<…>` gets erased to `any` by the
// bundler we use.
export function BeamTable<T = unknown>(
  props: BeamTableProps<T> & { ref?: Ref<HTMLElement> },
): ReactElement {
  return createElement(hostComponent('BeamTable'), props);
}
BeamTable.displayName = 'BeamTable';

// Re-export prop types under their canonical names so authors can write
// `BeamColumnProps<T>` without reaching into `react-markers`.
export type { BeamColumnProps, BeamColumnRenderer, BeamTopRowProps, BeamGroupHeaderProps, BeamTableGroup } from './react-markers';
export { BeamColumn, BeamTopRow, BeamGroupHeader } from './react-markers';

// Used by BeamTable when an `as` prop forwards a row cell to a typed React
// component. Re-exported here so authors can write `BeamCellProps<MyRow>`.
export interface BeamCellProps<T = unknown> {
  row: T;
  rowIndex: number;
}
export type BeamCellComponent<T = unknown> = ComponentType<BeamCellProps<T>>;

// ---------------------------------------------------------------------------
// BeamChildExtension — embed another portal extension inside an extension.
// ---------------------------------------------------------------------------

export interface BeamChildExtensionProps {
  extensionName: string;
  /**
   * Arbitrary in-process data to hand to the embedded extension. Shows up
   * on the child's `context.siteData` as a snapshot at mount time.
   *
   * Use for one-shot parent→child handoff (a resolved id, a computed
   * value). For live-in-sync scenarios, pass a store (`writable<T>`) —
   * the child subscribes to it in its own React tree.
   *
   * Referentially unstable values (fresh object literal per render) cause
   * new mounts to see a fresh snapshot; existing mounted children keep
   * their own. `useMemo` the value if that's not what you want.
   */
  siteData?: unknown;
}

export function BeamChildExtension(
  props: BeamChildExtensionProps & { ref?: Ref<HTMLElement> },
): ReactElement {
  return createElement(hostComponent('BeamChildExtension'), props);
}
BeamChildExtension.displayName = 'BeamChildExtension';

// ---------------------------------------------------------------------------
// BeamExtensionSite — declare a mount site inside this extension that other
// extensions can target. Matched children are paired by name (the parent
// extension's manifest `Name`) and the provided `selector`.
// ---------------------------------------------------------------------------

export interface BeamExtensionSiteProps {
  /**
   * The site identifier child extensions target. Children whose manifest
   * mount declares `selector: "#<this value>"` render here.
   */
  selector: string;
  /**
   * How multiple matching children render at this site.
   *
   * - `additive` (default): stack them, all mounted simultaneously.
   *   Children match by parent name: `mount.page = "<parent's manifest Name>"`.
   * - `tabs`: render in a `beam-tab-group`; tab selection is internal
   *   state. Children match by parent name (same as `additive`). Best for
   *   ephemeral tab UIs (settings dialogs, etc.) where deep-linking
   *   doesn't matter.
   * - `tabs-route`: render in a `beam-tab-group`; the **active tab** is
   *   whichever child's `mount.page` matches the current URL. Children
   *   declare full URL patterns (e.g. `"players/list/:playerId/inventory"`),
   *   not parent-name references. Tab clicks fire a portal navigate to
   *   the clicked child's URL, with the parent's matched params filled
   *   in. Use this for page-level tabbed views (Player 360, microservice
   *   detail, anything that's "a thing with deep-linkable views").
   *
   * `tabs` and `tabs-route` both require ≥2 matching children — a single
   * match falls back to additive so the tab strip is never one tab wide.
   */
  mountKind?: 'additive' | 'tabs' | 'tabs-route';
  /**
   * Arbitrary in-process data to hand to every child extension mounted at
   * this site. Shows up on the child's `context.siteData` as a snapshot at
   * mount time — mutating the value after a child has mounted does NOT
   * update that child; a remount picks up the new value.
   *
   * Typed as `unknown` — the mount site is a generic primitive and can't
   * know a specific child's contract. Publish a companion type or schema
   * if you want consumers to validate.
   *
   * For live-in-sync scenarios, pass a store (`writable<T>`) through
   * `siteData` and the child subscribes to it. No new observable API is
   * required — the store IS the observability boundary.
   *
   * Referentially unstable values (fresh object literal per render) cause
   * new mounts to see a fresh snapshot; existing mounted children keep
   * their own. `useMemo` the value if that's not what you want.
   */
  siteData?: unknown;
}

export function BeamExtensionSite(
  props: BeamExtensionSiteProps & { ref?: Ref<HTMLElement> },
): ReactElement {
  return createElement(hostComponent('BeamExtensionSite'), props);
}
BeamExtensionSite.displayName = 'BeamExtensionSite';

// ---------------------------------------------------------------------------
// BeamChangeBar — sticky change bar with typed `changes` / `errors` props
// and `onWaSave` / `onWaDiscard` event handlers.
// ---------------------------------------------------------------------------

/**
 * One pending edit shown in the change bar / review dialog. Labels and
 * values are pre-computed by the consumer — the component is a pure
 * renderer. Use `labelPrefix` for a muted lead-in (e.g. a namespace) that
 * should be visually de-emphasized but stay in line with the label.
 */
export interface BeamChangeEntry {
  /** Stable identity for the item. Used to match validation errors and as a render key. */
  key: string;
  /** Primary display label (plain text). */
  label: string;
  /** Optional muted prefix rendered before the label (e.g. a namespace). */
  labelPrefix?: string;
  /** Current/new value (added & modified). */
  value?: string;
  /** Original value (modified only). */
  previousValue?: string;
}

/** Full payload consumed by `BeamChangeBar.changes`. */
export interface BeamChangeSet {
  added?: BeamChangeEntry[];
  modified?: BeamChangeEntry[];
  deleted?: BeamChangeEntry[];
  /** Optional human summary. When omitted, the component derives one. */
  summary?: string;
}

/** Validation error shown inline next to its owning entry. */
export interface BeamChangeBarError {
  key: string;
  field?: string;
  message: string;
}

export interface BeamChangeBarProps {
  /** Pending edits to display + (optionally) summarize. */
  changes?: BeamChangeSet;
  /** Validation errors paired with `changes[*].key`. Save is gated while present. */
  errors?: BeamChangeBarError[];
  /** Show the Save button as "Saving…" and disable it while a save is in flight. @default false */
  saving?: boolean;
  /** Override the auto-derived summary text (e.g. "2 added, 1 modified"). */
  'summary-text'?: string;
  /** Label of the Save button. @default 'Save' */
  'save-label'?: string;
  /** Label of the Save button while saving. @default 'Saving...' */
  'saving-label'?: string;
  /** Label of the Discard button. @default 'Discard' */
  'discard-label'?: string;
  /** Label of the Review button. @default 'Review' */
  'review-label'?: string;
  /** Title of the review dialog. @default 'Review Changes' */
  'review-title'?: string;
  /** Hide the Review button + dialog entirely. @default false */
  'no-review'?: boolean;
  /**
   * Render the bar inline at the host's normal flow position instead of
   * fixed to the bottom of the viewport. Useful for in-page docs/previews
   * and for embedding the bar inside a settings panel. @default false
   */
  inline?: boolean;
  /**
   * Auto-save mode. When enabled, the bar dispatches `wa-save` automatically
   * (debounced by `auto-save-debounce`) whenever `changes` is dirty AND
   * `errors` is empty, and the visible bar UI is hidden. If validation
   * errors appear, the bar falls back to its normal interactive state so
   * the user can fix them. The consumer still owns the actual save logic
   * via the `wa-save` handler — the bar only schedules the dispatch.
   * @default false
   */
  'auto-save'?: boolean;
  /**
   * Debounce window in milliseconds before auto-save dispatches `wa-save`.
   * Resets every time `changes` updates so rapid edits collapse into one
   * save. @default 800
   */
  'auto-save-debounce'?: number;

  /** Fired when the user clicks Save (bar or dialog footer), or auto-save fires. */
  onWaSave?: (event: CustomEvent) => void;
  /** Fired when the user clicks Discard. */
  onWaDiscard?: (event: CustomEvent) => void;
  /** Fired when the review dialog opens. */
  onWaReviewShow?: (event: CustomEvent) => void;
  /** Fired when the review dialog closes for any reason. */
  onWaReviewHide?: (event: CustomEvent) => void;
  /** Fired when the bar transitions between high-level states. */
  onWaStateChange?: (event: CustomEvent) => void;

  className?: string;
  style?: CSSProperties;
}

/** React forwarder for `<beam-change-bar>`. */
export function BeamChangeBar(
  props: BeamChangeBarProps & { ref?: Ref<HTMLElement> },
): ReactElement {
  return createElement(hostComponent('BeamChangeBar'), props);
}
BeamChangeBar.displayName = 'BeamChangeBar';
