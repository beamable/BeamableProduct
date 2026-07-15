using System;
using System.IO;
using cli.Services.Sandbox;
using NUnit.Framework;

namespace tests.SandboxTests;

[TestFixture]
public class PathContainmentValidatorTests
{
	private string _root = null!;

	[SetUp]
	public void SetUp()
	{
		_root = Path.Combine(Path.GetTempPath(), "sandbox-pcv-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_root);
	}

	[TearDown]
	public void TearDown()
	{
		try { Directory.Delete(_root, recursive: true); } catch { /* best-effort */ }
	}

	[Test]
	public void SimpleRelativePath_Accepted()
	{
		var v = new PathContainmentValidator(_root);
		Assert.That(v.TryCanonicalize("services/Foo.cs", out var canonical), Is.True);
		// canonical lives under the canonicalized root, which on macOS may differ from
		// _root (e.g. /var/folders/... resolves to /private/var/folders/...). We only
		// assert containment via the validator's own check rather than comparing strings.
		Assert.That(canonical, Does.EndWith("Foo.cs"));
	}

	[Test]
	public void DotDotEscape_Rejected()
	{
		var v = new PathContainmentValidator(_root);
		Assert.That(v.TryCanonicalize("../escape.txt", out _), Is.False);
	}

	[Test]
	public void DotDotInMiddle_StillContained_Accepted()
	{
		var v = new PathContainmentValidator(_root);
		Assert.That(v.TryCanonicalize("services/../services/Foo.cs", out _), Is.True);
	}

	[Test]
	public void AbsolutePathOutsideRoot_Rejected()
	{
		var v = new PathContainmentValidator(_root);
		var outside = Path.Combine(Path.GetTempPath(), "definitely-not-in-root.txt");
		Assert.That(v.TryCanonicalize(outside, out _), Is.False);
	}

	[Test]
	public void AbsolutePathInsideRoot_Accepted()
	{
		var v = new PathContainmentValidator(_root);
		var inside = Path.Combine(_root, "inside.txt");
		Assert.That(v.TryCanonicalize(inside, out _), Is.True);
	}

	[Test]
	public void EmptyOrNull_Rejected()
	{
		var v = new PathContainmentValidator(_root);
		Assert.That(v.TryCanonicalize("", out _), Is.False);
		Assert.That(v.TryCanonicalize("   ", out _), Is.False);
		Assert.That(v.TryCanonicalize(null!, out _), Is.False);
	}

	[Test]
	public void NonExistentPath_WriteTarget_StillResolvable()
	{
		// Writes commonly target paths that don't exist yet; partial resolution is the
		// right behavior so WriteFile can be containment-checked before creating the file.
		var v = new PathContainmentValidator(_root);
		Assert.That(v.TryCanonicalize("does/not/exist/yet.txt", out var canonical), Is.True);
		Assert.That(canonical, Does.Contain("does"));
	}

	[Test]
	public void Symlink_PointingOutsideRoot_Rejected()
	{
		// Skip on platforms where we can't create symlinks without elevation.
		var outsideDir = Path.Combine(Path.GetTempPath(), "outside-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(outsideDir);
		try
		{
			var linkPath = Path.Combine(_root, "escape-link");
			try
			{
				Directory.CreateSymbolicLink(linkPath, outsideDir);
			}
			catch (UnauthorizedAccessException)
			{
				Assert.Ignore("Symlink creation requires elevation on this platform");
				return;
			}
			catch (IOException)
			{
				Assert.Ignore("Symlink creation not supported on this filesystem");
				return;
			}

			var v = new PathContainmentValidator(_root);
			Assert.That(v.TryCanonicalize("escape-link/secret.txt", out _), Is.False);
		}
		finally
		{
			try { Directory.Delete(outsideDir, recursive: true); } catch { }
		}
	}

	[Test]
	public void Symlink_PointingInsideRoot_Accepted()
	{
		var innerDir = Path.Combine(_root, "actual");
		Directory.CreateDirectory(innerDir);
		var linkPath = Path.Combine(_root, "alias");
		try
		{
			Directory.CreateSymbolicLink(linkPath, innerDir);
		}
		catch (UnauthorizedAccessException)
		{
			Assert.Ignore("Symlink creation requires elevation on this platform");
			return;
		}
		catch (IOException)
		{
			Assert.Ignore("Symlink creation not supported on this filesystem");
			return;
		}

		var v = new PathContainmentValidator(_root);
		Assert.That(v.TryCanonicalize("alias/file.txt", out _), Is.True);
	}

	[Test]
	public void CaseInsensitive_OnMacOrWindows_MixedCaseStillContained()
	{
		if (OperatingSystem.IsLinux())
		{
			Assert.Ignore("Linux filesystems are case-sensitive; this test asserts the macOS/Windows behavior");
			return;
		}

		var v = new PathContainmentValidator(_root);
		// Construct a path with a different case in the root segment than the canonical form.
		var differentCase = _root.ToUpperInvariant();
		var probe = Path.Combine(differentCase, "Foo.cs");
		Assert.That(v.TryCanonicalize(probe, out _), Is.True);
	}

	[Test]
	public void Constructor_RejectsEmptyRoot()
	{
		Assert.Throws<ArgumentException>(() => _ = new PathContainmentValidator(""));
		Assert.Throws<ArgumentException>(() => _ = new PathContainmentValidator("   "));
	}
}
