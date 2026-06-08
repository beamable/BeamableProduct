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

import { createElement, useEffect, useState, StrictMode, type ComponentType, type ReactNode } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { Portal, type ExtensionContext } from './portal';
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
  const { beamId, App, disableStrictMode, wrapper } = options;
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
  });
}
