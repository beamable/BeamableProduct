# Consumer ProGuard rules — applied to apps that include this library.
# Public entry points are invoked from C# (Unity) / C++ (Unreal) / JS (React Native)
# via JNI or reflection, so the shrinker cannot see the call sites.

# --- Push notifications (com.beamable.push.*) ---
-keep class com.beamable.push.** { *; }
-keep class com.beamable.push.unity.** { *; }

# The Unity adapter resolves UnityPlayer.UnitySendMessage reflectively.
-keepclassmembers class com.beamable.push.unity.UnityNotificationsBridge { *; }

# Firebase Cloud Messaging service is referenced from the merged manifest.
-keep class com.beamable.push.PushFirebaseService { *; }

# Receive-time handlers are instantiated by reflection (no-arg ctor) from a
# manifest-declared class name, so keep any implementor and its default constructor.
-keep class * implements com.beamable.push.PushNotificationReceivedHandler { <init>(); }

# --- Deep links (com.beamable.deeplink.*) ---
-keep class com.beamable.deeplink.** { *; }
-keep class com.beamable.deeplink.unity.** { *; }

# Standard Firebase / FCM keeps.
-keep class com.google.firebase.** { *; }
-keep class com.google.android.gms.** { *; }
-dontwarn com.google.firebase.**
-dontwarn com.google.android.gms.**

# The bundled React Native adapters (com.beamable.{push,deeplink}.react) reference react classes
# that are absent in a Unity/Unreal app; never loaded there, so silence the missing-class warnings.
-dontwarn com.facebook.react.**
