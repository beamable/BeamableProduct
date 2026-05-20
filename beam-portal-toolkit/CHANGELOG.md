# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.4] - Unreleased

### Added
- `portalExtensionPlugin({ react: true })` (Vite) and `portalExtensionRollup({ react: true })` (Rollup) options that externalize `react`, `react-dom`, `react-dom/client`, and `react/jsx-runtime` so React-based extensions can share the Portal host's React runtime via window globals.
- `react` and `react-dom` are declared as optional peer dependencies.
- `@beamable/portal-toolkit/react` types entry: a strict React JSX augmentation that adds per-component type information for `beam-*` web components (parallel to the existing `@beamable/portal-toolkit/svelte` entry).

### Changed
- Uses beam web component definitions from in development portal.