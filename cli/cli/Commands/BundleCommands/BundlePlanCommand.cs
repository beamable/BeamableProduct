using Beamable.Server;
using cli.Services.Bundles;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;

namespace cli.BundleCommands;

public class BundlePlanCommandArgs : CommandArgs
{
	public string bundleName;
}

public class BundlePlanComponentOutput
{
	public string beamoId;
	public bool existsLocally;
}

public class BundlePlanCommandOutput
{
	public string name;
	public List<BundlePlanComponentOutput> components = new List<BundlePlanComponentOutput>();
	public Dictionary<string, string> peerDependencies = new Dictionary<string, string>();
}

public class BundlePlanCommand : AtomicCommand<BundlePlanCommandArgs, BundlePlanCommandOutput>
{
	public BundlePlanCommand() : base("plan", "Preview the components an authored bundle would publish")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("bundle-name", "The namespaced bundle name, e.g. <namespace>/<bundle-name>"),
			(args, i) => args.bundleName = i);
	}

	public override Task<BundlePlanCommandOutput> GetResult(BundlePlanCommandArgs args)
	{
		var bundle = BundleWorkspace.Require(args.ConfigService, args.bundleName);
		var localIds = new HashSet<string>(
			args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions.Select(d => d.BeamoId));

		var output = new BundlePlanCommandOutput { name = bundle.name };
		foreach (var component in bundle.components)
		{
			var existsLocally = localIds.Contains(component);
			if (!existsLocally)
			{
				Log.Warning($"Bundle component=[{component}] was not found as a local service/storage/extension.");
			}

			output.components.Add(new BundlePlanComponentOutput { beamoId = component, existsLocally = existsLocally });
		}

		foreach (var kvp in bundle.peerDependencies)
		{
			output.peerDependencies[kvp.Key] = kvp.Value?.type;
		}

		return Task.FromResult(output);
	}
}
