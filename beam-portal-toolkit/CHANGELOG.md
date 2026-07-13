# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
