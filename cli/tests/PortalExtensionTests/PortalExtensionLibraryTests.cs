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

	private BeamoLocalManifest MakeManifestWithLib() => new()
	{
		ServiceDefinitions = new List<BeamoServiceDefinition>
		{
			new()
			{
				BeamoId = "MyLib",
				Protocol = BeamoProtocolType.PortalExtensionLib,
				PortalExtensionLibDefinition = new PortalExtensionLibDef
				{
					Name = "MyLib",
					AbsolutePath = _libDir,
				}
			}
		}
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
	public void ValidateAndRepair_LeavesCorrectPathUntouched()
	{
		var correct = PortalExtensionAddLibraryCommand.ComputeFileSpecifier(_extensionDir, _libDir);
		WriteExtensionPackageJson(correct);

		PortalExtensionAddLibraryCommand.ValidateAndRepairLibraryDependencies(MakeExtensionDef(), MakeManifestWithLib());

		var root = JObject.Parse(File.ReadAllText(Path.Combine(_extensionDir, "package.json")));
		Assert.That(root["dependencies"]?["MyLib"]?.ToString(), Is.EqualTo(correct));
	}

	[Test]
	public void ValidateAndRepair_FixesStalePath_WhenLibStillInManifest()
	{
		WriteExtensionPackageJson("file:../../wrong/place/MyLib");

		PortalExtensionAddLibraryCommand.ValidateAndRepairLibraryDependencies(MakeExtensionDef(), MakeManifestWithLib());

		var expected = PortalExtensionAddLibraryCommand.ComputeFileSpecifier(_extensionDir, _libDir);
		var root = JObject.Parse(File.ReadAllText(Path.Combine(_extensionDir, "package.json")));
		Assert.That(root["dependencies"]?["MyLib"]?.ToString(), Is.EqualTo(expected),
			"a stale file: path must be auto-repaired to the library's real location");
	}

	[Test]
	public void ValidateAndRepair_Throws_WhenLibMissingEverywhere()
	{
		WriteExtensionPackageJson("file:../../extensions-libs/MyLib");

		var manifestWithoutLib = new BeamoLocalManifest { ServiceDefinitions = new List<BeamoServiceDefinition>() };

		// Remove the lib dir so the recorded path no longer resolves.
		Directory.Delete(_libDir, true);

		Assert.That(
			() => PortalExtensionAddLibraryCommand.ValidateAndRepairLibraryDependencies(MakeExtensionDef(), manifestWithoutLib),
			Throws.InstanceOf<CliException>());
	}
}
