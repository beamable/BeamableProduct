using cli.Services;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace tests.PortalExtensionTests;

/// <summary>
///   An extension is allowed to depend on no microservice at all. When one such extension existed in a
///   workspace, <c>project generate pe-client</c> threw a NullReferenceException while scanning it — and
///   because that scan runs over EVERY extension for EVERY microservice, the throw took down the
///   post-build target for the entire microservice tier, so nothing started. These tests pin the two
///   shapes that produced the null: an omitted key in package.json, and the explicit <c>null</c> that the
///   generated assets/metadata.json writes for it.
/// </summary>
public class PortalExtensionDependencyTests
{
	private static PortalExtensionDef DefFrom(string propertiesJson) => new()
	{
		Name = "MyExt",
		Properties = JsonConvert.DeserializeObject<PortalExtensionPackageProperties>(propertiesJson)
	};

	[Test]
	public void AnOmittedDependencyKeyReadsAsAnEmptyList()
	{
		var def = DefFrom(@"{ ""version"": ""1.0.0"", ""portalExtension"": true }");

		Assert.That(def.MicroserviceDependencies, Is.Not.Null);
		Assert.That(def.MicroserviceDependencies, Is.Empty);
	}

	[Test]
	public void AnExplicitNullDependencyListReadsAsAnEmptyList()
	{
		// This is the shape the generated assets/metadata.json actually writes, and it is why a field
		// initializer on PortalExtensionPackageProperties is not sufficient: Json.NET assigns the null
		// over any initializer, so the normalisation has to live on the accessor.
		var def = DefFrom(@"{ ""portalExtension"": true, ""microserviceDependencies"": null }");

		Assert.That(def.MicroserviceDependencies, Is.Not.Null);
		Assert.That(def.MicroserviceDependencies, Is.Empty);
	}

	[Test]
	public void DeclaredDependenciesAreLeftAlone()
	{
		var def = DefFrom(@"{ ""microserviceDependencies"": [ ""CampaignService"" ] }");

		Assert.That(def.MicroserviceDependencies, Is.EqualTo(new List<string> { "CampaignService" }));
	}

	[Test]
	public void TheNormalisedListIsWrittenBackSoMutationsPersist()
	{
		// PortalExtensionAddDependencyCommand reads, .Add()s and re-serializes through this same
		// accessor. If the normalisation handed back a fresh throwaway list each time, the added
		// dependency would be silently dropped.
		var def = DefFrom(@"{ ""portalExtension"": true }");

		def.MicroserviceDependencies.Add("CampaignService");

		Assert.That(def.Properties.MicroserviceDependencies, Is.EqualTo(new List<string> { "CampaignService" }));
		Assert.That(def.MicroserviceDependencies, Is.EqualTo(new List<string> { "CampaignService" }));
	}

	[Test]
	public void ScanningExtensionsForAServiceSurvivesADependencyFreeExtension()
	{
		// The exact predicate GeneratePortalExtensionClientsCommand runs. Before the fix, the
		// dependency-free extension in this list threw for every microservice in the workspace.
		var extensions = new List<PortalExtensionDef>
		{
			DefFrom(@"{ ""microserviceDependencies"": [ ""CampaignService"" ] }"),
			DefFrom(@"{ ""microserviceDependencies"": null }"),
			DefFrom(@"{ ""portalExtension"": true }")
		};

		var matched = extensions.Where(e => e.MicroserviceDependencies.Contains("CampaignService")).ToList();

		Assert.That(matched.Count, Is.EqualTo(1), "only the extension that declares the dependency should match");
	}
}
