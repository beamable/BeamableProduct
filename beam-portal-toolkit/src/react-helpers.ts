// React-flavored helpers for Beamable portal extensions.
//
// These wrap the two repetitive bits every React extension does today:
//   1) Bootstrap a createRoot inside the host's mount callback and
//      hand the root back to the unmount callback. ~12 lines of
//      boilerplate every extension ships verbatim.
//   2) Resolve the `context.beam` promise into a usable Beam instance
//      and re-render once it lands. Bare `useState` + `useEffect` works
//      but every extension was inventing it (and several were storing
//      `beam` in state then immediately ignoring it).
//
// Import via:  import { registerReactExtension, useBeam } from '@beamable/portal-toolkit/react-helpers';

import { createElement, useCallback, useEffect, useMemo, useRef, useState, StrictMode, type ComponentType, type ReactNode } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { Portal, type BadgeContext, type BadgeValue, type ExtensionContext } from './portal';
import type { Beam } from '@beamable/sdk';

// ---------------------------------------------------------------------------
// useBeam
// ---------------------------------------------------------------------------

/**
 * Resolves `context.beam` into the Beam SDK instance and re-renders the
 * caller once it lands. Returns `null` until the SDK is ready.
 *
 * The promise from `context.beam` is portal-supplied: it settles once
 * authentication, CID/PID resolution, and SDK construction are done.
 * On the host's side this is typically already resolved by the time
 * the extension mounts, but the hook accommodates the slow path too.
 *
 * @example
 *   export default function App({ context }: { context: ExtensionContext }) {
 *     const beam = useBeam(context);
 *     if (!beam) return <BeamSpinner />;
 *     // ...use beam.api / beam.player / etc.
 *   }
 */
export function useBeam(context: ExtensionContext): Beam | null {
  const [beam, setBeam] = useState<Beam | null>(null);
  useEffect(() => {
    let cancelled = false;
    context.beam.then((b) => {
      if (!cancelled) setBeam(b);
    });
    return () => {
      cancelled = true;
    };
    // Depend on the promise specifically, not the whole `context`
    // object — host implementations may rebuild `context` on every
    // render but keep the promise stable.
  }, [context.beam]);
  return beam;
}

// ---------------------------------------------------------------------------
// registerReactExtension
// ---------------------------------------------------------------------------

/**
 * Options for {@link registerReactExtension}.
 */
export interface RegisterReactExtensionOptions {
  /** Unique extension name — must match the `beamId` in your manifest. */
  beamId: string;
  /**
   * Your top-level React component. Receives `{ context }` as its only
   * prop. The component is wrapped in `<StrictMode>` automatically.
   */
  App: ComponentType<{ context: ExtensionContext }>;
  /**
   * Disable React StrictMode if your component genuinely can't tolerate
   * double-invocation (rare — usually a sign the component should be
   * fixed, not the strict mode disabled). Defaults to `false`.
   */
  disableStrictMode?: boolean;
  /**
   * Optional wrapper rendered around the App. Use for providers
   * (theme/router/query client/etc.). Receives `children` and the
   * `context` as props.
   */
  wrapper?: (props: { context: ExtensionContext; children: ReactNode }) => ReactNode;
  /**
   * Optional sidebar nav-badge supplier — forwarded straight to
   * {@link Portal.registerExtension}. See its docs for the contract:
   * runs once per page-load when the nav item is in view, MUST NOT
   * depend on the extension's React tree (the extension may not be
   * mounted yet), pair with {@link ExtensionContext.updateBadge} for
   * live updates while mounted.
   */
  getBadge?: (context: BadgeContext) => Promise<BadgeValue | null>;
}

/**
 * One-line React extension bootstrap. Equivalent to:
 *
 *   Portal.registerExtension({
 *     beamId,
 *     onMount(container, context) {
 *       const root = createRoot(container);
 *       root.render(<StrictMode><App context={context} /></StrictMode>);
 *       return root;
 *     },
 *     onUnmount(instance) {
 *       (instance as Root).unmount();
 *     },
 *   });
 *
 * but with the boilerplate captured once.
 *
 * @example
 *   import { registerReactExtension } from '@beamable/portal-toolkit/react-helpers';
 *   import App from './App';
 *   registerReactExtension({ beamId: 'my-extension', App });
 */
export function registerReactExtension(options: RegisterReactExtensionOptions): void {
  const { beamId, App, disableStrictMode, wrapper, getBadge } = options;
  Portal.registerExtension({
    beamId,
    onMount: (container, context) => {
      const root = createRoot(container);
      const appEl = createElement(App, { context });
      const body = wrapper ? wrapper({ context, children: appEl }) : appEl;
      root.render(disableStrictMode ? body : createElement(StrictMode, null, body));
      return root;
    },
    onUnmount: (instance) => {
      (instance as Root).unmount();
    },
    ...(getBadge ? { getBadge } : {}),
  });
}

// ---------------------------------------------------------------------------
// useChangeTracker
// ---------------------------------------------------------------------------

/**
 * One validation error against a tracked item. `field` is consumer-defined
 * (e.g. `"key"` or `"value"` for a config-style key/value editor) so the
 * paired `<BeamChangeBar>` can surface errors per cell.
 */
export interface ChangeTrackerValidationError {
  key: string;
  field: string;
  message: string;
}

/**
 * Computed diff between the original baseline and the current draft.
 * Feed the shape this exposes into `<BeamChangeBar changes={…} />` after
 * projecting to the bar's `BeamChangeSet` payload (the bar is a pure
 * renderer; it doesn't know about your row shape).
 */
export interface ChangeTrackerChangeSet<T> {
  added: T[];
  modified: { key: string; original: T; current: T }[];
  deleted: T[];
  /** Human-readable summary like "2 added, 1 modified". */
  summary: string;
}

export interface ChangeTracker<T> {
  /** The current edited list (baseline + pending changes). */
  draft: T[];
  /** True when any add/modify/delete is pending. */
  isDirty: boolean;
  /** Per-bucket diff against the baseline. */
  changes: ChangeTrackerChangeSet<T>;
  /** Flattened validation errors across the entire draft. */
  errors: ChangeTrackerValidationError[];
  /** True when `errors` is empty — gate Save on this. */
  isValid: boolean;
  /** Errors for a single item (by key). */
  getErrors: (key: string) => ChangeTrackerValidationError[];
  /** Replace an item in the draft. The updater receives the current value. */
  updateItem: (key: string, updater: (item: T) => T) => void;
  /** Append an item to the draft. */
  addItem: (item: T) => void;
  /** Remove an item from the draft (does not touch the baseline). */
  deleteItem: (key: string) => void;
  /** Reset the draft to the current baseline. */
  discard: () => void;
  /** Replace the baseline (and reset the draft to it). Call after a save. */
  applyOriginal: (newOriginal: T[]) => void;
}

/**
 * Tracks the diff between a server-side baseline and a locally edited
 * draft, plus validation errors. Pairs with `<BeamChangeBar>` to drive
 * the typical "edit a table of rows, batch save" pattern (realm config,
 * player stats, leaderboard rows, etc.).
 *
 * @example
 *   interface Row { key: string; value: string }
 *   const tracker = useChangeTracker<Row>({
 *     original: rowsFromServer,
 *     getKey: r => r.key,
 *     isEqual: (a, b) => a.value === b.value,
 *     validate: row => row.value ? [] : [{ key: row.key, field: 'value', message: 'required' }],
 *   });
 *   // tracker.draft → render rows
 *   // tracker.updateItem(key, r => ({ ...r, value: newValue })) → edit
 *   // tracker.changes  → project to BeamChangeSet for <BeamChangeBar>
 *   // tracker.applyOriginal(freshFromServer) after a successful save
 */
export function useChangeTracker<T>(options: {
  /** Server-side baseline. Replacing this resets the draft to the new baseline. */
  original: T[];
  /** Stable identity per item. Used for diff matching and as React row key. */
  getKey: (item: T) => string;
  /** Equality check for "is this item modified?". Defaults to deep JSON compare. */
  isEqual?: (a: T, b: T) => boolean;
  /** Optional per-item validator. Errors are merged into `tracker.errors`. */
  validate?: (item: T, allItems: T[]) => ChangeTrackerValidationError[];
}): ChangeTracker<T> {
  const { original, getKey, isEqual, validate } = options;
  const equalFn = isEqual ?? ((a: T, b: T) => JSON.stringify(a) === JSON.stringify(b));

  const [baseline, setBaseline] = useState<T[]>(original);
  const [draft, setDraft] = useState<T[]>(() => structuredClone(original));

  // Mid-render baseline sync. When the parent passes a fresh `original`
  // (e.g. after a successful save → re-fetch), reset draft to it before
  // the next render so the children don't briefly see stale state.
  const prevOriginalRef = useRef(original);
  if (original !== prevOriginalRef.current) {
    prevOriginalRef.current = original;
    setBaseline(original);
    setDraft(structuredClone(original));
  }

  const changes = useMemo<ChangeTrackerChangeSet<T>>(() => {
    const baseMap = new Map<string, T>();
    for (const item of baseline) baseMap.set(getKey(item), item);

    const draftMap = new Map<string, T>();
    for (const item of draft) draftMap.set(getKey(item), item);

    const added: T[] = [];
    const modified: { key: string; original: T; current: T }[] = [];
    const deleted: T[] = [];

    for (const [key, current] of draftMap) {
      const orig = baseMap.get(key);
      if (!orig) added.push(current);
      else if (!equalFn(orig, current)) modified.push({ key, original: orig, current });
    }
    for (const [key, orig] of baseMap) {
      if (!draftMap.has(key)) deleted.push(orig);
    }

    const parts: string[] = [];
    if (added.length) parts.push(`${added.length} added`);
    if (modified.length) parts.push(`${modified.length} modified`);
    if (deleted.length) parts.push(`${deleted.length} deleted`);
    const summary = parts.join(', ') || 'No changes';

    return { added, modified, deleted, summary };
  }, [baseline, draft, getKey, equalFn]);

  const errors = useMemo<ChangeTrackerValidationError[]>(() => {
    if (!validate) return [];
    const out: ChangeTrackerValidationError[] = [];
    for (const item of draft) out.push(...validate(item, draft));
    return out;
  }, [draft, validate]);

  const isDirty = changes.added.length > 0 || changes.modified.length > 0 || changes.deleted.length > 0;
  const isValid = errors.length === 0;

  const getErrors = useCallback(
    (key: string) => errors.filter(e => e.key === key),
    [errors],
  );

  const updateItem = useCallback((key: string, updater: (item: T) => T) => {
    setDraft(prev => prev.map(item => (getKey(item) === key ? updater(item) : item)));
  }, [getKey]);

  const addItem = useCallback((item: T) => {
    setDraft(prev => [...prev, item]);
  }, []);

  const deleteItem = useCallback((key: string) => {
    setDraft(prev => prev.filter(item => getKey(item) !== key));
  }, [getKey]);

  const discard = useCallback(() => {
    setDraft(structuredClone(baseline));
  }, [baseline]);

  const applyOriginal = useCallback((newOriginal: T[]) => {
    setBaseline(newOriginal);
    setDraft(structuredClone(newOriginal));
  }, []);

  return { draft, isDirty, changes, errors, isValid, getErrors, updateItem, addItem, deleteItem, discard, applyOriginal };
}
