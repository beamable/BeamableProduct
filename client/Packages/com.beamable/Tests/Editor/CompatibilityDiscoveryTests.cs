using Beamable.Common.Reflection;
using Beamable.Editor.Util;
using NUnit.Framework;
using UnityEngine;

namespace Beamable.Editor.Tests
{
	public class CompatibilityDiscoveryTests
	{
		[Test]
		public void LoadedAssembliesIncludeSdkAndCallingAssembly()
		{
			var assemblies = AssemblyDiscovery.GetLoadedAssemblies();
			Assert.That(assemblies, Does.Contain(typeof(AssemblyDiscovery).Assembly));
			Assert.That(assemblies, Does.Contain(typeof(CompatibilityDiscoveryTests).Assembly));
		}

		[Test]
		public void ObjectIdentityIsStableAndDistinct()
		{
			var first = new GameObject("first");
			var second = new GameObject("second");
			try
			{
				var identity = UnityObjectIdentity.GetObjectId(first);
				Assert.That(UnityObjectIdentity.GetObjectId(first), Is.EqualTo(identity));
				Assert.That(UnityObjectIdentity.GetObjectId(second), Is.Not.EqualTo(identity));
			}
			finally
			{
				Object.DestroyImmediate(first);
				Object.DestroyImmediate(second);
			}
		}

#if UNITY_ANDROID
		[Test]
		public void AndroidRealmHelperCanDiscoverAssembliesWithoutGooglePlayGames()
		{
			// Exercise the Android-only path without installing or configuring GPGS.
			var method = typeof(global::Beamable.Editor.UI.Config.UpdateGPGSRealmConfigHelper)
				.GetMethod("GetWebClientId", System.Reflection.BindingFlags.NonPublic |
					System.Reflection.BindingFlags.Static);
			Assert.That(method, Is.Not.Null);
			Assert.DoesNotThrow(() => method.Invoke(null, null));
		}
#endif
	}
}
