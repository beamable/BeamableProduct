using cli.Services;
using Serilog;
using System.CommandLine;

namespace cli.Commands.Project.Deps;

public class ListDepsCommandArgs : CommandArgs
{
	public string ServiceName;
	public bool ListAll;
}

[Serializable]
public class ListDepsCommandResults
{
	public List<ServiceDependenciesPair> Services;
}

[Serializable]
public class ServiceDependenciesPair
{
	public string name;
	public List<DependencyData> dependencies;
}

public class ListDepsCommand : AtomicCommand<ListDepsCommandArgs, ListDepsCommandResults>
{
	public ListDepsCommand() : base("list", "Lists all dependencies of given service, if none then lists all dependencies of all existing services")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--service", "The name of the service to list the dependencies of"),
			(args, i) => args.ServiceName = i);
		AddOption(new Option<bool>("--all", "If this is passed and set to True, then all references of the service will be listed"),
			(args, i) => args.ListAll = i);
	}

	public override async Task<ListDepsCommandResults> GetResult(ListDepsCommandArgs args)
	{
		var listAllServices = string.IsNullOrEmpty(args.ServiceName);
		List<string> servicesToShow;
		ListDepsCommandResults result = new();
		var allServicesDefinitions = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions;

		if (listAllServices)
		{

			servicesToShow = allServicesDefinitions.Where(sd => sd.Protocol == BeamoProtocolType.HttpMicroservice)
				.Select(sd => sd.BeamoId).ToList();
		}
		else
		{
			var elementIndex = allServicesDefinitions.FindIndex(sd => sd.BeamoId == args.ServiceName);
			if (elementIndex < 0)
			{
				Log.Error("Specified service name does not exist");
			}

			servicesToShow = new List<string>() { args.ServiceName };
		}

		if (servicesToShow.Count == 0)
		{
			Log.Information("There are no microservices to show dependencies at the moment.");
		}

		result.Services = new List<ServiceDependenciesPair>();
		foreach (string service in servicesToShow)
		{
			List<DependencyData> dependencies = await args.BeamoLocalSystem.GetDependencies(service, args.ListAll);
			result.Services.Add(new ServiceDependenciesPair()
			{
				name = service,
				dependencies = dependencies
			});
		}

		return result;
	}
}