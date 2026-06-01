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
