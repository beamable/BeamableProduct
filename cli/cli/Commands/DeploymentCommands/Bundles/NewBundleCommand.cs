using Beamable.Server;
using cli.Services.Bundles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;

namespace cli.DeploymentCommands.Bundles;

public class NewBundleCommandArgs : CommandArgs
{
	public string bundleName;
	public List<string> components = new List<string>();
}

public class NewBundleCommandOutput
{
	public string name;
	public string filePath;
}

public class NewBundleCommand : AtomicCommand<NewBundleCommandArgs, NewBundleCommandOutput>, ISkipManifest
{
	public NewBundleCommand() : base("new", "Scaffold a new bundle config file in the workspace")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("bundle-name", "The namespaced bundle name, e.g. <namespace>/<bundle-name>"),
			(args, i) => args.bundleName = i);
		AddOption(new Option<List<string>>(new[] { "--component", "-c" }, "A beamoId to include in the bundle (repeatable)")
		{
			AllowMultipleArgumentsPerToken = true
		}, (args, i) => args.components = i ?? new List<string>());
	}

	public override Task<NewBundleCommandOutput> GetResult(NewBundleCommandArgs args)
	{
		// validate the name splits into a namespace + short name.
		BundleWorkspace.SplitBundleName(args.bundleName);

		var existing = BundleWorkspace.Discover(args.ConfigService).FirstOrDefault(b => b.name == args.bundleName);
		if (existing != null)
		{
			Log.Warning($"A bundle named [{args.bundleName}] already exists at [{existing.filePath}]. Not overwriting.");
			return Task.FromResult(new NewBundleCommandOutput { name = args.bundleName, filePath = existing.filePath });
		}

		var fileName = args.bundleName.Replace("@", "").Replace('/', '-') + BundleWorkspace.BUNDLE_FILE_SUFFIX;
		var fullPath = Path.Combine(args.ConfigService.BeamableWorkspace, fileName);

		var json = new JObject
		{
			["name"] = args.bundleName,
			["components"] = new JArray(args.components),
			["peerDependencies"] = new JObject()
		};
		File.WriteAllText(fullPath, json.ToString(Formatting.Indented));
		Log.Information($"Created bundle config [{args.bundleName}] at [{fullPath}]");

		return Task.FromResult(new NewBundleCommandOutput { name = args.bundleName, filePath = fullPath });
	}
}
