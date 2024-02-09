using Beamable.Common;
using Serilog;
using System.CommandLine;
using System.Text.RegularExpressions;

namespace cli;

public class ServicesSetManifestCommandArgs : CommandArgs
{
	public List<string> microservices;
	public List<string> storages;
	public List<string> disabledServices;
}


public class ServicesSetManifestCommand : AppCommand<ServicesSetManifestCommandArgs>, IEmptyResult
{
	public ServicesSetManifestCommand() : base("set-local-manifest",
		"Set the entire state of the local manifest, overwriting any existing entries")
	{
	}

	public override void Configure()
	{
		var names = new Option<List<string>>("--services", "Local http services paths")
		{
			Arity = ArgumentArity.ZeroOrMore,
			AllowMultipleArgumentsPerToken = true
		};
		AddOption(names, (x, i) => x.microservices = i);

		var storageDeps = new Option<List<string>>("--storage-paths", "Local storages paths")
		{
			Arity = ArgumentArity.ZeroOrMore,
			AllowMultipleArgumentsPerToken = true
		};
		AddOption(storageDeps, (x, i) => x.storages = i);

		var shouldBeEnabled = new Option<List<string>>("--disabled-services", "Names of the services that should be disabled on remote")
		{
			Arity = ArgumentArity.ZeroOrMore,
			AllowMultipleArgumentsPerToken = true
		};
		AddOption(shouldBeEnabled, (x, i) => x.disabledServices = i);
	}

	public override async Task Handle(ServicesSetManifestCommandArgs args)
	{
		var nonExistingServices = args.microservices.Concat(args.storages)
			.Where(p => !Directory.Exists(Path.Combine(args.ConfigService.BaseDirectory, p))).ToArray();
		if (nonExistingServices.Length > 0)
		{
			throw new CliException(
				$"The following services do not exist: {string.Join(", ", nonExistingServices)}");
		}

		args.BeamoLocalSystem.BeamoManifest.Clear();

		foreach (var storagePath in args.storages)
		{
			var storageName = Directory.GetDirectories(storagePath).Last();
			await args.BeamoLocalSystem.AddDefinition_EmbeddedMongoDb(storageName, "mongo:latest",
				storagePath, CancellationToken.None);
		}

		foreach (string microservicePath in args.microservices)
		{
			var directoryInfo = new DirectoryInfo(Path.Combine(args.ConfigService.BaseDirectory, microservicePath));
			var name = directoryInfo.Name;
			var contextPath = args.ConfigService.GetRelativePath(directoryInfo.Parent!.FullName);
			var dockerPath = Path.Combine(name, "Dockerfile");
			var shouldBeEnabled = !args.disabledServices.Contains(name);

			_ = await args.BeamoLocalSystem.AddDefinition_HttpMicroservice(name,
				contextPath,
				dockerPath,
				CancellationToken.None,
				shouldBeEnabled,
				microservicePath);
		}
		args.BeamoLocalSystem.SaveBeamoLocalManifest();
	}
}
