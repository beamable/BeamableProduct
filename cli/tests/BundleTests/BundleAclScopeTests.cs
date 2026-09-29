using cli;
using cli.BundleCommands;
using NUnit.Framework;

namespace tests.BundleTests;

/// <summary>
/// Unit tests for <see cref="BundleAclScope.Resolve"/> — the CLI-facing ACL visibility tier
/// keywords and their mapping to the wire tokens the catalog expects. The private tier was
/// renamed from <c>realm</c> to <c>private</c> (hard rename, no alias); the wire token stays
/// <c>cid.pid</c>.
/// </summary>
public class BundleAclScopeTests
{
	[TestCase("private", "cid.pid")]
	[TestCase("PRIVATE", "cid.pid")]
	[TestCase("org", "cid")]
	[TestCase("public", "*")]
	[TestCase("*", "*")]
	public void Resolve_KnownTier_MapsToWireToken(string input, string expected)
	{
		Assert.That(BundleAclScope.Resolve(input, null), Is.EqualTo(expected));
	}

	[Test]
	public void Resolve_Realm_IsRejected_AfterRename()
	{
		Assert.That(() => BundleAclScope.Resolve("realm", null), Throws.TypeOf<CliException>());
	}

	[TestCase("")]
	[TestCase("   ")]
	[TestCase("garbage")]
	public void Resolve_MissingOrUnknown_Throws(string input)
	{
		Assert.That(() => BundleAclScope.Resolve(input, null), Throws.TypeOf<CliException>());
	}
}
