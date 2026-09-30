using cli;
using cli.Services;
using cli.Services.Bundles;
using NUnit.Framework;
using System.Collections.Generic;

namespace tests.BundleTests;

/// <summary>
/// Unit tests for the strict per-file structure validation (<see cref="BundleWorkspace.ParseBundleFile"/>)
/// and the manifest-aware single-scope guard (<see cref="BundleWorkspace.ValidateComponentScope"/>). Both
/// are pure enough to test without a workspace or CLI run.
/// </summary>
public class BundleWorkspaceValidationTests
{
	private static BundleConfigFile Parse(string json) => BundleWorkspace.ParseBundleFile("x.beam.bundle.json", json);

	private static BeamoLocalManifest ManifestWith(params BeamoServiceDefinition[] definitions) =>
		new BeamoLocalManifest { ServiceDefinitions = new List<BeamoServiceDefinition>(definitions) };

	private static BeamoServiceDefinition Component(string beamoId, string scope) =>
		new BeamoServiceDefinition { BeamoId = beamoId, ServiceScope = scope };

	// ---- ParseBundleFile: valid ----

	[Test]
	public void Parse_ValidFile_ReadsAllFields()
	{
		var config = Parse(@"{
			""scope"": ""realm"",
			""components"": [""a"", ""b""],
			""bundleDependencies"": { ""@ns/dep"": { ""min"": 1, ""max"": 5 }, ""@ns/open"": { ""min"": 2 } }
		}");

		Assert.That(config.scope, Is.EqualTo("realm"));
		Assert.That(config.IsZoneScoped, Is.False);
		Assert.That(config.components, Is.EqualTo(new[] { "a", "b" }));
		Assert.That(config.bundleDependencies["@ns/dep"].min, Is.EqualTo(1));
		Assert.That(config.bundleDependencies["@ns/dep"].max, Is.EqualTo(5));
		Assert.That(config.bundleDependencies["@ns/open"].min, Is.EqualTo(2));
		Assert.That(config.bundleDependencies["@ns/open"].max, Is.Null);
	}

	[Test]
	public void Parse_EmptyObject_IsValidAndEmpty()
	{
		var config = Parse("{}");
		Assert.That(config.scope, Is.Null);
		Assert.That(config.components, Is.Empty);
		Assert.That(config.bundleDependencies, Is.Empty);
	}

	[Test]
	public void Parse_ScopeZone_SetsIsZoneScoped()
	{
		Assert.That(Parse(@"{ ""scope"": ""ZONE"" }").IsZoneScoped, Is.True, "scope value is case-insensitive on read");
	}

	// ---- ParseBundleFile: structural errors (each names the file) ----

	[TestCase("", TestName = "Parse_EmptyFile_Throws")]
	[TestCase("   ", TestName = "Parse_WhitespaceFile_Throws")]
	[TestCase("{ not json", TestName = "Parse_MalformedJson_Throws")]
	[TestCase("[]", TestName = "Parse_TopLevelArray_Throws")]
	[TestCase(@"{ ""component"": [] }", TestName = "Parse_UnknownTopLevelKey_Throws")]
	[TestCase(@"{ ""scope"": ""cluster"" }", TestName = "Parse_BadScopeValue_Throws")]
	[TestCase(@"{ ""scope"": 1 }", TestName = "Parse_ScopeNotString_Throws")]
	[TestCase(@"{ ""components"": {} }", TestName = "Parse_ComponentsNotArray_Throws")]
	[TestCase(@"{ ""components"": [""""] }", TestName = "Parse_ComponentEmptyString_Throws")]
	[TestCase(@"{ ""components"": [""a"", ""a""] }", TestName = "Parse_DuplicateComponent_Throws")]
	[TestCase(@"{ ""bundleDependencies"": [] }", TestName = "Parse_DependenciesNotObject_Throws")]
	[TestCase(@"{ ""bundleDependencies"": { ""d"": 1 } }", TestName = "Parse_DependencyValueNotObject_Throws")]
	[TestCase(@"{ ""bundleDependencies"": { ""d"": { ""minimum"": 1 } } }", TestName = "Parse_DependencyUnknownKey_Throws")]
	[TestCase(@"{ ""bundleDependencies"": { ""d"": { ""min"": -1 } } }", TestName = "Parse_NegativeMin_Throws")]
	[TestCase(@"{ ""bundleDependencies"": { ""d"": { ""min"": 5, ""max"": 1 } } }", TestName = "Parse_MaxLessThanMin_Throws")]
	public void Parse_Invalid_Throws(string json)
	{
		var ex = Assert.Throws<CliException>(() => Parse(json));
		Assert.That(ex.Message, Does.Contain("x.beam.bundle.json"), "the error must name the offending file");
	}

	// ---- ValidateComponentScope ----

	[Test]
	public void Scope_AllRealm_Ok()
	{
		var manifest = ManifestWith(Component("a", null), Component("b", ""));
		var bundle = new BundleConfigFile { name = "x", components = { "a", "b" } };
		Assert.DoesNotThrow(() => BundleWorkspace.ValidateComponentScope(manifest, bundle));
	}

	[Test]
	public void Scope_AllZone_Ok()
	{
		var manifest = ManifestWith(Component("a", "zone"), Component("b", "zone"));
		var bundle = new BundleConfigFile { name = "x", scope = "zone", components = { "a", "b" } };
		Assert.DoesNotThrow(() => BundleWorkspace.ValidateComponentScope(manifest, bundle));
	}

	[Test]
	public void Scope_Mixed_Throws()
	{
		var manifest = ManifestWith(Component("a", null), Component("b", "zone"));
		var bundle = new BundleConfigFile { name = "x", components = { "a", "b" } };
		var ex = Assert.Throws<CliException>(() => BundleWorkspace.ValidateComponentScope(manifest, bundle));
		Assert.That(ex.Message, Does.Contain("realm").And.Contain("zone"));
	}

	[Test]
	public void Scope_DeclaredZone_WithRealmComponent_Throws()
	{
		var manifest = ManifestWith(Component("a", null));
		var bundle = new BundleConfigFile { name = "x", scope = "zone", components = { "a" } };
		Assert.Throws<CliException>(() => BundleWorkspace.ValidateComponentScope(manifest, bundle));
	}

	[Test]
	public void Scope_DeclaredRealm_WithZoneComponent_Throws()
	{
		var manifest = ManifestWith(Component("a", "zone"));
		var bundle = new BundleConfigFile { name = "x", scope = "realm", components = { "a" } };
		Assert.Throws<CliException>(() => BundleWorkspace.ValidateComponentScope(manifest, bundle));
	}

	[Test]
	public void Scope_ComponentNotInManifest_IsIgnored()
	{
		var manifest = ManifestWith(Component("a", "zone"));
		var bundle = new BundleConfigFile { name = "x", components = { "a", "ghost" } };
		// 'ghost' isn't resolvable here (existence is validated elsewhere); scope must not throw on it.
		Assert.DoesNotThrow(() => BundleWorkspace.ValidateComponentScope(manifest, bundle));
	}
}
