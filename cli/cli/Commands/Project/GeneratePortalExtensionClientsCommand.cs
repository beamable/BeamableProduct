using Beamable.Server;
using cli.Dotnet;
using cli.Services;
using cli.Services.Web;
using Microsoft.OpenApi.Models;
using System.Collections.Concurrent;
using System.CommandLine;

namespace cli.Commands.Project;

public class GeneratePortalExtensionClientsCommandArgs : CommandArgs
{
	public List<string> services = new List<string>();
	public List<string> withServiceTags = new List<string>();
	public List<string> withoutServiceTags = new List<string>();
}


public class GeneratePortalExtensionClientsCommand : AppCommand<GeneratePortalExtensionClientsCommandArgs>
{
	private static readonly ConcurrentDictionary<string, object> _pathLocks = new();
	public override bool IsForInternalUse => true;

	public GeneratePortalExtensionClientsCommand() : base("portal-extension-clients", "Generates portal extension clients for a specified microservice (or for all if none is passed)")
	{
		AddAlias("pe-client");
	}

	public override void Configure()
	{
		ProjectCommand.AddIdsOption(this, (args, i) => args.services = i);
		ProjectCommand.AddServiceTagsOption(this,
			bindWithTags: (args, i) => args.withServiceTags = i,
			bindWithoutTags: (args, i) => args.withoutServiceTags = i);
	}

	public override async Task Handle(GeneratePortalExtensionClientsCommandArgs args)
	{
		ProjectCommand.FinalizeServicesArg(args,
			withTags: args.withServiceTags,
			withoutTags: args.withoutServiceTags,
			includeStorage: false,
			ref args.services);

		var allServices = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions;
		var allExtensions = allServices.Where((s) => s.Protocol == BeamoProtocolType.PortalExtension).ToList();

		var micros = allServices
			.Where(s => s.Protocol == BeamoProtocolType.HttpMicroservice && args.services.Contains(s.BeamoId))
			.ToList();

		var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 8 };

		await Parallel.ForEachAsync(micros, parallelOptions, async (ms, cancellationToken) =>
		{
			// Null-safe on the definition as well as the list: this predicate runs against EVERY
			// extension in the workspace for EVERY microservice, so a single malformed extension
			// throws here for all of them, fails the post-build `generate pe-client` target, and
			// stops the entire microservice tier from starting.
			var extensionsToUpdate = allExtensions
				.Where(e => e.PortalExtensionDefinition?.MicroserviceDependencies.Contains(ms.BeamoId) == true)
				.ToList();

			if (extensionsToUpdate.Count == 0) return;

			(bool hasOpenApiDocument, OpenApiDocument document) = await BeamoServiceDefinition.TryGetOpenApiDocument(ms.OpenApiPath);

			if (!hasOpenApiDocument)
			{
				Log.Warning("Could not find any open API document: {path} for service {beamoId}", new object[] { ms.OpenApiPath, ms.BeamoId });
				return;
			}

			// The client binds to the SDK type that routes to the service's own scope, whatever the
			// extension's scope is (a zone extension calls a realm service via assumeRealm's BeamBase).
			var augmentType = WebClientCodeGenerator.AugmentTypeFor(ms);

			foreach (var extension in extensionsToUpdate)
			{
				var extensionPath = extension.PortalExtensionDefinition.AbsolutePath;
				var clientsOutputDirectory = Path.Combine(extensionPath, "beamable/clients");

				if (ms.IsZoneScoped && !extension.IsZoneScoped)
				{
					Log.Warning("Realm-scoped portal extension {extension} depends on zone-scoped microservice {beamoId}; " +
					            "a realm extension has no zone SDK to call it with.",
						new object[] { extension.BeamoId, ms.BeamoId });
				}

				var generator = new WebClientCodeGenerator(document, "ts", augmentType);

				object pathLock = _pathLocks.GetOrAdd(clientsOutputDirectory, _ => new object());

				// Multiple microservices might try to generate client to the same extension, so we lock this by path
				lock (pathLock)
				{
					generator.GenerateClientCode(clientsOutputDirectory);
				}
			}
		});
	}
}
