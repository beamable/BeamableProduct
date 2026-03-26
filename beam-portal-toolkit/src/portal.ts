// Portal plugin registration utilities.
// Import via:  import { Portal } from '@beamable/portal-toolkit';

import { BeamBase } from "beamable-sdk";

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

/**
 * Runtime context provided by the Beamable portal to every plugin on mount.
 */
export interface PluginContext extends Map<any, any> {
  realm: string;
  cid: string;
  beam: BeamBase;
}

/**
 * Options passed to {@link Portal.registerPlugin}.
 */
export interface RegisterPluginOptions {
  /**
   * Unique name for the extension
   */
  beamId: string;

  /**
   * Called when the portal mounts this plugin into the DOM.
   *
   * Render your UI into `container`. The portal guarantees that
   * `container` is attached to the document before this callback fires.
   * 
   * @returns some unique instance object for the service. The same instance will be given to the unmount call later. 
   * @param container - The `HTMLElement` allocated for this plugin.
   * @param context   - Runtime context supplied by the portal.
   */
  onMount: (container: HTMLElement, context: PluginContext) => unknown | Promise<unknown>;

  /**
   * Called when the portal is about to remove this plugin from the DOM.
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
 * Registers a plugin with the Beamable portal.
 *
 */
function registerPlugin(options: RegisterPluginOptions): void {

  (window as unknown as Record<string, unknown>)[options.beamId] = {
    mount: (targetElement: HTMLElement, context: PluginContext) => {
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
 * Portal.registerPlugin({ ... });
 * ```
 */
export const Portal = {
  registerPlugin,
} as const;
