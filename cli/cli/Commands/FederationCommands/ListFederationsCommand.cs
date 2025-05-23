using Beamable.Api.Autogenerated.Beamo;
using Beamable.Api.Autogenerated.Models;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Server;
using Beamable.Server.Common;
using cli.Services;
using cli.Utils;
using Newtonsoft.Json;
using System.Collections;
using System.CommandLine;

namespace cli.FederationCommands;

public class ListServicesCommandArgs : CommandArgs
{
	public string idFilter;
	public string federationFilter;
	public string federationNamespaceFilter;
}

[Serializable]
public class ListServicesCommandOutput
{
	public string cid;
	public string pid;
	public List<ServiceFederations> services = new List<ServiceFederations>();
}

[Serializable]
public class ServiceFederations : IEquatable<ServiceFederations>
{
	public string beamoName;
	public string routingKey;
	public FederationsConfig federations;

	public bool Equals(ServiceFederations other)
	{
		if (other is null) return false;
		if (ReferenceEquals(this, other)) return true;
		return beamoName == other.beamoName && routingKey == other.routingKey;
	}

	public override bool Equals(object obj)
	{
		if (obj is null) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != GetType()) return false;
		return Equals((ServiceFederations)obj);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			return ((beamoName != null ? beamoName.GetHashCode() : 0) * 397) ^ (routingKey != null ? routingKey.GetHashCode() : 0);
		}
	}

	public static bool operator ==(ServiceFederations left, ServiceFederations right) => Equals(left, right);
	public static bool operator !=(ServiceFederations left, ServiceFederations right) => !Equals(left, right);
}

public class ListFederationsCommand : StreamCommand<ListServicesCommandArgs, ListServicesCommandOutput>
{
	public ListFederationsCommand() : base("list", "List all federations configured in known services")
	{
	}

	public override void Configure()
	{
		var federationFilterOpt =
			new Option<string>("--type", "Filter the federations by the type");
		AddOption(federationFilterOpt, (args, i) => args.federationFilter = i?.ToLowerInvariant());

		var idsFilterOpt =
			new Option<string>("--id", "Filter the federations by the service name");
		AddOption(idsFilterOpt, (args, i) => args.idFilter = i?.ToLowerInvariant());


		var namespaceFilterOpt =
			new Option<string>("--fed-ids", "Filter the federation by its federation id");
		namespaceFilterOpt.AddAlias("-fid");
		AddOption(namespaceFilterOpt, (args, i) => args.federationNamespaceFilter = i?.ToLowerInvariant());
	}

	public override async Task Handle(ListServicesCommandArgs args)
	{
		var output = GetLocalFederations(args.AppContext.Cid, args.AppContext.Pid, args.BeamoLocalSystem.BeamoManifest);
		ApplyFilters(args, output);
		SendResults(output);
		
		await Task.CompletedTask;
	}

	public static ListServicesCommandOutput GetLocalFederations(string cid, string pid, BeamoLocalManifest beamoManifest)
	{
		var output = new ListServicesCommandOutput();
		output.cid = cid;
		output.pid = pid;
		output.services = new();

		foreach (var sd in beamoManifest.ServiceDefinitions)
		{
			if (sd.Protocol is not BeamoProtocolType.HttpMicroservice) continue;
			if (sd.IsLocal is not true) continue; 
			
			var service = new ServiceFederations();
			service.beamoName = sd.BeamoId;
			service.routingKey = ServiceRoutingStrategyExtensions.GetDefaultRoutingKeyForMachine();
			service.federations = sd.FederationsConfig.Federations;

			output.services.Add(service);
		}

		return output;
	}

	public override bool AutoLogOutput => true;


	private static void ApplyFilters(ListServicesCommandArgs args, ListServicesCommandOutput res)
	{
		IEnumerable<ServiceFederations> services = res.services;
		if (!string.IsNullOrEmpty(args.federationFilter))
		{
			Log.Verbose($"applying federation type filter=[{args.federationFilter}]");
			services = res.services.Where(x =>
				x.federations.Any(f => f.Value.Any(fi => fi.Interface.Contains(args.federationFilter, StringComparison.InvariantCultureIgnoreCase))));
		}

		if (!string.IsNullOrEmpty(args.idFilter))
		{
			Log.Verbose($"applying federation name filter=[{args.idFilter}]");
			services = res.services.Where(x => x.beamoName.Contains(args.idFilter, StringComparison.InvariantCultureIgnoreCase));
		}

		if (!string.IsNullOrEmpty(args.federationNamespaceFilter))
		{
			Log.Verbose($"applying federation namespace filter=[{args.federationNamespaceFilter}]");
			services = res.services.Where(x =>
				x.federations.Any(f => f.Key.Contains(args.federationNamespaceFilter, StringComparison.InvariantCultureIgnoreCase)));
		}

		res.services = services.ToList();
	}
}
