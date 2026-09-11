using cli.Services;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace tests.DiscoveryTests;

/// <summary>
/// Unit tests for <see cref="DiscoveryService.TryResolveBroadcastDefinition"/>, the reconciliation that
/// lets host discovery map the name a service broadcasts back to its local manifest definition. This is
/// what makes a zone-scoped portal extension visible to <c>beam project ps</c> / stoppable by
/// <c>beam project stop</c>: its backing server broadcasts a synthetic runtime name, and without this
/// resolution the <c>IsZoneScoped</c> escape hatch in host discovery never engages.
/// </summary>
public class DiscoveryServiceTests
{
	private static BeamoLocalManifest ManifestWith(params BeamoServiceDefinition[] definitions)
	{
		return new BeamoLocalManifest
		{
			ServiceDefinitions = new List<BeamoServiceDefinition>(definitions),
			HttpMicroserviceLocalProtocols = new BeamoLocalProtocolMap<HttpMicroserviceLocalProtocol>(),
			EmbeddedMongoDbLocalProtocols = new BeamoLocalProtocolMap<EmbeddedMongoDbLocalProtocol>(),
		};
	}

	private static string BroadcastName(string beamoId) =>
		$"BeamPortalExtension_{beamoId}_{Guid.NewGuid()}";

	[Test]
	public void ResolvesMicroservice_ByExactName()
	{
		var manifest = ManifestWith(new BeamoServiceDefinition
		{
			BeamoId = "MyService",
			Protocol = BeamoProtocolType.HttpMicroservice,
		});

		var found = DiscoveryService.TryResolveBroadcastDefinition(manifest, "MyService", out var def, out var beamoId);

		Assert.That(found, Is.True, "a microservice broadcasts under its beamoId and must resolve directly");
		Assert.That(beamoId, Is.EqualTo("MyService"));
		Assert.That(def, Is.Not.Null);
	}

	[Test]
	public void ResolvesRealmPortalExtension_FromMangledBroadcastName()
	{
		var manifest = ManifestWith(new BeamoServiceDefinition
		{
			BeamoId = "MyExt",
			Protocol = BeamoProtocolType.PortalExtension,
			ServiceScope = null,
		});

		var found = DiscoveryService.TryResolveBroadcastDefinition(manifest, BroadcastName("MyExt"), out var def, out var beamoId);

		Assert.That(found, Is.True, "a portal extension's synthetic runtime name must resolve back to its beamoId");
		Assert.That(beamoId, Is.EqualTo("MyExt"), "the surfaced name must be the beamoId, not the mangled runtime name");
		Assert.That(def, Is.Not.Null);
		Assert.That(def.IsZoneScoped, Is.False, "a realm extension is not zone-scoped");
	}

	[Test]
	public void ResolvesZonePortalExtension_AndReportsZoneScoped()
	{
		// This is the regression case: a zone-scoped portal extension broadcasts a mangled runtime name and a
		// zone id in its pid slot. Host discovery only keeps it (past the realm-pid filter) if this resolution
		// finds the definition and surfaces IsZoneScoped.
		var manifest = ManifestWith(new BeamoServiceDefinition
		{
			BeamoId = "MyZoneExt",
			Protocol = BeamoProtocolType.PortalExtension,
			ServiceScope = "zone",
		});

		var found = DiscoveryService.TryResolveBroadcastDefinition(manifest, BroadcastName("MyZoneExt"), out var def, out var beamoId);

		Assert.That(found, Is.True, "a zone portal extension must resolve so host discovery can keep it");
		Assert.That(beamoId, Is.EqualTo("MyZoneExt"));
		Assert.That(def, Is.Not.Null);
		Assert.That(def.IsZoneScoped, Is.True, "the resolved definition must report zone scope so the realm-pid filter is bypassed");
	}

	[Test]
	public void PicksCorrectExtension_WhenMultipleExist()
	{
		var manifest = ManifestWith(
			new BeamoServiceDefinition { BeamoId = "ExtA", Protocol = BeamoProtocolType.PortalExtension, ServiceScope = "zone" },
			new BeamoServiceDefinition { BeamoId = "ExtB", Protocol = BeamoProtocolType.PortalExtension, ServiceScope = null });

		var found = DiscoveryService.TryResolveBroadcastDefinition(manifest, BroadcastName("ExtB"), out var def, out var beamoId);

		Assert.That(found, Is.True);
		Assert.That(beamoId, Is.EqualTo("ExtB"), "resolution must match the extension embedded in the broadcast name");
		Assert.That(def.IsZoneScoped, Is.False);
	}

	[Test]
	public void ReturnsFalse_ForUnknownName()
	{
		var manifest = ManifestWith(new BeamoServiceDefinition
		{
			BeamoId = "Known",
			Protocol = BeamoProtocolType.HttpMicroservice,
		});

		var found = DiscoveryService.TryResolveBroadcastDefinition(manifest, BroadcastName("Unknown"), out var def, out var beamoId);

		Assert.That(found, Is.False, "a broadcast that matches no local definition must not resolve");
		Assert.That(def, Is.Null);
		Assert.That(beamoId, Does.StartWith("BeamPortalExtension_Unknown_"),
			"when unresolved, the raw broadcast name is passed through unchanged");
	}
}
