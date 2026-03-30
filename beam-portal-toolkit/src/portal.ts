// Portal extension registration utilities.
// Import via:  import { Portal } from '@beamable/portal-toolkit';

import { Beam, BeamBase } from "beamable-sdk";

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

/**
 * Runtime context provided by the Beamable portal to every extension on mount.
 */
export interface ExtensionContext extends Map<any, any> {
  realm: string;
  cid: string;
  beam: Promise<Beam>;
}

/**
 * Options passed to {@link Portal.registerExtension}.
 */
export interface RegisterExtensionOptions {
  /**
   * Unique name for the extension
   */
  beamId: string;

  /**
   * Called when the portal mounts this extension into the DOM.
   *
   * Render your UI into `container`. The portal guarantees that
   * `container` is attached to the document before this callback fires.
   *
   * @returns some unique instance object for the service. The same instance will be given to the unmount call later.
   * @param container - The `HTMLElement` allocated for this extension.
   * @param context   - Runtime context supplied by the portal.
   */
  onMount: (container: HTMLElement, context: ExtensionContext) => unknown | Promise<unknown>;

  /**
   * Called when the portal is about to remove this extension from the DOM.
   *
   * Use this to tear down event listeners, timers, framework instances, etc.
   * The portal waits for a returned `Promise` to settle before removing
   * `container` from the DOM.
   * @param instance - The instance object returned by the corresponding `onMount` call.
   */
  onUnmount: (instance: unknown) => void | Promise<void>;
}


// ---------------------------------------------------------------------------
// Implementation
// ---------------------------------------------------------------------------

/**
 * Registers an extension with the Beamable portal.
 *
 * @example
 * ```ts
 * import { Portal } from '@beamable/portal-toolkit';
 *
 * Portal.registerExtension({
 *   beamId: 'MyExtension',
 *   onMount(container, context) {
 *     container.innerHTML = `<p>Hello from ${context.realm}</p>`;
 *   },
 *   onUnmount(instance) {
 *     // clean up
 *   },
 * });
 * ```
 */
function registerExtension(options: RegisterExtensionOptions): void {

  (window as unknown as Record<string, unknown>)[options.beamId] = {
    mount: (targetElement: HTMLElement, context: ExtensionContext) => {
      return options.onMount(targetElement, context);
    },
    unmount: (instance: unknown) => {
      return options.onUnmount(instance);
    },
  };

}

// ---------------------------------------------------------------------------
// Namespace export
// ---------------------------------------------------------------------------

/**
 * Top-level namespace for portal utilities.
 *
 * @example
 * ```ts
 * import { Portal } from '@beamable/portal-toolkit';
 * Portal.registerExtension({ ... });
 * ```
 */
export const Portal = {
  registerExtension,
} as const;
