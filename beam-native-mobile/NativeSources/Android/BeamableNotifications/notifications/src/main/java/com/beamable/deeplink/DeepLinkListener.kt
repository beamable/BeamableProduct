package com.beamable.deeplink

/**
 * Receives raw deeplink URLs extracted natively.
 *
 * The native library only ever delivers the raw URL string; parsing into
 * scheme/host/path/query is the consumer's responsibility (e.g. the C#
 * `DeepLinkManager` on Unity).
 */
interface DeepLinkListener {
    /**
     * @param url the raw VIEW-intent data URI, e.g. "myapp://path?foo=bar".
     * @param isColdStart true if delivered from the launch intent (process start),
     *        false if delivered from a warm-start / new-intent forward.
     */
    fun onDeepLink(url: String, isColdStart: Boolean)
}
