# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.5.0] - 2026-08-04

### Added
- `beam-tab` gains `with-remove`, which shows a remove button and emits `wa-remove`. Web Awesome 3
  dropped the closable-tab feature Shoelace had, so a tab strip whose items can be deleted had
  nowhere to put the affordance. Same vocabulary as `beam-tag`'s `with-remove` / `wa-remove`.
- `beam-tag-input` — a text field that commits what you type into removable pills, with a real
  `string[]` value. Replaces the comma-separated-string pattern, where re-splitting the value on
  every keystroke makes the separator itself impossible to type.
- `beam-date-picker` — a calendar (and optional time) picker. The value format is identical to a
  native `datetime-local` / `date` input, so it is a drop-in replacement for one.

### Fixed
- **The five chart components (`BeamLineChart`, `BeamBarChart`, `BeamDonutChart`, `BeamFunnelChart`,
  `BeamSankeyChart`) are exported again.** They have hand-written forwarders in `react-custom.ts` but
  were absent from `REACT_HANDWRITTEN`, so codegen emitted a second copy of each; `react.ts`
  star-exports both modules, and a name exported by two `export *` sources is ambiguous, which ES
  semantics resolve by omitting it entirely. Consumers got "has no exported member 'BeamLineChart'"
  while the definition sat visibly in the shipped bundle.
- **`.d.ts` builds no longer drop declarations that reference the DOM `Element` type.**
  `react-elements.ts` emits inside `declare module 'react' { namespace JSX { … } }`, where a bare
  `Element` binds to React's `JSX.Element` rather than the DOM type and fails with TS4033. Such types
  are now emitted as `globalThis.Element`.
- `@types/react` / `@types/react-dom` / `@vitejs/plugin-react` are now devDependencies. Node and
  TypeScript resolve a package's imports from its real location, so without them a `npm link`ed
  toolkit could not typecheck its own React surface or load its Vite plugin — and the missing React
  types were silently degrading emitted prop types to ones without `children`.

### Changed
- Generated React prop types now include typed handlers for each component's custom events
  (`onWaRemove`, `onWaChange`, `onWaTabShow`, …). The codegen previously read only CEM attributes, so
  a component whose output was a custom event had no typed React surface at all and needed either a
  cast or a hand-written forwarder. Purely additive — every new prop is optional.

## [0.4.0] - 2026-07-13

### Added
- `useMountSiteCandidates` function can query available extensions for a given mount site
- `BeamExtensionSite` accepts `include` and `resolve` props for filtering and ordering available extensions

## [0.3.0] - 2026-07-10

### Added
- shared storage layer for extensions
- context site data for arbitrary data sharing between extensions

## [0.2.0] - 2026-06-23

### Added
- `ExtensionContext` gains new fields, `params`, `location`, `navigate`, `mount`, `config`, and `updateBadge()`
- Extensions can add badges to nav bar
- `definePortalExtensionConfig` vite extensions

## [0.1.10] - 2026-06-03

### Changed

- Also update peer dependencies with web sdk version `1.2.1`

## [0.1.9] - 2026-06-03

### Changed

- Properly update web sdk version to `1.2.1`

## [0.1.8] - 2026-06-03

### Changed

- Updated web sdk version to `1.2.1`

## [0.1.7] - 2026-06-03

### Added

- Add `BeamExtensionSite` and `BeamExtensionChild` components types

## [0.1.4] - 2026-06-03

### Added

- `portalExtensionPlugin({ react: true })` (Vite) and `portalExtensionRollup({ react: true })` (Rollup) options that externalize `react`, `react-dom`, `react-dom/client`, and `react/jsx-runtime` so React-based extensions can share the Portal host's React runtime via window globals.
- `react` and `react-dom` are declared as optional peer dependencies.
- `@beamable/portal-toolkit/react` types entry: a strict React JSX augmentation that adds per-component type information for `beam-*` web components (parallel to the existing `@beamable/portal-toolkit/svelte` entry).

### Removed

- `@beamable/portal-toolkit/svelte` types export and `svelte` peer/dev dependencies. Svelte template is no longer supported — React is the only portal extension template.

### Changed

- Uses beam web component definitions from in development portal.
