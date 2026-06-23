// Marker components for BeamTable. Hand-written, NOT auto-generated.
//
// Marker components are pure-prop carriers that always return `null`. Their
// parent (`BeamTable`) walks JSX children at render time, reads the props off
// each marker, and bridges them to the underlying `<beam-table>` element.
//
// These can't be forwarders pointing at `window.__beamPortal.react.*` because
// `BeamTable` identifies them via a `displayName` comparison, and the React
// element type still has to be the function React rendered. Shipping the
// markers as real (tiny) components in the toolkit lets BeamTable on the host
// recognize them by `displayName` regardless of which side wrote the JSX.
//
// All Beam markers set `displayName` to a unique `Beam*` string. The host
// uses `(child.type as any)?.displayName === 'Beam<Name>'` to identify them.

import type { ReactNode, ComponentType } from 'react';

// ── BeamColumn — column definition for BeamTable ─────────────────────────

export type BeamColumnRenderer<T> = (row: T, rowIndex: number) => ReactNode;

export interface BeamColumnProps<T = unknown> {
  /** Object key on the row (also used as the column key for sorting). */
  field?: (keyof T & string) | string;
  header?: string;
  sortable?: boolean;
  width?: string;
  align?: 'left' | 'center' | 'right';

  /** Pure-string formatter — fast path; no React subtree per cell. */
  format?: (value: unknown, row: T) => string;

  /** React component that receives `{ row, rowIndex }` as props. */
  as?: ComponentType<{ row: T; rowIndex: number }>;

  /** Render-as-children — `(row, rowIndex) => ReactNode`. */
  children?: BeamColumnRenderer<T>;

  /** Plugin escape hatch — name of a registered custom element. */
  cellTag?: string;
}

export function BeamColumn<T = unknown>(_props: BeamColumnProps<T>): null {
  return null;
}
BeamColumn.displayName = 'BeamColumn';

// ── BeamTopRow — single-slot marker above the data rows ──────────────────

export interface BeamTopRowProps {
  children: () => ReactNode;
}

export function BeamTopRow(_props: BeamTopRowProps): null {
  return null;
}
BeamTopRow.displayName = 'BeamTopRow';

// ── BeamGroupHeader — per-group header content marker ────────────────────

export interface BeamTableGroup {
  groupKey: string;
  rows: unknown[];
  /** Total rows in this group across all pages. */
  total: number;
}

export interface BeamGroupHeaderProps {
  children: (group: BeamTableGroup) => ReactNode;
}

export function BeamGroupHeader(_props: BeamGroupHeaderProps): null {
  return null;
}
BeamGroupHeader.displayName = 'BeamGroupHeader';
