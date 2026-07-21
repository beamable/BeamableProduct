using Beamable.Server;
using cli.Services.Bundles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;

namespace cli.BundleCommands;

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
		AddArgument(new Argument<string>("bundle-name", "The bundle name; the config file will be named <bundle-name>.beam.bundle.json"),
			(args, i) => args.bundleName = i);
		AddOption(new Option<List<string>>(new[] { "--component", "-c" }, () => new List<string>(), "A beamoId to include in the bundle (repeatable)")
		{
			AllowMultipleArgumentsPerToken = true
		}, (args, i) => args.components = i);
	}

	public override async Task<NewBundleCommandOutput> GetResult(NewBundleCommandArgs args)
	{
		BundleWorkspace.ValidateName(args.bundleName);

		// The logical bundle name is @<alias>/<bundle-name>; resolved best-effort so scaffolding
		// still works when the namespace can't be resolved (e.g. offline).
		var ns = await BundleNamespace.TryGet(args);
		var fullName = ns == null ? args.bundleName : BundleNamespace.Qualify(ns, args.bundleName);

		var existing = BundleWorkspace.Discover(args.ConfigService).FirstOrDefault(b => b.name == args.bundleName);
		if (existing != null)
		{
			Log.Warning($"A bundle named [{fullName}] already exists at [{existing.filePath}]. Not overwriting.");
			return new NewBundleCommandOutput { name = fullName, filePath = existing.filePath };
		}

		var fileName = args.bundleName + BundleWorkspace.BUNDLE_FILE_SUFFIX;
		var fullPath = Path.Combine(args.ConfigService.BeamableWorkspace, fileName);

		// the bundle's name is the file name itself; it is not stored inside the file.
		var json = new JObject
		{
			["components"] = new JArray(args.components),
			["peerDependencies"] = new JObject()
		};
		File.WriteAllText(fullPath, json.ToString(Formatting.Indented));
		Log.Information($"Created bundle config [{fullName}] at [{fullPath}]");

		return new NewBundleCommandOutput { name = fullName, filePath = fullPath };
	}
}
