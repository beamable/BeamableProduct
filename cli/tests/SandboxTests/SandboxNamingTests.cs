using System;
using cli.Services.Sandbox;
using NUnit.Framework;

namespace tests.SandboxTests;

[TestFixture]
public class SandboxNamingTests
{
	[Test]
	public void BuildServiceName_ProducesParseableName()
	{
		var name = SandboxNaming.BuildServiceName(4827, Guid.NewGuid());
		Assert.That(SandboxNaming.TryParse(name, out var accountId, out var guid), Is.True);
		Assert.That(accountId, Is.EqualTo(4827));
		Assert.That(guid, Is.Not.EqualTo(Guid.Empty));
	}

	[Test]
	public void Regex_RejectsPortalExtensionNames()
	{
		Assert.That(
			SandboxNaming.TryParse("BeamPortalExtension_Foo_12345678-1234-1234-1234-123456789012", out _, out _),
			Is.False);
	}

	[Test]
	public void Regex_RejectsMalformedAccountId()
	{
		Assert.That(SandboxNaming.TryParse("BeamSandbox_abc_12345678-1234-1234-1234-123456789012", out _, out _),
			Is.False);
	}

	[Test]
	public void Regex_RejectsMissingGuid()
	{
		Assert.That(SandboxNaming.TryParse("BeamSandbox_4827", out _, out _), Is.False);
	}

	[Test]
	public void JoinCode_FormatIsXXXXDashXXXX()
	{
		var code = SandboxNaming.GenerateJoinCode();
		Assert.That(code, Has.Length.EqualTo(9));
		Assert.That(code[4], Is.EqualTo('-'));
	}

	[Test]
	public void JoinCode_AlphabetExcludesAmbiguousGlyphs()
	{
		// Generate enough codes to make a forbidden character extremely likely to appear.
		for (var i = 0; i < 200; i++)
		{
			var code = SandboxNaming.GenerateJoinCode();
			foreach (var c in code)
			{
				if (c == '-') continue;
				Assert.That(c, Is.Not.EqualTo('0'));
				Assert.That(c, Is.Not.EqualTo('O'));
				Assert.That(c, Is.Not.EqualTo('1'));
				Assert.That(c, Is.Not.EqualTo('I'));
				Assert.That(c, Is.Not.EqualTo('L'));
			}
		}
	}

	[Test]
	public void HmacSecret_Is32Bytes()
	{
		var s = SandboxNaming.GenerateHmacSecret();
		Assert.That(s.Length, Is.EqualTo(32));
	}

	[Test]
	public void HmacSecret_RandomEachCall()
	{
		var a = SandboxNaming.GenerateHmacSecret();
		var b = SandboxNaming.GenerateHmacSecret();
		Assert.That(a, Is.Not.EqualTo(b));
	}
}
