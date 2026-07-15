using System;
using System.IO;
using System.Linq;
using cli.Services.Sandbox;
using NUnit.Framework;

namespace tests.SandboxTests;

[TestFixture]
public class SandboxStateServiceTests
{
	private string _root = null!;

	[SetUp]
	public void SetUp()
	{
		_root = Path.Combine(Path.GetTempPath(), "sandbox-state-" + Guid.NewGuid().ToString("N"));
	}

	[TearDown]
	public void TearDown()
	{
		try { Directory.Delete(_root, recursive: true); } catch { }
	}

	[Test]
	public void WriteAndRead_RoundTrips()
	{
		var s = new SandboxStateService(_root);
		s.WriteCode("BeamSandbox_4827_abcd", "K2M-X9Q3");
		Assert.That(s.ReadCode("BeamSandbox_4827_abcd"), Is.EqualTo("K2M-X9Q3"));
	}

	[Test]
	public void MissingFile_ReadReturnsNull()
	{
		var s = new SandboxStateService(_root);
		Assert.That(s.ReadCode("BeamSandbox_does_not_exist"), Is.Null);
	}

	[Test]
	public void Remove_DeletesFile()
	{
		var s = new SandboxStateService(_root);
		s.WriteCode("BeamSandbox_x", "code");
		Assert.That(File.Exists(s.PathForService("BeamSandbox_x")), Is.True);
		s.RemoveCode("BeamSandbox_x");
		Assert.That(File.Exists(s.PathForService("BeamSandbox_x")), Is.False);
	}

	[Test]
	public void RemoveMissing_NoThrow()
	{
		var s = new SandboxStateService(_root);
		Assert.DoesNotThrow(() => s.RemoveCode("BeamSandbox_does_not_exist"));
	}

	[Test]
	public void ListLocal_EnumeratesAllCodeFiles()
	{
		var s = new SandboxStateService(_root);
		s.WriteCode("BeamSandbox_a", "c1");
		s.WriteCode("BeamSandbox_b", "c2");

		var listed = s.ListLocalSandboxes().ToList();
		Assert.That(listed, Has.Count.EqualTo(2));
		Assert.That(listed.Select(t => t.serviceName), Is.EquivalentTo(new[] { "BeamSandbox_a", "BeamSandbox_b" }));
	}

	[Test]
	public void OwnerOnlyPermissions_OnPosix()
	{
		if (OperatingSystem.IsWindows())
		{
			Assert.Ignore("Permission semantics differ on Windows");
			return;
		}

		var s = new SandboxStateService(_root);
		s.WriteCode("BeamSandbox_p", "secret-code");
		var mode = File.GetUnixFileMode(s.PathForService("BeamSandbox_p"));
		// User read+write; no group / other perms.
		Assert.That(mode, Is.EqualTo(UnixFileMode.UserRead | UnixFileMode.UserWrite));
	}
}
