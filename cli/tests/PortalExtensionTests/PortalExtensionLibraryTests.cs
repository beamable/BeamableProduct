using Beamable.Server;
using cli;
using cli.Portal;
using cli.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
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
			Directory.Delete(_root, true);
		}
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

	// The peer-version check delegates resolution to npm (a `--install-links --strict-peer-deps` dry-run);
	// these cover the pure scoping logic that decides whether npm's failure actually concerns the toolkit.

	[Test]
	public void HasToolkitVersionConflict_False_WhenNpmSucceeds()
	{
		Assert.That(PortalExtensionAddLibraryCommand.HasToolkitVersionConflict(0, "up to date"), Is.False);
	}

	[Test]
	public void HasToolkitVersionConflict_False_WhenConflictIsUnrelated()
	{
		const string reactNoise =
			"npm error ERESOLVE unable to resolve dependency tree\n" +
			"npm error peer react@\"^19.2.0\" from react-dom@19.2.7";

		Assert.That(PortalExtensionAddLibraryCommand.HasToolkitVersionConflict(1, reactNoise), Is.False);
	}

	[Test]
	public void HasToolkitVersionConflict_True_WhenToolkitConflicts()
	{
		const string toolkitConflict =
			"npm error ERESOLVE unable to resolve dependency tree\n" +
			"npm error Could not resolve dependency:\n" +
			"npm error peer @beamable/portal-toolkit@\"0.1.10\" from MyLib@1.0.0";

		Assert.That(PortalExtensionAddLibraryCommand.HasToolkitVersionConflict(1, toolkitConflict), Is.True);
	}
}
