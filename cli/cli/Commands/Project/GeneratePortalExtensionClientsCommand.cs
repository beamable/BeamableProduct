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

		// Every clients directory that actually received a client, so `types.ts` can be written beside
		// each one afterwards. A set, because two microservices can generate into the same extension.
		var clientDirectories = new ConcurrentDictionary<string, byte>();

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

			foreach (var extension in extensionsToUpdate)
			{
				var extensionPath = extension.PortalExtensionDefinition.AbsolutePath;
				var clientsOutputDirectory = Path.Combine(extensionPath, "beamable/clients");

				// A zone extension's `context.beam` is a BeamZoneSdk, not a Beam, so its
				// generated client must bind to BeamZoneSdk (constructor param + the
				// `declare module` augmentation that types `beam.<name>Client`). Realm
				// extensions bind to BeamBase as before.
				var isZone = string.Equals(
					extension.PortalExtensionDefinition.Properties?.ServiceScope?.Trim(),
					"zone",
					StringComparison.OrdinalIgnoreCase);
				var augmentType = isZone ? "BeamZoneSdk" : "BeamBase";
				var generator = new WebClientCodeGenerator(document, "ts", augmentType);

				object pathLock = _pathLocks.GetOrAdd(clientsOutputDirectory, _ => new object());

				// Multiple microservices might try to generate client to the same extension, so we lock this by path
				lock (pathLock)
				{
					generator.GenerateClientCode(clientsOutputDirectory);
				}

				clientDirectories.TryAdd(clientsOutputDirectory, 0);
			}
		});

		// The generated client imports `./types` whenever any type was collected, and nothing here used
		// to write that file — so every extension with a typed client ended up importing a module that
		// did not exist, and failed to compile. `GenerateWebClientCommand` has always done this for the
		// web SDK; the portal-extension path simply never did.
		//
		// After the parallel pass, not inside it: the accumulator is process-wide, so emitting mid-flight
		// would write a types file missing whatever a still-running worker had yet to contribute.
		// Nothing generated means nothing to write types for — and, critically, means no generator was
		// ever constructed. `IsTypeScript` reads a static set by that constructor, so consulting it
		// first throws a NullReferenceException for every service no extension depends on: the build
		// compiles, the post-build target dies, and the service never starts.
		if (clientDirectories.IsEmpty || !WebClientCodeGenerator.IsTypeScript)
			return;

		foreach (var clientsOutputDirectory in clientDirectories.Keys)
		{
			WebClientCodeGenerator.GenerateClientTypes(Path.Combine(clientsOutputDirectory, "types"));
		}
	}
}
