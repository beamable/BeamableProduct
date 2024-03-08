using Beamable.Common;
using NUnit.Framework;

namespace tests;

public class VersioningTests
{
	[Test]
	public void ParseBetaVersion()
	{
		if (!PackageVersion.TryFromSemanticVersionString("2.0.0-EXP.RC1", out var version))
		{
			Assert.Fail("unable to parse version at all");
		}
		
		Assert.That(version.Major, Is.EqualTo(2));
		Assert.That(version.Minor, Is.EqualTo(0));
		Assert.That(version.Patch, Is.EqualTo(0));
		Assert.That(version.IsNightly, Is.EqualTo(false));
		Assert.That(version.IsExperimental, Is.EqualTo(true));
		Assert.That(version.IsPreview, Is.EqualTo(false));
		Assert.That(version.IsReleaseCandidate, Is.EqualTo(true));
		Assert.That(version.RC, Is.EqualTo(1));

		var stringifed = version.ToString();
		Assert.That(stringifed, Is.EqualTo("2.0.0-EXPERIMENTAL.RC1"));
	}
}
