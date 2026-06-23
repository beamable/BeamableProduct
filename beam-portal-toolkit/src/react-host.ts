// Shared host-component lookup for the toolkit's React forwarders.
//
// The host portal publishes its real `Beam*` React components on
// `window.__beamPortal.react.<Name>` at boot (see the portal's
// `extensionReactComponents.ts`). Each toolkit forwarder calls
// `hostComponent('BeamX')` and renders the result through `React.createElement`.
//
// We deliberately throw if the host hasn't populated the registry: extensions
// are only ever rendered inside the portal, so a missing host component means
// either a host/toolkit version mismatch or a boot-ordering bug — both of
// which should surface loudly.

import type { ComponentType } from 'react';

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type AnyComponent = ComponentType<any>;

interface BeamPortalGlobal {
  react?: Record<string, AnyComponent>;
}

declare global {
  // eslint-disable-next-line no-var
  var __beamPortal: BeamPortalGlobal | undefined;
}

// `any` props on purpose. The host component's prop type is whatever the host
// chose; the toolkit forwarders type-check at the call site against their own
// `BeamFooProps` interface and pass the prop bag straight through. Typing the
// host as `ComponentType<unknown>` would make `createElement(host, typedProps)`
// fail (ComponentType is contravariant in its prop type).
export function hostComponent(name: string): AnyComponent {
  const registry =
    (typeof globalThis !== 'undefined'
      ? (globalThis as { __beamPortal?: BeamPortalGlobal }).__beamPortal?.react
      : undefined) ?? {};
  const Cmp = registry[name];
  if (!Cmp) {
    throw new Error(
      `Beam React component "${name}" is not provided by the host portal. ` +
        `Extensions must run inside the Beamable portal — see ` +
        `https://help.beamable.com/ for the extension setup guide.`,
    );
  }
  return Cmp;
}
