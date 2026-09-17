using Beamable.Server;
using cli;
using cli.Portal;
using cli.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace tests.PortalExtensionTests;

public class PortalExtensionLibraryTests
{
	private string _root;
	private string _extensionDir;
	private string _libDir;

	[SetUp]
	public void SetUp()
	{
		// The repair path logs a warning; ensure a logger exists in this bare test context.
		BeamableZLoggerProvider.SetLogger(NullLogger.Instance);

		_root = Path.Combine(Path.GetTempPath(), "pe-lib-tests", Path.GetRandomFileName());
		_extensionDir = Path.Combine(_root, "extensions", "MyExt");
		_libDir = Path.Combine(_root, "extensions-libs", "MyLib");
		Directory.CreateDirectory(_extensionDir);
		Directory.CreateDirectory(_libDir);

		// The library is discovered on demand by scanning extensions-libs, so it needs a valid package.json.
		var libJson = new JObject
		{
			["name"] = "MyLib",
			["beamable"] = new JObject { ["portalExtensionLib"] = true }
		};
		File.WriteAllText(Path.Combine(_libDir, "package.json"), libJson.ToString());
	}

	[TearDown]
	public void TearDown()
	{
		if (Directory.Exists(_root))
		{
			DeleteDirectoryTree(_root);
		}
	}

	// Tests may materialize directory links (junctions/symlinks); a plain recursive delete can follow a
	// reparse point into its target (deleting real content) or loop on cyclic junctions. Walk manually and
	// unlink reparse points instead of descending through them.
	private static void DeleteDirectoryTree(string dir)
	{
		var info = new DirectoryInfo(dir);
		if (info.LinkTarget != null || info.Attributes.HasFlag(FileAttributes.ReparsePoint))
		{
			Directory.Delete(dir); // removes the link, not its target
			return;
		}

		foreach (var sub in Directory.EnumerateDirectories(dir))
		{
			DeleteDirectoryTree(sub);
		}

		Directory.Delete(dir, true); // only real files/empty dirs remain at this point
	}

	private void WriteExtensionPackageJson(string libSpecifier)
	{
		var json = new JObject
		{
			["name"] = "MyExt",
			["dependencies"] = new JObject { ["MyLib"] = libSpecifier }
		};
		File.WriteAllText(Path.Combine(_extensionDir, "package.json"), json.ToString());
	}

	private PortalExtensionDef MakeExtensionDef() => new()
	{
		Name = "MyExt",
		AbsolutePath = _extensionDir,
	};

	[Test]
	public void ComputeFileSpecifier_ProducesRelativeFilePath()
	{
		var specifier = PortalExtensionAddLibraryCommand.ComputeFileSpecifier(_extensionDir, _libDir);

		Assert.That(specifier, Does.StartWith("file:"));
		Assert.That(specifier, Does.Contain("extensions-libs/MyLib"));
		Assert.That(specifier, Does.Not.Contain("\\"), "the specifier must use forward slashes");
	}

	[Test]
	public void LocateLibrary_FindsLibraryByName()
	{
		var found = PortalExtensionAddLibraryCommand.LocateLibrary(_root, "MyLib");
		Assert.That(found, Is.EqualTo(Path.GetFullPath(_libDir)));

		var missing = PortalExtensionAddLibraryCommand.LocateLibrary(_root, "DoesNotExist");
		Assert.That(missing, Is.Null);
	}

	[Test]
	public void ValidateAndRepair_LeavesCorrectPathUntouched()
	{
		var correct = PortalExtensionAddLibraryCommand.ComputeFileSpecifier(_extensionDir, _libDir);
		WriteExtensionPackageJson(correct);

		PortalExtensionAddLibraryCommand.ValidateAndRepairLibraryDependencies(MakeExtensionDef(), _root);

		var root = JObject.Parse(File.ReadAllText(Path.Combine(_extensionDir, "package.json")));
		Assert.That(root["dependencies"]?["MyLib"]?.ToString(), Is.EqualTo(correct));
	}

	[Test]
	public void ValidateAndRepair_FixesStalePath_WhenLibStillExists()
	{
		WriteExtensionPackageJson("file:../../wrong/place/MyLib");

		PortalExtensionAddLibraryCommand.ValidateAndRepairLibraryDependencies(MakeExtensionDef(), _root);

		var expected = PortalExtensionAddLibraryCommand.ComputeFileSpecifier(_extensionDir, _libDir);
		var root = JObject.Parse(File.ReadAllText(Path.Combine(_extensionDir, "package.json")));
		Assert.That(root["dependencies"]?["MyLib"]?.ToString(), Is.EqualTo(expected),
			"a stale file: path must be auto-repaired to the library's real location");
	}

	[Test]
	public void ValidateAndRepair_Throws_WhenLibMissingEverywhere()
	{
		WriteExtensionPackageJson("file:../../extensions-libs/MyLib");

		// Remove the lib dir so it can neither be located by name nor resolved by the recorded path.
		Directory.Delete(_libDir, true);

		Assert.That(
			() => PortalExtensionAddLibraryCommand.ValidateAndRepairLibraryDependencies(MakeExtensionDef(), _root),
			Throws.InstanceOf<CliException>());
	}

	// The peer-version check compares the versions the extension provides against each library's declared
	// peerDependency ranges. These cover the pure conflict-detection logic (DetectPeerVersionConflicts).

	private const string Toolkit = "@beamable/portal-toolkit";
	private const string React = "react";
	private static readonly string[] SharedPackages = { Toolkit, React };

	private static PortalExtensionAddLibraryCommand.LibraryPeerRequirements Lib(string name, params (string pkg, string range)[] peers)
	{
		var req = new PortalExtensionAddLibraryCommand.LibraryPeerRequirements { LibraryName = name };
		foreach (var (pkg, range) in peers)
		{
			req.PeerRanges[pkg] = range;
		}

		return req;
	}

	[Test]
	public void DetectConflicts_None_WhenVersionsMatch()
	{
		var provided = new Dictionary<string, string> { [Toolkit] = "0.2.0", [React] = "19.1.0" };
		var libs = new[] { Lib("MyLib", (Toolkit, "0.2.0"), (React, "^19.0.0")) };

		var conflicts = PortalExtensionAddLibraryCommand.DetectPeerVersionConflicts(provided, libs, SharedPackages);

		Assert.That(conflicts, Is.Empty);
	}

	[Test]
	public void DetectConflicts_Flags_WhenToolkitVersionOutsidePeerRange()
	{
		var provided = new Dictionary<string, string> { [Toolkit] = "0.2.0" };
		var libs = new[] { Lib("MyLib", (Toolkit, "0.3.0")) };

		var conflicts = PortalExtensionAddLibraryCommand.DetectPeerVersionConflicts(provided, libs, SharedPackages);

		Assert.That(conflicts, Has.Count.EqualTo(1));
		Assert.That(conflicts[0], Does.Contain("MyLib").And.Contain(Toolkit).And.Contain("0.3.0").And.Contain("0.2.0"));
	}

	[Test]
	public void DetectConflicts_Flags_WhenReactVersionOutsideCaretRange()
	{
		var provided = new Dictionary<string, string> { [React] = "18.3.0" };
		var libs = new[] { Lib("MyLib", (React, "^19.0.0")) };

		var conflicts = PortalExtensionAddLibraryCommand.DetectPeerVersionConflicts(provided, libs, SharedPackages);

		Assert.That(conflicts, Has.Count.EqualTo(1));
		Assert.That(conflicts[0], Does.Contain(React));
	}

	[Test]
	public void DetectConflicts_FailsOpen_WhenProvidedVersionUnknown()
	{
		// The extension doesn't provide a resolvable version (e.g. not installed) -> never block.
		var provided = new Dictionary<string, string>();
		var libs = new[] { Lib("MyLib", (Toolkit, "0.3.0")) };

		var conflicts = PortalExtensionAddLibraryCommand.DetectPeerVersionConflicts(provided, libs, SharedPackages);

		Assert.That(conflicts, Is.Empty);
	}

	[Test]
	public void DetectConflicts_FailsOpen_WhenRangeIsUnparseable()
	{
		// A workspace/url/git range this matcher doesn't understand must not block the user.
		var provided = new Dictionary<string, string> { [Toolkit] = "0.2.0" };
		var libs = new[] { Lib("MyLib", (Toolkit, "workspace:*")) };

		var conflicts = PortalExtensionAddLibraryCommand.DetectPeerVersionConflicts(provided, libs, SharedPackages);

		Assert.That(conflicts, Is.Empty);
	}

	[Test]
	public void DetectConflicts_IgnoresPeersOutsideTheSharedSet()
	{
		// A library may peer-depend on packages we don't police; those must never produce a conflict.
		var provided = new Dictionary<string, string> { [Toolkit] = "0.2.0" };
		var libs = new[] { Lib("MyLib", (Toolkit, "0.2.0"), ("some-other-pkg", "^1.0.0")) };

		var conflicts = PortalExtensionAddLibraryCommand.DetectPeerVersionConflicts(provided, libs, SharedPackages);

		Assert.That(conflicts, Is.Empty);
	}

	[Test]
	public void ReadInstalledPackageVersion_ReadsScopedPackageVersion()
	{
		var toolkitDir = Path.Combine(_extensionDir, "node_modules", "@beamable", "portal-toolkit");
		Directory.CreateDirectory(toolkitDir);
		File.WriteAllText(Path.Combine(toolkitDir, "package.json"),
			new JObject { ["name"] = Toolkit, ["version"] = "0.2.0" }.ToString());

		var version = PortalExtensionAddLibraryCommand.ReadInstalledPackageVersion(_extensionDir, Toolkit);

		Assert.That(version, Is.EqualTo("0.2.0"));
	}

	[Test]
	public void ReadInstalledPackageVersion_NullWhenNotInstalled()
	{
		Assert.That(PortalExtensionAddLibraryCommand.ReadInstalledPackageVersion(_extensionDir, Toolkit), Is.Null);
	}

	// LinkTransitiveLibraryDependencies materializes a library's own library deps into its node_modules so a
	// library->library import resolves at build time (npm won't install a file:-linked library's deps for us).

	private string CreateLibrary(string name)
	{
		var dir = Path.Combine(_root, "extensions-libs", name);
		Directory.CreateDirectory(dir);
		File.WriteAllText(Path.Combine(dir, "package.json"), new JObject
		{
			["name"] = name,
			["beamable"] = new JObject { ["portalExtensionLib"] = true }
		}.ToString());
		return dir;
	}

	private static void SetLibraryDeps(string libDir, string block, JObject deps)
	{
		var path = Path.Combine(libDir, "package.json");
		var root = JObject.Parse(File.ReadAllText(path));
		root[block] = deps;
		File.WriteAllText(path, root.ToString());
	}

	[Test]
	public void LinkTransitive_LinksLibrarysOwnLibraryDevDependency()
	{
		// MyExt -> MyLib (devDependency) -> MyDepLib
		var depLibDir = CreateLibrary("MyDepLib");
		SetLibraryDeps(_libDir, "devDependencies", new JObject { ["MyDepLib"] = "file:../MyDepLib" });
		WriteExtensionPackageJson(PortalExtensionAddLibraryCommand.ComputeFileSpecifier(_extensionDir, _libDir));

		PortalExtensionAddLibraryCommand.LinkTransitiveLibraryDependencies(MakeExtensionDef(), _root);

		var linkPath = Path.Combine(_libDir, "node_modules", "MyDepLib");
		Assert.That(new DirectoryInfo(linkPath).LinkTarget, Is.Not.Null,
			"MyDepLib must be materialized as a link (not a copied directory) inside MyLib's node_modules");
		Assert.That(File.Exists(Path.Combine(linkPath, "package.json")), Is.True,
			"the link must resolve to MyDepLib's real directory");
		Assert.That(Path.GetFullPath(depLibDir), Is.Not.Null); // depLibDir is the real target the link resolves to
	}

	[Test]
	public void LinkTransitive_IgnoresNonLibraryDependencies()
	{
		// A dep that isn't a portal-extension library (e.g. react) must never be linked.
		SetLibraryDeps(_libDir, "devDependencies", new JObject { ["react"] = "^19.0.0" });
		WriteExtensionPackageJson(PortalExtensionAddLibraryCommand.ComputeFileSpecifier(_extensionDir, _libDir));

		PortalExtensionAddLibraryCommand.LinkTransitiveLibraryDependencies(MakeExtensionDef(), _root);

		Assert.That(Directory.Exists(Path.Combine(_libDir, "node_modules", "react")), Is.False);
	}

	[Test]
	public void LinkTransitive_IsCycleSafe()
	{
		// MyLib <-> MyDepLib depend on each other; the walk must terminate and link both directions.
		var depLibDir = CreateLibrary("MyDepLib");
		SetLibraryDeps(_libDir, "devDependencies", new JObject { ["MyDepLib"] = "file:../MyDepLib" });
		SetLibraryDeps(depLibDir, "devDependencies", new JObject { ["MyLib"] = "file:../MyLib" });
		WriteExtensionPackageJson(PortalExtensionAddLibraryCommand.ComputeFileSpecifier(_extensionDir, _libDir));

		PortalExtensionAddLibraryCommand.LinkTransitiveLibraryDependencies(MakeExtensionDef(), _root);

		Assert.That(File.Exists(Path.Combine(_libDir, "node_modules", "MyDepLib", "package.json")), Is.True);
		Assert.That(File.Exists(Path.Combine(depLibDir, "node_modules", "MyLib", "package.json")), Is.True);
	}
}
