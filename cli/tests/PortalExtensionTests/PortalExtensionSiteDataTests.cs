using cli;
using cli.Services;
using cli.Services.PortalExtension;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace tests.PortalExtensionTests;

/// <summary>
/// Site data (Stage 2.1): the CLI doesn't check a site's presentation or its schemas. Both pass through to
/// metadata.json untouched and the portal checks them when it loads the extension, so a new presentation
/// (<c>host</c>) or schema keyword needs no CLI release. The checks that need no list of values stay.
/// </summary>
[NonParallelizable]
public class PortalExtensionSiteDataTests
{
	private string _root;

	[SetUp]
	public void SetUp()
	{
		_root = Path.Combine(Path.GetTempPath(), "pe-sitedata-tests", Path.GetRandomFileName());
		Directory.CreateDirectory(_root);
	}

	[TearDown]
	public void TearDown()
	{
		if (Directory.Exists(_root))
			Directory.Delete(_root, true);
	}

	private static PortalExtensionPackageProperties Package(params PortalExtensionSiteDeclaration[] sites) => new()
	{
		IsPortalExtension = true,
		Sites = sites.ToList(),
		Mounts = new List<PortalExtensionMountProperties>(),
	};

	[TestCase("host")]
	[TestCase("grid")]
	[TestCase("Stack")]
	[TestCase(null)]
	public void DeclarationChecks_AnyPresentation_PassesThrough(string presentation)
	{
		var properties = Package(new PortalExtensionSiteDeclaration { Id = "campaign-node", Presentation = presentation });

		Assert.That(PortalExtensionSiteDeclarationChecks.GetErrors(properties), Is.Empty);
	}

	[Test]
	public void DeclarationChecks_StillCatchIdsWithoutAList()
	{
		var errors = PortalExtensionSiteDeclarationChecks.GetErrors(Package(
			new PortalExtensionSiteDeclaration { Id = "x", Presentation = "host" },
			new PortalExtensionSiteDeclaration { Id = "x", Presentation = "host" },
			new PortalExtensionSiteDeclaration { Id = "#y", Presentation = "host" }));

		Assert.That(errors, Has.Count.EqualTo(2));
		Assert.That(errors[0], Does.Contain("more than once"));
		Assert.That(errors[1], Does.Contain("#y"));
	}

	[Test]
	public void HostSite_AndItsData_ReachMetadataJsonUnchecked()
	{
		// oneOf is outside the portal's schema subset: the CLI doesn't know or care, the portal reports it.
		var dir = Path.Combine(_root, "editor");
		Directory.CreateDirectory(dir);
		var path = Path.Combine(dir, "package.json");
		File.WriteAllText(path, @"{ ""name"": ""node-editor"", ""beamable"": {
			""portalExtension"": true,
			""sites"": [ {
				""id"": ""campaign-node"", ""presentation"": ""host"",
				""definitions"": { ""config"": { ""type"": ""object"" } },
				""static"": { ""type"": ""object"", ""required"": [ ""title"" ], ""properties"": { ""title"": { ""type"": ""string"" } } },
				""input"": { ""oneOf"": [ { ""$ref"": ""#/definitions/config"" } ] },
				""output"": { ""validate"": { ""args"": {}, ""result"": { ""type"": ""array"" }, ""timeoutMs"": 2000 } },
				""events"": { ""configChanged"": { ""payload"": {} } }
			} ],
			""mounts"": [ { ""siteIds"": [ ""extension-page"" ], ""page"": ""development/editor"" } ] } }");

		var def = ProjectContextUtil.FindPortalExtensionProjects(_root, new[] { path }).Single();
		Assert.That(PortalExtensionSiteDeclarationChecks.GetErrors(def.Properties), Is.Empty);

		var observer = new PortalExtensionObserver { ExtensionMetaData = def };
		observer.CreateMetaDataFile();

		var site = (JObject)JObject.Parse(File.ReadAllText(observer.MetadataPath))["Properties"]["sites"][0];
		Assert.That(site["presentation"]?.ToString(), Is.EqualTo("host"));
		Assert.That(site["definitions"]?["config"]?["type"]?.ToString(), Is.EqualTo("object"));
		Assert.That(site["static"]?["required"]?[0]?.ToString(), Is.EqualTo("title"));
		Assert.That(site["input"]?["oneOf"]?[0]?["$ref"]?.ToString(), Is.EqualTo("#/definitions/config"));
		Assert.That((int)site["output"]["validate"]["timeoutMs"], Is.EqualTo(2000));
		Assert.That(site["events"]?["configChanged"], Is.InstanceOf<JObject>());
	}
}
