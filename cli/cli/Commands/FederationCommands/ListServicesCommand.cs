using Beamable.Api.Autogenerated.Beamo;
using Beamable.Api.Autogenerated.Models;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Serilog;
using System.Collections;
using System.CommandLine;

namespace cli.FederationCommands;

public class ListServicesCommandArgs : CommandArgs
{
	public string federationFilter;
	public string federationNamespaceFilter;
	public long authorFilter;
	public OptionalBool enabledFilter;
	public string nameFilter;
	public bool localOnlyFilter;
}

public class ListServicesCommandOutput
{
	public string cid;
	public string pid;
	public List<RunningService> services = new List<RunningService>();
}

public class RunningService
{
	public string name;
	public string routingKey;
	public string fullName;
	public int instanceCount;
	public bool trafficFilterEnabled;
	public long authorPlayerId;

	public List<RunningFederation> federations = new List<RunningFederation>();
}

public class RunningFederation
{
	public string nameSpace;
	public string federationType;
}

public class ListServicesCommand : AtomicCommand<ListServicesCommandArgs,ListServicesCommandOutput>
{
	public ListServicesCommand() : base("list", "List all running services in the current realm")
	{
	}

	public override void Configure()
	{
		var federationFilterOpt =
			new Option<string>("--type", "Filter the services by the types of federations they provide");
		AddOption(federationFilterOpt, (args, i) => args.federationFilter = i?.ToLowerInvariant());
		
		var nameFilterOpt =
			new Option<string>("--name", "Filter the services by the service name");
		AddOption(nameFilterOpt, (args, i) => args.nameFilter = i?.ToLowerInvariant());
		
		var namespaceFilterOpt =
			new Option<string>("--namespace", "Filter the services by the federation namespace");
		namespaceFilterOpt.AddAlias("-ns");
		AddOption(namespaceFilterOpt, (args, i) => args.federationNamespaceFilter = i?.ToLowerInvariant());
		
		var playerIdOpt =
			new Option<long>("--player", "Filter the services by the playerId of the author");
		AddOption(playerIdOpt, (args, i) => args.authorFilter = i);
		
		var localOnlyOpt =
			new Option<bool>("--local", "Filter the services for which ones are running locally");
		AddOption(localOnlyOpt, (args, i) => args.localOnlyFilter = i);
		
		var enabledOpt =
			new Option<bool>("--enabled", "Filter the services by their traffic enablement");
		AddOption(enabledOpt, (args, ctx, i) =>
		{
			// ctx.ParseResult.GetValueForOption()
			var output = ctx.ParseResult.FindResultFor(enabledOpt);
			if (output != null)
			{
				args.enabledFilter = i;
			}
			else
			{
				args.enabledFilter = new OptionalBool();
			}
				
		});
	}

	public override async Task<ListServicesCommandOutput> GetResult(ListServicesCommandArgs args)
	{
		var res = await GetRunningServices(args.DependencyProvider, args.localOnlyFilter);

		IEnumerable<RunningService> services = res.services;
		if (!string.IsNullOrEmpty(args.federationFilter))
		{
			Log.Verbose($"applying federation type filter=[{args.federationFilter}]");
			services = res.services.Where(x =>
				x.federations.Any(f => f.federationType.ToLowerInvariant().Contains(args.federationFilter)));
		}
		if (!string.IsNullOrEmpty(args.nameFilter))
		{
			Log.Verbose($"applying federation name filter=[{args.nameFilter}]");
			services = res.services.Where(x => x.name.ToLowerInvariant().Contains(args.nameFilter));
		}
		if (args.authorFilter > 0)
		{
			Log.Verbose($"applying federation author filter=[{args.authorFilter}]");
			services = res.services.Where(x => x.authorPlayerId == args.authorFilter);
		}
		if (!string.IsNullOrEmpty(args.federationNamespaceFilter))
		{
			Log.Verbose($"applying federation namespace filter=[{args.federationNamespaceFilter}]");
			services = res.services.Where(x =>
				x.federations.Any(f => f.nameSpace.ToLowerInvariant().Contains(args.federationNamespaceFilter)));
		}

		if (args.enabledFilter.HasValue)
		{
			var enabledValue = args.enabledFilter.Value;
			Log.Verbose($"applying federation enabled filter=[{enabledValue}]");
			services = res.services.Where(x => x.trafficFilterEnabled == enabledValue);
		}

		res.services = services.ToList();

		return res;
	}

	public static async Task<ListServicesCommandOutput> GetRunningServices(IDependencyProvider provider, bool? localOnly=null)
	{
		var api = provider.GetService<IBeamoApi>();
		var req = new MicroserviceRegistrationsQuery { };
		if (localOnly.HasValue)
		{
			req.localOnly = localOnly;
		}
		var res = await api.PostMicroserviceRegistrations(req);

		var ctx = provider.GetService<IAppContext>();
		
		return new ListServicesCommandOutput
		{
			cid = ctx.Cid,
			pid = ctx.Pid,
			services = res.registrations.Select(x => new RunningService
			{
				name = x.serviceName.Split('.')[2],
				fullName = x.serviceName,
				instanceCount = x.instanceCount,
				trafficFilterEnabled = x.trafficFilterEnabled,
				routingKey = x.routingKey,
				authorPlayerId = x.startedByGamerTag.GetOrElse(0),
				federations = x.federation.GetOrElse(() => null)?.Select(f => new RunningFederation
				{
					nameSpace = f.nameSpace,
					federationType = f.type.ToString()
				}).ToList()
			}).ToList()
		};
	}
}