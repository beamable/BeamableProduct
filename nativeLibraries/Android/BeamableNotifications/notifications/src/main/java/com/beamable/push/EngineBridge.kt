package com.beamable.push

/**
 * Lowest-common-denominator bridge to a host game engine.
 *
 * Engine-specific adapters (Unity, Unreal, React Native) implement this to
 * forward a callback [method] name and a serialized [payload] string back to
 * managed/script code. The core library never depends on any engine type.
 */
interface EngineBridge {
    /**
     * Emit a callback to the host engine.
     *
     * @param method the callback/handler name the engine should dispatch to.
     * @param payload a string (often JSON) carrying the callback data.
     */
    fun emit(method: String, payload: String)
}
