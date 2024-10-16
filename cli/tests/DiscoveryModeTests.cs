using cli.Services;
using NUnit.Framework;

namespace tests;

// I wrote these tests because I wasn't 100% sure how enum flags worked...
public class DiscoveryModeTests
{
	[Test]
	public void LocalMode()
	{
		var mode = DiscoveryMode.LOCAL;
		Assert.That(mode.HasDiscoveryFlag(DiscoveryFlags.DOCKER), Is.True, "local mode should include docker");
		Assert.That(mode.HasDiscoveryFlag(DiscoveryFlags.HOST), Is.True, "local mode should include dotnet");
		Assert.That(mode.HasDiscoveryFlag(DiscoveryFlags.REMOTE), Is.False, "local mode should not include remote");
	}

	[Test]
	public void AllMode()
	{
		var mode = DiscoveryMode.ALL;
		Assert.That(mode.HasDiscoveryFlag(DiscoveryFlags.DOCKER), Is.True, "all mode should include docker");
		Assert.That(mode.HasDiscoveryFlag(DiscoveryFlags.HOST), Is.True, "all mode should include dotnet");
		Assert.That(mode.HasDiscoveryFlag(DiscoveryFlags.REMOTE), Is.True, "all mode should include remote");
	}
}
