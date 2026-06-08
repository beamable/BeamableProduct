// Hand-written React forwarders for components whose React surface differs
// substantially from the underlying web component's CEM-derived API.
// Auto-generated forwarders live in `react-components.ts`.
//
// At the moment that's just `BeamTable<T>` — the React wrapper accepts
// `<BeamColumn>` / `<BeamTopRow>` / `<BeamGroupHeader>` markers as children
// and exposes a generic row type, neither of which the CEM models.

import { createElement, type ComponentType, type CSSProperties, type ReactElement, type ReactNode, type Ref } from 'react';
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
}

export function BeamExtensionSite(
  props: BeamExtensionSiteProps & { ref?: Ref<HTMLElement> },
): ReactElement {
  return createElement(hostComponent('BeamExtensionSite'), props);
}
BeamExtensionSite.displayName = 'BeamExtensionSite';
