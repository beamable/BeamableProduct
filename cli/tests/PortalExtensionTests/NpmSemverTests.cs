using cli.Utils;
using NUnit.Framework;

namespace tests.PortalExtensionTests;

public class NpmSemverTests
{
	private static bool Satisfies(string version, string range)
	{
		Assert.That(NpmSemver.TrySatisfies(version, range, out var satisfied), Is.True,
			$"expected [{version}] vs [{range}] to be decidable");
		return satisfied;
	}

	[TestCase("0.2.0", "0.2.0", true)]
	[TestCase("0.2.1", "0.2.0", false)]
	[TestCase("0.2.0", "=0.2.0", true)]
	[TestCase("1.0.0", "1.0.0", true)]
	public void Exact(string version, string range, bool expected)
	{
		Assert.That(Satisfies(version, range), Is.EqualTo(expected));
	}

	[TestCase("19.0.0", "^19.0.0", true)]
	[TestCase("19.5.2", "^19.0.0", true)]
	[TestCase("20.0.0", "^19.0.0", false)]
	[TestCase("18.9.9", "^19.0.0", false)]
	// Caret on a 0.x version is locked to the minor.
	[TestCase("0.2.0", "^0.2.0", true)]
	[TestCase("0.2.9", "^0.2.0", true)]
	[TestCase("0.3.0", "^0.2.0", false)]
	[TestCase("0.1.9", "^0.2.0", false)]
	// Caret on a 0.0.x version is locked to the patch.
	[TestCase("0.0.3", "^0.0.3", true)]
	[TestCase("0.0.4", "^0.0.3", false)]
	public void Caret(string version, string range, bool expected)
	{
		Assert.That(Satisfies(version, range), Is.EqualTo(expected));
	}

	[TestCase("1.2.3", "~1.2.3", true)]
	[TestCase("1.2.9", "~1.2.3", true)]
	[TestCase("1.3.0", "~1.2.3", false)]
	[TestCase("1.2.0", "~1.2", true)]
	[TestCase("1.3.0", "~1.2", false)]
	[TestCase("1.9.9", "~1", true)]
	[TestCase("2.0.0", "~1", false)]
	public void Tilde(string version, string range, bool expected)
	{
		Assert.That(Satisfies(version, range), Is.EqualTo(expected));
	}

	[TestCase("1.2.5", "1.2.x", true)]
	[TestCase("1.3.0", "1.2.x", false)]
	[TestCase("1.9.9", "1.x", true)]
	[TestCase("2.0.0", "1.x", false)]
	[TestCase("123.45.6", "*", true)]
	[TestCase("0.0.0", "x", true)]
	public void XRangesAndWildcards(string version, string range, bool expected)
	{
		Assert.That(Satisfies(version, range), Is.EqualTo(expected));
	}

	[TestCase("1.5.0", ">=1.0.0 <2.0.0", true)]
	[TestCase("2.0.0", ">=1.0.0 <2.0.0", false)]
	[TestCase("0.9.0", ">=1.0.0 <2.0.0", false)]
	[TestCase("1.0.0", ">=1.0.0", true)]
	[TestCase("1.0.0", ">1.0.0", false)]
	[TestCase("1.0.1", ">1.0.0", true)]
	[TestCase("2.0.0", "<=2.0.0", true)]
	[TestCase("2.0.1", "<=2.0.0", false)]
	public void Comparators(string version, string range, bool expected)
	{
		Assert.That(Satisfies(version, range), Is.EqualTo(expected));
	}

	[TestCase("18.0.0", "^18.0.0 || ^19.0.0", true)]
	[TestCase("19.2.0", "^18.0.0 || ^19.0.0", true)]
	[TestCase("20.0.0", "^18.0.0 || ^19.0.0", false)]
	[TestCase("17.0.0", "^18.0.0 || ^19.0.0", false)]
	public void OrUnions(string version, string range, bool expected)
	{
		Assert.That(Satisfies(version, range), Is.EqualTo(expected));
	}

	[TestCase("1.2.3", "1.2.3 - 2.3.4", true)]
	[TestCase("2.3.4", "1.2.3 - 2.3.4", true)]
	[TestCase("2.4.0", "1.2.3 - 2.3.4", false)]
	[TestCase("1.2.2", "1.2.3 - 2.3.4", false)]
	[TestCase("2.3.9", "1.2.3 - 2.3", true)]
	[TestCase("2.4.0", "1.2.3 - 2.3", false)]
	public void HyphenRanges(string version, string range, bool expected)
	{
		Assert.That(Satisfies(version, range), Is.EqualTo(expected));
	}

	[Test]
	public void Prerelease_HasLowerPrecedenceThanRelease()
	{
		// A prerelease of the same tuple sorts below the release.
		Assert.That(Satisfies("1.0.0-rc.1", ">=1.0.0"), Is.False);
		Assert.That(Satisfies("1.0.0", ">=1.0.0-rc.1"), Is.True);
	}

	[TestCase("workspace:*")]
	[TestCase("file:../foo")]
	[TestCase("git+https://example.com/repo.git")]
	[TestCase("npm:@scope/pkg@1.0.0")]
	public void Undecidable_RangesReturnFalseFromTry(string range)
	{
		Assert.That(NpmSemver.TrySatisfies("1.0.0", range, out _), Is.False,
			"non-semver ranges must be reported as undecided so callers can fail open");
	}

	[Test]
	public void Undecidable_WhenVersionIsNotConcrete()
	{
		Assert.That(NpmSemver.TrySatisfies("not-a-version", "^1.0.0", out _), Is.False);
		Assert.That(NpmSemver.TrySatisfies("1.2", "^1.0.0", out _), Is.False, "a partial version isn't a concrete install");
	}
}
