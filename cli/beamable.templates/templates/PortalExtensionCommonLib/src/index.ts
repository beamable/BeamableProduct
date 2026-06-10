// Public entry point for this shared library.
//
// Anything exported here becomes importable from a Portal Extension that depends
// on this library, e.g. `import { greet, SampleWidget } from '<library-name>'`.
// The consuming extension's Vite build transpiles this source and bundles it into
// the extension, so there is no separate build step for this library.

export * from './sample';
export * from './SampleWidget';
