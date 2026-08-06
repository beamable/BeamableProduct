import { Platform } from 'react-native';

import type { Subscription, WebTransport } from '../types';

/**
 * Bridge to gree/unity-webview (https://github.com/gree/unity-webview) when a React web
 * build runs inside a Unity WebView. This is the DEFAULT web transport for
 * `@beamable/notifications-react-native` (see `index.web.ts`); it exports both the
 * `WebTransport` implementation (`unityTransport`) and the raw helpers a host app's demo /
 * diagnostics UI may want.
 *
 * Inert off-web (native builds import this too via the shared re-exports; `isWeb` is false
 * there so nothing runs).
 *
 * Transport:
 *  - Page → Unity: `window.Unity.call(msg)`. On Android gree exposes this
 *    natively via addJavascriptInterface(..., "Unity"). On iOS and the macOS
 *    editor (WKWebView) it does NOT exist by default — the Unity side injects
 *    the shim in its `ld:` (page-loaded) callback. Because the shim arrives
 *    after page load, this module polls for it and then performs the
 *    handshake.
 *  - Unity → page: Unity calls
 *    webViewObject.EvaluateJS("window.onUnityMessage('...')"); this module
 *    owns that global.
 *
 * Protocol (JSON envelopes over the string transport):
 *  - page → Unity: {type:'ready'}                       handshake request
 *                  {type:'call', method, args?, id?}    invoke native API
 *  - Unity → page: {type:'platform', os, isEditor, nativeSupported}
 *                  {type:'event', name, payload}        native SDK event
 *                  {type:'result', id, ok, payload|error}  reply to a call
 * Anything that is not one of these envelopes is delivered to the raw
 * message listeners (used by the demo bridge panel).
 */

declare global {
  interface Window {
    Unity?: { call: (msg: string) => void };
    onUnityMessage?: (msg: string) => void;
  }
}

const isWeb = Platform.OS === 'web' && typeof window !== 'undefined';

/** What the Unity host reports about itself in the handshake (a `WebHostInfo`). */
export type UnityHostPlatform = {
  /** e.g. 'ios', 'android', 'osx-editor', 'windows-editor', 'osx' */
  os: string;
  isEditor: boolean;
  /** True when the Beamable native notifications library is functional (iOS/Android device builds). */
  nativeSupported: boolean;
};

let hostPlatform: UnityHostPlatform | null = null;
let nextRequestId = 1;

type Listener<T> = (value: T) => void;
const rawListeners = new Set<Listener<string>>();
const platformListeners = new Set<Listener<UnityHostPlatform>>();
const eventListeners = new Map<string, Set<Listener<unknown>>>();
const pendingRequests = new Map<
  number,
  { resolve: (v: unknown) => void; reject: (e: Error) => void }
>();

/** True when running inside a gree Unity WebView (after the Unity shim exists). */
export function isUnityWebView(): boolean {
  return isWeb && typeof window.Unity?.call === 'function';
}

/** The Unity host's platform info, or null before the handshake completes. */
export function getUnityHostPlatform(): UnityHostPlatform | null {
  return hostPlatform;
}

/** Subscribe to the host-platform handshake. Fires immediately if already known. */
export function addUnityPlatformListener(
  listener: Listener<UnityHostPlatform>,
): Subscription {
  platformListeners.add(listener);
  if (hostPlatform) listener(hostPlatform);
  return { remove: () => platformListeners.delete(listener) };
}

/**
 * Post a raw string message to Unity. Returns false when not inside a Unity
 * WebView (plain browser or native app) — the message is simply dropped.
 */
export function sendToUnity(message: string): boolean {
  if (!isUnityWebView()) return false;
  window.Unity!.call(message);
  return true;
}

/** Fire-and-forget native API call: {type:'call', method, args}. */
export function callUnity(method: string, args?: unknown): boolean {
  return sendToUnity(JSON.stringify({ type: 'call', method, args }));
}

/**
 * Native API call with a reply: resolves with the {type:'result'} payload.
 * Rejects when outside a Unity WebView, on a Unity-side error, or on timeout.
 */
export function requestUnity<T>(
  method: string,
  args?: unknown,
  timeoutMs = 5000,
): Promise<T> {
  return new Promise<T>((resolve, reject) => {
    const id = nextRequestId++;
    if (!sendToUnity(JSON.stringify({ type: 'call', method, args, id }))) {
      reject(new Error('Not inside a Unity WebView'));
      return;
    }
    const timer = setTimeout(() => {
      pendingRequests.delete(id);
      reject(new Error(`Unity call '${method}' timed out`));
    }, timeoutMs);
    pendingRequests.set(id, {
      resolve: (v) => {
        clearTimeout(timer);
        resolve(v as T);
      },
      reject: (e) => {
        clearTimeout(timer);
        reject(e);
      },
    });
  });
}

/** Subscribe to a named native SDK event forwarded by Unity. Inert off-web. */
export function addUnityEventListener(
  name: string,
  listener: Listener<unknown>,
): Subscription {
  let set = eventListeners.get(name);
  if (!set) {
    set = new Set();
    eventListeners.set(name, set);
  }
  set.add(listener);
  return { remove: () => eventListeners.get(name)?.delete(listener) };
}

/** Subscribe to raw (non-protocol) messages from Unity. Returns { remove }. */
export function addUnityMessageListener(listener: Listener<string>): Subscription {
  rawListeners.add(listener);
  return { remove: () => rawListeners.delete(listener) };
}

function handleInbound(msg: string): void {
  let parsed: any = null;
  try {
    parsed = JSON.parse(msg);
  } catch {
    // not JSON — raw message
  }
  if (parsed && typeof parsed === 'object') {
    switch (parsed.type) {
      case 'platform': {
        hostPlatform = {
          os: String(parsed.os ?? 'unknown'),
          isEditor: Boolean(parsed.isEditor),
          nativeSupported: Boolean(parsed.nativeSupported),
        };
        platformListeners.forEach((l) => l(hostPlatform!));
        return;
      }
      case 'event': {
        eventListeners.get(String(parsed.name))?.forEach((l) => l(parsed.payload));
        return;
      }
      case 'result': {
        const pending = pendingRequests.get(parsed.id);
        if (pending) {
          pendingRequests.delete(parsed.id);
          if (parsed.ok) pending.resolve(parsed.payload);
          else pending.reject(new Error(String(parsed.error ?? 'Unity call failed')));
        }
        return;
      }
    }
  }
  rawListeners.forEach((l) => l(String(msg)));
}

if (isWeb) {
  window.onUnityMessage = handleInbound;

  // Handshake: window.Unity appears at page load on Android but only after
  // Unity's ld: injection on iOS/macOS — poll, then announce we're ready.
  // Unity replies with the {type:'platform'} envelope.
  let attempts = 0;
  const poll = setInterval(() => {
    attempts += 1;
    if (isUnityWebView()) {
      clearInterval(poll);
      sendToUnity(JSON.stringify({ type: 'ready' }));
    } else if (attempts > 120) {
      clearInterval(poll); // plain browser — give up quietly
    }
  }, 500);
}

/**
 * The default web `WebTransport` — routes façade calls to a Unity WebView host over the
 * gree bridge above. Support is dynamic: false until the Unity handshake reports a
 * native-capable host.
 */
export const unityTransport: WebTransport = {
  isSupported: () => getUnityHostPlatform()?.nativeSupported ?? false,
  getHost: () => {
    const h = getUnityHostPlatform();
    return h
      ? { os: h.os, isEditor: h.isEditor, nativeSupported: h.nativeSupported }
      : null;
  },
  addSupportListener: (listener) => {
    listener(getUnityHostPlatform()?.nativeSupported ?? false);
    return addUnityPlatformListener((p) => listener(p.nativeSupported));
  },
  call: (method, args) => {
    callUnity(method, args);
  },
  request: (method, args, timeoutMs) => requestUnity(method, args, timeoutMs),
  addEventListener: (name, listener) => addUnityEventListener(name, listener),
};
