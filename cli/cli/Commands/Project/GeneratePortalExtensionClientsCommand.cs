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
			var extensionsToUpdate = allExtensions
				.Where(e => e.PortalExtensionDefinition.MicroserviceDependencies.Contains(ms.BeamoId))
				.ToList();

			if (extensionsToUpdate.Count == 0) return;

			(bool hasOpenApiDocument, OpenApiDocument document) = await BeamoServiceDefinition.TryGetOpenApiDocument(ms.OpenApiPath);

			if (!hasOpenApiDocument)
			{
				Log.Warning("Could not find any open API document: {path} for service {beamoId}", new object[] { ms.OpenApiPath, ms.BeamoId });
				return;
			}

			var generator = new WebClientCodeGenerator(document, "ts");

			foreach (var extension in extensionsToUpdate)
			{
				var extensionPath = extension.PortalExtensionDefinition.AbsolutePath;
				var clientsOutputDirectory = Path.Combine(extensionPath, "beamable/clients");

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
