// Entry for `@beamable/portal-toolkit/react`.
//
// Re-exports every Beam React component the toolkit knows about:
//
//   - Auto-generated forwarders for non-bindable `beam-*` web components
//     (codegen output in `generated/react-components.ts`). These delegate to
//     the host portal's real components at render time.
//   - Auto-generated forwarders for bindable input-like components
//     (`generated/react-bindable.ts`) — same shape plus typed
//     `onValueChange` / `onCheckedChange` callbacks.
//   - Hand-written wrappers for components whose React surface differs from
//     the underlying web component (currently only `BeamTable`).
//   - Hand-written marker components (`BeamColumn`, `BeamTopRow`,
//     `BeamGroupHeader`). These ship as real (tiny) code so identity-based
//     children walks in the host can recognize them by `displayName`.

export * from './generated/react-components';
export * from './generated/react-bindable';
export * from './generated/react-custom';
// React-flavored runtime helpers: `useBeam`, `registerReactExtension`.
// Co-located with the components so extension authors get them from a
// single import path instead of remembering a separate subpath.
export * from './react-helpers';
