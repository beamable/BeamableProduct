using Beamable.Server;
using cli.Services;
using cli.Services.Web;
using Microsoft.OpenApi.Models;
using System.Collections.Concurrent;
using System.CommandLine;

namespace cli.Commands.Project;

public class GeneratePortalExtensionClientsCommandArgs : CommandArgs
{
	public string ServiceName;
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
		AddOption(new Option<string>("--service-name", () => null,
				"When null or empty, it will generate clients for all microservices. Generates a client for this service for all dependent portal extensions"),
			(args, val) => args.ServiceName = val);
	}

	public override async Task Handle(GeneratePortalExtensionClientsCommandArgs args)
	{
		var allServices = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions;
		var allExtensions = allServices.Where((s) => s.Protocol == BeamoProtocolType.PortalExtension).ToList();

		var micros = new List<BeamoServiceDefinition>();

		if (!string.IsNullOrEmpty(args.ServiceName))
		{
			var def = allServices.FirstOrDefault((s) => s.BeamoId == args.ServiceName);

			if (def == null)
			{
				throw new CliException($"Could not find microservice with name: {args.ServiceName}");
			}

			micros.Add(def);
		}
		else
		{
			foreach (var service in allServices)
			{
				if (service.Protocol == BeamoProtocolType.HttpMicroservice)
				{
					micros.Add(service);
				}
			}
		}

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
