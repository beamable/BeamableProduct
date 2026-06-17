# Consumer ProGuard rules — public entry points reached by name (engine adapters via JNI/reflection).
-keep class com.beamable.deeplink.** { *; }
-keep class com.beamable.deeplink.unity.** { *; }

# The bundled React Native adapter references react classes absent in non-RN apps; never loaded
# there, so silence the missing-class warnings.
-dontwarn com.facebook.react.**
