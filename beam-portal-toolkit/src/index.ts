// Utility functions for the Beamable portal extension system.
// Add utility exports here as the toolkit grows.

// Pull in global HTMLElementTagNameMap augmentations so that any consumer
// who imports from this package automatically gets TypeScript types for all
// Beamable custom elements.  The file itself is auto-generated — run
// `pnpm sync-components` to refresh it from Portal's beam-components.json.
import './generated/globals';

export * from '@beamable/sdk';
export * from './portal';
