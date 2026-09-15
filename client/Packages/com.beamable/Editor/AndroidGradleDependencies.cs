// Android-only in its entirety. UnityEditor.Android ships with the Android build support module, so
// referencing it unguarded would break compilation for anyone who has not installed that module.
// Same pattern BuildPostProcessor.cs uses for UnityEditor.iOS.Xcode.
#if UNITY_ANDROID

using Beamable.AccountManagement;
using Beamable.Common.Api.Auth;
using System;
using System.IO;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

namespace Beamable.Editor
{
	/// <summary>
	/// Puts Google Play services on the classpath of the Gradle project Unity generates, when Google
	/// Sign-In is enabled.
	/// </summary>
	/// <remarks>
	/// <para><c>googlesignin-release.aar</c> declares Play services as an <c>implementation</c>
	/// dependency, so the .aar neither embeds it nor carries dependency metadata for it. Something in
	/// the consuming project has to supply it, or every call into the plugin fails at runtime with
	/// <c>NoClassDefFoundError</c>.</para>
	///
	/// <para><b>Why this is a post-generate callback and not a <c>mainTemplate.gradle</c>.</b> A
	/// custom template replaces the one Unity would have written, which makes it specific to the
	/// Unity version it was copied from - and Beamable supports 2021.3 upwards, which spans a range
	/// no single template can cover. Unity 2021.3 builds with AGP 4.0.1, where the <c>namespace</c>
	/// DSL does not exist yet; Unity 6 builds with AGP 8, which requires it and will not read the
	/// package name from the manifest instead. The two are mutually exclusive in one file, and that
	/// is before the token vocabulary, which also changes between versions - an unrecognised
	/// <c>**TOKEN**</c> is left in the output verbatim and fails Gradle with a syntax error.</para>
	///
	/// <para>Running after generation sidesteps all of it: Unity writes whatever build.gradle is
	/// correct for the version in use, and this appends one dependency to it. Gradle merges repeated
	/// <c>dependencies</c> blocks, so nothing has to be parsed or matched.</para>
	///
	/// <para>Opt out with the <c>BEAMABLE_NO_ANDROID_GRADLE_DEPENDENCIES</c> define, or simply
	/// declare Play services yourself - a project that already has it on the unityLibrary classpath
	/// is left alone.</para>
	/// </remarks>
	public class AndroidGradleDependencies : IPostGenerateGradleAndroidProject
	{
		/// <summary>
		/// The Play services artifact, without a version, used to detect a project that already
		/// supplies it - including one that pins a different version on purpose.
		/// </summary>
		private const string GMS_AUTH_ARTIFACT = "com.google.android.gms:play-services-auth";

		/// <summary>
		/// Keep in step with <c>plugins/google-signin/googlesignin/build.gradle</c>. The plugin
		/// compiles against this version, so anything a project resolves instead has to be new enough
		/// for the APIs it calls.
		/// </summary>
		private const string GMS_AUTH_VERSION = "18.1.0";

		private const string LOG_PREFIX = "[Beamable] Google Sign-In: ";

		public int callbackOrder => 0;

		/// <param name="unityLibraryPath">
		/// The generated <c>unityLibrary</c> module directory - the module the .aar is packaged into,
		/// and therefore the one whose classpath needs Play services.
		/// </param>
		public void OnPostGenerateGradleAndroidProject(string unityLibraryPath)
		{
#if !BEAMABLE_NO_ANDROID_GRADLE_DEPENDENCIES
			if (!IsGoogleSignInEnabled())
			{
				return;
			}

			var buildGradlePath = Path.Combine(unityLibraryPath, "build.gradle");

			try
			{
				if (!File.Exists(buildGradlePath))
				{
					throw new FileNotFoundException(
						"Unity did not generate a build.gradle for the unityLibrary module.", buildGradlePath);
				}

				var contents = File.ReadAllText(buildGradlePath);
				if (contents.Contains(GMS_AUTH_ARTIFACT))
				{
					Debug.Log($"{LOG_PREFIX}{GMS_AUTH_ARTIFACT} is already declared in " +
							  $"{buildGradlePath}; leaving it alone.");
					return;
				}

				File.WriteAllText(buildGradlePath, contents + DependencyBlock());
				Debug.Log($"{LOG_PREFIX}added {GMS_AUTH_ARTIFACT}:{GMS_AUTH_VERSION} to {buildGradlePath}.");
			}
			catch (Exception e)
			{
				// Failing the build is the lesser evil. The alternative is an APK that looks fine and
				// then throws NoClassDefFoundError the first time a player taps the Google button -
				// in release, on a device, long after anyone would connect it to this step.
				throw new BuildFailedException(
					$"Beamable could not add {GMS_AUTH_ARTIFACT}:{GMS_AUTH_VERSION} to {buildGradlePath}, " +
					$"which Google Sign-In needs. Either declare it yourself in your own " +
					$"Assets/Plugins/Android/mainTemplate.gradle - this step then sees it and does nothing - " +
					$"or define BEAMABLE_NO_ANDROID_GRADLE_DEPENDENCIES to skip this step, or turn Google off " +
					$"in the Account Management configuration. ({e.Message})");
			}
#endif
		}

		/// <summary>
		/// Whether Google Sign-In is switched on in the Account Management configuration.
		/// </summary>
		/// <remarks>
		/// Reuses <see cref="AccountManagementConfiguration.AuthEnabled"/> rather than reading the
		/// <c>Google</c> checkbox directly, so this answers the same question the login flow asks at
		/// runtime, including the iOS-only <c>EnableGoogleSignInOnApple</c> rule it encodes.
		///
		/// <para>Treated as "off" if the configuration cannot be read: the .aar's own consumer proguard
		/// rules cover the minification half of the setup regardless, and a build should not fail over a
		/// missing optional asset.</para>
		/// </remarks>
		private static bool IsGoogleSignInEnabled()
		{
			try
			{
				var configuration = AccountManagementConfiguration.Instance;
				return configuration != null && configuration.AuthEnabled(AuthThirdParty.Google);
			}
			catch (Exception e)
			{
				Debug.Log($"{LOG_PREFIX}could not read the Account Management configuration while deciding " +
					  $"whether to add {GMS_AUTH_ARTIFACT}. {e.Message}");
				return false;
			}
		}

		/// <summary>
		/// A standalone <c>dependencies</c> block to append. Gradle merges repeated blocks, so this
		/// needs no knowledge of what Unity wrote above it.
		/// </summary>
		private static string DependencyBlock()
		{
			return "\n"
				 + "// Added by Beamable (Editor/AndroidGradleDependencies.cs) because Google Sign-In is enabled\n"
				 + "// in the Account Management configuration. googlesignin-release.aar declares Google Play\n"
				 + "// services as an `implementation` dependency, so it neither embeds it nor carries dependency\n"
				 + "// metadata for it - the consuming project has to put it on the classpath.\n"
				 + "//\n"
				 + "// Declare it yourself in Assets/Plugins/Android/mainTemplate.gradle and this step will see it\n"
				 + "// and do nothing, or turn the step off with the BEAMABLE_NO_ANDROID_GRADLE_DEPENDENCIES define.\n"
				 + "dependencies {\n"
				 + $"    implementation '{GMS_AUTH_ARTIFACT}:{GMS_AUTH_VERSION}'\n"
				 + "}\n";
		}
	}
}

#endif
