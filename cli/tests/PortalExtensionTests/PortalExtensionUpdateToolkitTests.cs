using cli.Portal;
using NUnit.Framework;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace tests.PortalExtensionTests;

public class PortalExtensionUpdateToolkitTests
{
	private string _root;

	[SetUp]
	public void SetUp()
	{
		_root = Path.Combine(Path.GetTempPath(), "pe-update-toolkit-tests", Path.GetRandomFileName());
		Directory.CreateDirectory(_root);
	}

	[TearDown]
	public void TearDown()
	{
		if (Directory.Exists(_root))
		{
			Directory.Delete(_root, true);
		}
	}

	private string WritePackageJson(JObject json)
	{
		var path = Path.Combine(_root, "package.json");
		File.WriteAllText(path, json.ToString());
		return path;
	}

	[Test]
	public void RewriteToolkitVersion_UpdatesDevDependencies()
	{
		var path = WritePackageJson(new JObject
		{
			["name"] = "MyExt",
			["devDependencies"] = new JObject { ["@beamable/portal-toolkit"] = "0.1.6" }
		});

		var changed = PortalExtensionUpdateToolkitCommand.RewriteToolkitVersion(path, "0.2.0", out var previous);

		Assert.That(changed, Is.True);
		Assert.That(previous, Is.EqualTo("0.1.6"));
		var root = JObject.Parse(File.ReadAllText(path));
		Assert.That(root["devDependencies"]?["@beamable/portal-toolkit"]?.ToString(), Is.EqualTo("0.2.0"));
	}

	[Test]
	public void RewriteToolkitVersion_UpdatesPeerDependencies()
	{
		var path = WritePackageJson(new JObject
		{
			["name"] = "MyLib",
			["peerDependencies"] = new JObject { ["@beamable/portal-toolkit"] = "0.1.10" }
		});

		var changed = PortalExtensionUpdateToolkitCommand.RewriteToolkitVersion(path, "0.2.0", out var previous);

		Assert.That(changed, Is.True);
		Assert.That(previous, Is.EqualTo("0.1.10"));
		var root = JObject.Parse(File.ReadAllText(path));
		Assert.That(root["peerDependencies"]?["@beamable/portal-toolkit"]?.ToString(), Is.EqualTo("0.2.0"));
	}

	[Test]
	public void RewriteToolkitVersion_UpdatesEveryBlockThatReferencesToolkit()
	{
		var path = WritePackageJson(new JObject
		{
			["name"] = "MyExt",
			["devDependencies"] = new JObject { ["@beamable/portal-toolkit"] = "0.1.6" },
			["peerDependencies"] = new JObject { ["@beamable/portal-toolkit"] = "0.1.6" }
		});

		var changed = PortalExtensionUpdateToolkitCommand.RewriteToolkitVersion(path, "0.2.0", out _);

		Assert.That(changed, Is.True);
		var root = JObject.Parse(File.ReadAllText(path));
		Assert.That(root["devDependencies"]?["@beamable/portal-toolkit"]?.ToString(), Is.EqualTo("0.2.0"));
		Assert.That(root["peerDependencies"]?["@beamable/portal-toolkit"]?.ToString(), Is.EqualTo("0.2.0"));
	}

	[Test]
	public void RewriteToolkitVersion_ReturnsFalse_AndLeavesFileUntouched_WhenToolkitMissing()
	{
		var original = new JObject
		{
			["name"] = "MyExt",
			["devDependencies"] = new JObject { ["typescript"] = "^5.6.3" }
		};
		var path = WritePackageJson(original);
		var before = File.ReadAllText(path);

		var changed = PortalExtensionUpdateToolkitCommand.RewriteToolkitVersion(path, "0.2.0", out var previous);

		Assert.That(changed, Is.False);
		Assert.That(previous, Is.Null);
		Assert.That(File.ReadAllText(path), Is.EqualTo(before), "the file must not be rewritten when there is no toolkit dependency");
	}

	[Test]
	public void LocateAllLibraries_FindsOnlyPortalExtensionLibs()
	{
		var libDir = Path.Combine(_root, "libs", "MyLib");
		var extDir = Path.Combine(_root, "extensions", "MyExt");
		Directory.CreateDirectory(libDir);
		Directory.CreateDirectory(extDir);

		File.WriteAllText(Path.Combine(libDir, "package.json"), new JObject
		{
			["name"] = "MyLib",
			["beamable"] = new JObject { ["portalExtensionLib"] = true }
		}.ToString());

		// An extension app (portalExtension, not portalExtensionLib) must not be returned as a library.
		File.WriteAllText(Path.Combine(extDir, "package.json"), new JObject
		{
			["name"] = "MyExt",
			["beamable"] = new JObject { ["portalExtension"] = true }
		}.ToString());

		var libs = PortalExtensionAddLibraryCommand.LocateAllLibraries(_root);

		Assert.That(libs.Select(l => l.Name), Is.EquivalentTo(new[] { "MyLib" }));
		Assert.That(libs.Single().PackageJsonPath, Is.EqualTo(Path.GetFullPath(Path.Combine(libDir, "package.json"))));
	}

	[Test]
	public void LocateAllLibraries_IgnoresCopiesInsideNodeModules()
	{
		var libDir = Path.Combine(_root, "extensions-libs", "MyLib");
		Directory.CreateDirectory(libDir);
		File.WriteAllText(Path.Combine(libDir, "package.json"), new JObject
		{
			["name"] = "MyLib",
			["beamable"] = new JObject { ["portalExtensionLib"] = true }
		}.ToString());

		// A file:-linked library is symlinked/copied into each consuming extension's node_modules. Those copies
		// carry the same portalExtensionLib marker and must NOT be discovered as additional libraries, or the
		// one real library would be processed once per consumer.
		var linkedCopyDir = Path.Combine(_root, "extensions", "MyExt", "node_modules", "MyLib");
		Directory.CreateDirectory(linkedCopyDir);
		File.WriteAllText(Path.Combine(linkedCopyDir, "package.json"), new JObject
		{
			["name"] = "MyLib",
			["beamable"] = new JObject { ["portalExtensionLib"] = true }
		}.ToString());

		var libs = PortalExtensionAddLibraryCommand.LocateAllLibraries(_root);

		Assert.That(libs, Has.Count.EqualTo(1), "a library copy inside node_modules must not be counted again");
		Assert.That(libs.Single().PackageJsonPath, Is.EqualTo(Path.GetFullPath(Path.Combine(libDir, "package.json"))));
	}
}