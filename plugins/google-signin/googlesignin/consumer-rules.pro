# Rules shipped inside the .aar as proguard.txt and applied automatically to any app that consumes
# this library, including the Gradle project Unity generates.
#
# Everything in this package is reached only from C#, through JNI reflection by name
# (AndroidJavaClass + CallStatic), and GoogleSignInActivity is additionally instantiated by the
# Android framework by name. R8 sees no Java call sites for any of it, so without these rules a
# minified release build strips or renames the entry points and every call from C# fails at runtime
# with a NoSuchMethodError wrapped in an AndroidJavaException.
#
# Deliberately the same shape as the rule Beamable's Editor/BuildPreProcessor.cs asks users to add to
# Assets/Plugins/Android/proguard-user.txt, so that the two mechanisms agree rather than compete.
-keep class com.beamable.googlesignin.** { *; }
