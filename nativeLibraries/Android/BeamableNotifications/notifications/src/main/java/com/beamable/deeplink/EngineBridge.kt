package com.beamable.deeplink

/**
 * Engine-agnostic outbound bridge.
 *
 * Each game engine (Unity, Unreal, React Native) provides its own implementation
 * that forwards a named method call + string payload into the managed/script layer.
 * The deeplink core never references any engine type directly; it only talks to
 * this interface (and to [DeepLinkListener]).
 */
interface EngineBridge {
    /**
     * Emit a call to the engine layer.
     *
     * @param method engine-side handler name (e.g. a Unity method on a GameObject).
     * @param payload the string payload (e.g. the raw deeplink URL).
     */
    fun emit(method: String, payload: String)
}
