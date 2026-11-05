using cli;
using cli.Services;
using cli.Services.Bundles;
using NUnit.Framework;
using System.Collections.Generic;

namespace tests.BundleTests;

/// <summary>
/// Unit tests for the strict per-file validation (<see cref="BundleWorkspace.ParseBundleFile"/>) and the
/// rules that need the workspace's projects (<see cref="BundleWorkspace.ValidateAgainstProjects"/>). Both
/// are pure enough to test without a workspace or CLI run.
/// </summary>
public class BundleWorkspaceValidationTests
{
	private static BundleConfigFile Parse(string json) => BundleWorkspace.ParseBundleFile("x.beam.bundle.json", json);

	private static BeamoServiceDefinition Project(string beamoId, BeamoProtocolType protocol, string scope = null) =>
		new BeamoServiceDefinition { BeamoId = beamoId, Protocol = protocol, ServiceScope = scope, ProjectPath = $"{beamoId}/{beamoId}.csproj" };

	private static BeamoLocalManifest Manifest(BeamoServiceDefinition[] definitions, Dictionary<string, string[]> storagesByService = null)
	{
		var manifest = new BeamoLocalManifest
		{
			ServiceDefinitions = new List<BeamoServiceDefinition>(definitions),
			HttpMicroserviceLocalProtocols = new BeamoLocalProtocolMap<HttpMicroserviceLocalProtocol>(),
		};
		foreach (var (service, storages) in storagesByService ?? new Dictionary<string, string[]>())
			manifest.HttpMicroserviceLocalProtocols[service] = new HttpMicroserviceLocalProtocol { StorageDependencyBeamIds = new List<string>(storages) };
		return manifest;
	}

	private const string ScoresBundle = @"{
		""components"": {
			""Scores"": { ""source"": true },
			""ScoresDb"": { ""source"": true },
			""Panel"": { ""site"": ""realm"" }
		},
		""componentGroups"": {
			""leaderboards"": { ""title"": ""Leaderboards"", ""components"": [""Scores"", ""ScoresDb"", ""Panel""], ""enabledByDefault"": true }
		},
		""dependencies"": { ""@beamable/core-ui"": { ""kind"": ""hard"", ""min"": 3 }, ""@acme/rewards"": { ""kind"": ""soft"", ""min"": 1, ""max"": 4 } }
	}";

	// ---- ParseBundleFile: valid ----

	[Test]
	public void Parse_ValidFile_ReadsAllFields()
	{
		var config = Parse(ScoresBundle);

		Assert.That(config.ComponentNames, Is.EqualTo(new[] { "Scores", "ScoresDb", "Panel" }));
		Assert.That(config.components["Scores"].source, Is.True);
		Assert.That(config.components["Scores"].site, Is.Null, "a site that isn't given comes from the project");
		Assert.That(config.components["Panel"].site, Is.EqualTo("realm"));
		Assert.That(config.componentGroups["leaderboards"].components, Is.EqualTo(new[] { "Scores", "ScoresDb", "Panel" }));
		Assert.That(config.componentGroups["leaderboards"].enabledByDefault, Is.True);
		Assert.That(config.dependencies["@beamable/core-ui"].IsHard, Is.True);
		Assert.That(config.dependencies["@beamable/core-ui"].max, Is.Null);
		Assert.That(config.dependencies["@acme/rewards"].IsHard, Is.False);
		Assert.That(config.dependencies["@acme/rewards"].max, Is.EqualTo(4));
	}

	[Test]
	public void Parse_EmptyObject_IsValidAndEmpty()
	{
		var config = Parse("{}");
		Assert.That(config.components, Is.Empty);
		Assert.That(config.componentGroups, Is.Empty);
		Assert.That(config.dependencies, Is.Empty);
	}

	[Test]
	public void Parse_ComponentGroupTitle_DefaultsToItsName()
	{
		var config = Parse(@"{ ""components"": { ""a"": {} }, ""componentGroups"": { ""main"": { ""components"": [""a""] } } }");
		Assert.That(config.componentGroups["main"].title, Is.EqualTo("main"));
	}

	// ---- ParseBundleFile: errors (each names the file) ----

	[TestCase("", TestName = "Parse_EmptyFile_Throws")]
	[TestCase("{ not json", TestName = "Parse_MalformedJson_Throws")]
	[TestCase("[]", TestName = "Parse_TopLevelArray_Throws")]
	[TestCase(@"{ ""scope"": ""realm"" }", TestName = "Parse_OldScopeKey_Throws")]
	[TestCase(@"{ ""bundleDependencies"": {} }", TestName = "Parse_OldDependenciesKey_Throws")]
	[TestCase(@"{ ""components"": [""a""] }", TestName = "Parse_OldComponentList_Throws")]
	[TestCase(@"{ ""components"": { ""a"": { ""site"": ""cluster"" } }, ""componentGroups"": { ""f"": { ""components"": [""a""] } } }", TestName = "Parse_BadSite_Throws")]
	[TestCase(@"{ ""components"": { ""a"": { ""sorce"": true } } }", TestName = "Parse_UnknownComponentKey_Throws")]
	[TestCase(@"{ ""components"": { ""a"": {} } }", TestName = "Parse_ComponentsWithoutGroups_Throws")]
	[TestCase(@"{ ""components"": { ""a"": {}, ""b"": {} }, ""componentGroups"": { ""f"": { ""components"": [""a""] } } }", TestName = "Parse_ComponentInNoGroup_Throws")]
	[TestCase(@"{ ""components"": { ""a"": {} }, ""componentGroups"": { ""f"": { ""components"": [""a"", ""ghost""] } } }", TestName = "Parse_GroupUnknownComponent_Throws")]
	[TestCase(@"{ ""components"": { ""a"": {} }, ""componentGroups"": { ""f"": { ""components"": [""a"", ""a""] } } }", TestName = "Parse_GroupRepeatsComponent_Throws")]
	[TestCase(@"{ ""components"": { ""a"": {} }, ""componentGroups"": { ""f"": { ""components"": [] }, ""g"": { ""components"": [""a""] } } }", TestName = "Parse_EmptyGroup_Throws")]
	[TestCase(@"{ ""dependencies"": { ""rewards"": { ""kind"": ""hard"", ""min"": 1 } } }", TestName = "Parse_DependencyNotFullName_Throws")]
	[TestCase(@"{ ""dependencies"": { ""@a/b"": { ""min"": 1 } } }", TestName = "Parse_DependencyWithoutKind_Throws")]
	[TestCase(@"{ ""dependencies"": { ""@a/b"": { ""kind"": ""optional"", ""min"": 1 } } }", TestName = "Parse_DependencyBadKind_Throws")]
	[TestCase(@"{ ""dependencies"": { ""@a/b"": { ""kind"": ""hard"", ""min"": -1 } } }", TestName = "Parse_NegativeMin_Throws")]
	[TestCase(@"{ ""dependencies"": { ""@a/b"": { ""kind"": ""hard"", ""min"": 5, ""max"": 1 } } }", TestName = "Parse_MaxLessThanMin_Throws")]
	public void Parse_Invalid_Throws(string json)
	{
		var ex = Assert.Throws<CliException>(() => Parse(json));
		Assert.That(ex.Message, Does.Contain("x.beam.bundle.json"), "the error must name the offending file");
	}

	// ---- ValidateAgainstProjects ----

	private static BeamoLocalManifest ScoresProjects(string panelScope = null) => Manifest(new[]
	{
		Project("Scores", BeamoProtocolType.HttpMicroservice, "zone"),
		Project("ScoresDb", BeamoProtocolType.EmbeddedMongoDb, "zone"),
		Project("Panel", BeamoProtocolType.PortalExtension, panelScope),
	}, new Dictionary<string, string[]> { ["Scores"] = new[] { "ScoresDb" } });

	[Test]
	public void Projects_MatchingBundle_IsValid()
	{
		Assert.DoesNotThrow(() => BundleWorkspace.ValidateAgainstProjects(ScoresProjects(), Parse(ScoresBundle)));
	}

	[Test]
	public void Projects_SiteComesFromTheProject_WhenNotDeclared()
	{
		var bundle = Parse(ScoresBundle);
		Assert.That(BundleWorkspace.SiteOf(ScoresProjects(), bundle, "Scores"), Is.EqualTo("zone"));
		Assert.That(BundleWorkspace.SiteOf(ScoresProjects(), bundle, "Panel"), Is.EqualTo("realm"));
	}

	[Test]
	public void Projects_DeclaredSiteDiffersFromTheProject_Throws()
	{
		var ex = Assert.Throws<CliException>(() => BundleWorkspace.ValidateAgainstProjects(ScoresProjects(panelScope: "zone"), Parse(ScoresBundle)));
		Assert.That(ex.Message, Does.Contain("Panel").And.Contain("zone-scoped"));
	}

	[Test]
	public void Projects_UnknownComponent_SuggestsANearMiss()
	{
		var bundle = Parse(ScoresBundle.Replace("\"Scores\"", "\"scores\""));
		var ex = Assert.Throws<CliException>(() => BundleWorkspace.ValidateAgainstProjects(ScoresProjects(), bundle));
		Assert.That(ex.Message, Does.Contain("Did you mean [Scores]"));
	}

	[Test]
	public void Projects_GroupWithServiceButNotItsStorage_Throws()
	{
		var bundle = Parse(@"{
			""components"": { ""Scores"": {}, ""ScoresDb"": {}, ""Panel"": {} },
			""componentGroups"": { ""a"": { ""components"": [""Scores"", ""Panel""] }, ""b"": { ""components"": [""ScoresDb""] } }
		}");
		var ex = Assert.Throws<CliException>(() => BundleWorkspace.ValidateAgainstProjects(ScoresProjects(), bundle));
		Assert.That(ex.Message, Does.Contain("Component group [a] has service [Scores] but not the storage it uses, [ScoresDb]"));
	}

	[Test]
	public void Projects_ServiceStorageNotInTheBundle_Throws()
	{
		var bundle = Parse(@"{ ""components"": { ""Scores"": {} }, ""componentGroups"": { ""a"": { ""components"": [""Scores""] } } }");
		var ex = Assert.Throws<CliException>(() => BundleWorkspace.ValidateAgainstProjects(ScoresProjects(), bundle));
		Assert.That(ex.Message, Does.Contain("uses storage [ScoresDb], which isn't in the bundle"));
	}

	[Test]
	public void Projects_SourceServiceWithClosedStorage_Throws()
	{
		var bundle = Parse(ScoresBundle.Replace(@"""ScoresDb"": { ""source"": true }", @"""ScoresDb"": {}"));
		var ex = Assert.Throws<CliException>(() => BundleWorkspace.ValidateAgainstProjects(ScoresProjects(), bundle));
		Assert.That(ex.Message, Does.Contain("its storage [ScoresDb] must too"));
	}

	[Test]
	public void Projects_ServiceAndStorageOnDifferentSites_Throws()
	{
		var manifest = Manifest(new[]
		{
			Project("Scores", BeamoProtocolType.HttpMicroservice, "zone"),
			Project("ScoresDb", BeamoProtocolType.EmbeddedMongoDb),
			Project("Panel", BeamoProtocolType.PortalExtension),
		}, new Dictionary<string, string[]> { ["Scores"] = new[] { "ScoresDb" } });
		var ex = Assert.Throws<CliException>(() => BundleWorkspace.ValidateAgainstProjects(manifest, Parse(ScoresBundle)));
		Assert.That(ex.Message, Does.Contain("must run on the same site"));
	}
}
