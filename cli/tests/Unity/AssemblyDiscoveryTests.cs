using Beamable.Common.Reflection;
using NUnit.Framework;

namespace tests.Unity;

public class AssemblyDiscoveryTests
{
	[Test]
	public void DiscoversCommonAndCallingAssemblyOutsideUnity()
	{
		var assemblies = AssemblyDiscovery.GetLoadedAssemblies();
		Assert.That(assemblies, Does.Contain(typeof(AssemblyDiscovery).Assembly));
		Assert.That(assemblies, Does.Contain(typeof(AssemblyDiscoveryTests).Assembly));
	}
}
