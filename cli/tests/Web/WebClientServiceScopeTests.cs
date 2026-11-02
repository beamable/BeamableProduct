using cli.Portal;
using cli.Services;
using cli.Services.Web;
using Microsoft.OpenApi.Models;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace tests.Web;

/// <summary>
///   A generated web client must bind to the SDK type that routes to the scope of the SERVICE it calls:
///   <c>BeamBase</c> (Beam/BeamServer, cid.pid) for a realm service, <c>BeamZoneSdk</c> (cid.zid) for a zone
///   service. It used to follow the scope of the EXTENSION instead, so a zone extension's client for a realm
///   service was typed <c>BeamZoneSdk</c>: it rejected the <c>BeamServer</c> that <c>assumeRealm(pid)</c>
///   returns, and accepted the zone SDK, which routes the call to the zid where the service does not run.
/// </summary>
public class WebClientServiceScopeTests
{
	private string _dir;

	[SetUp]
	public void SetUp()
	{
		_dir = Path.Combine(Path.GetTempPath(), "beam-webclient-scope-" + Path.GetRandomFileName());
		Directory.CreateDirectory(_dir);
	}

	[TearDown]
	public void TearDown()
	{
		if (Directory.Exists(_dir))
		{
			Directory.Delete(_dir, true);
		}
	}

	private static OpenApiDocument Doc(string serviceName) => new()
	{
		Info = new OpenApiInfo { Title = serviceName },
		Paths = new OpenApiPaths()
	};

	private static BeamoServiceDefinition Service(string beamoId, string scope) => new()
	{
		BeamoId = beamoId,
		Protocol = BeamoProtocolType.HttpMicroservice,
		ServiceScope = scope
	};

	private static void AssertBoundTo(string clientSource, string expected, string other)
	{
		StringAssert.Contains($"type {expected}", clientSource);
		StringAssert.Contains($"beam: {expected}", clientSource);
		StringAssert.Contains($"interface {expected}", clientSource);
		StringAssert.DoesNotContain(other, clientSource);
	}

	[Test]
	public void ARealmServiceBindsToBeamBase()
	{
		Assert.That(WebClientCodeGenerator.AugmentTypeFor(Service("RealmSvc", null)), Is.EqualTo(WebClientAugmentType.BeamBase));
		Assert.That(WebClientCodeGenerator.AugmentTypeFor(Service("RealmSvc", "realm")), Is.EqualTo(WebClientAugmentType.BeamBase));
	}

	[Test]
	public void AZoneServiceBindsToBeamZoneSdk()
	{
		Assert.That(WebClientCodeGenerator.AugmentTypeFor(Service("ZoneSvc", "zone")), Is.EqualTo(WebClientAugmentType.BeamZoneSdk));
		Assert.That(WebClientCodeGenerator.AugmentTypeFor(Service("ZoneSvc", " Zone ")), Is.EqualTo(WebClientAugmentType.BeamZoneSdk));
	}

	[Test]
	public void AnUnknownServiceFallsBackToBeamBase()
	{
		Assert.That(WebClientCodeGenerator.AugmentTypeFor(null), Is.EqualTo(WebClientAugmentType.BeamBase));
	}

	[Test]
	public void TheGeneratedClientUsesTheAugmentTypeEverywhere()
	{
		var realmFile = new WebClientCodeGenerator(Doc("RealmSvc"), "ts", WebClientAugmentType.BeamBase).GenerateClientCode(_dir);
		AssertBoundTo(File.ReadAllText(realmFile), "BeamBase", "BeamZoneSdk");

		var zoneFile = new WebClientCodeGenerator(Doc("ZoneSvc"), "ts", WebClientAugmentType.BeamZoneSdk).GenerateClientCode(_dir);
		AssertBoundTo(File.ReadAllText(zoneFile), "BeamZoneSdk", "BeamBase");
	}

	[Test]
	public void AZoneExtensionGetsAClientPerServiceScope()
	{
		// The agentic-portal case: a zone extension that calls a zone service on `context.beam` and a realm
		// service through `assumeRealm(pid)`.
		File.WriteAllText(Path.Combine(_dir, "package.json"), @"{
  ""name"": ""zone-ext"",
  ""beamable"": {
    ""portalExtension"": true,
    ""serviceScope"": ""zone"",
    ""microserviceDependencies"": [ ""RealmSvc"", ""ZoneSvc"" ]
  }
}");

		var manifest = new BeamoLocalManifest
		{
			ServiceDefinitions = new List<BeamoServiceDefinition> { Service("RealmSvc", null), Service("ZoneSvc", "zone") },
			HttpMicroserviceLocalProtocols = new BeamoLocalProtocolMap<HttpMicroserviceLocalProtocol>
			{
				["RealmSvc"] = new HttpMicroserviceLocalProtocol { OpenApiDoc = Doc("RealmSvc") },
				["ZoneSvc"] = new HttpMicroserviceLocalProtocol { OpenApiDoc = Doc("ZoneSvc") }
			}
		};

		PortalExtensionAddDependencyCommand.GenerateDependenciesClients(_dir, manifest);

		var clients = Path.Combine(_dir, "beamable", "clients");
		AssertBoundTo(File.ReadAllText(Path.Combine(clients, "RealmSvcClient.ts")), "BeamBase", "BeamZoneSdk");
		AssertBoundTo(File.ReadAllText(Path.Combine(clients, "ZoneSvcClient.ts")), "BeamZoneSdk", "BeamBase");
	}
}
