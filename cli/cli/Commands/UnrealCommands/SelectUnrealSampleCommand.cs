using Beamable.Common;
using Beamable.Serialization.SmallerJSON;
using cli.Commands.Project;
using cli.Dotnet;
using cli.Utils;
using System.Collections;
using System.CommandLine;
using System.Text;

namespace cli.UnrealCommands;

public class SelectUnrealSampleCommandArgs : CommandArgs
{
	public string SampleName;
}

public class SelectUnrealSampleCommand : AppCommand<SelectUnrealSampleCommandArgs>
{
	public SelectUnrealSampleCommand() : base("select-sample", "Run this ONLY when inside the root of the UnrealSDK repo to configure it as a particular sample")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("sample-name", "The name of the sample with or without \"BEAMPROJ_\""), (args, sampleName) =>
		{
			args.SampleName = sampleName;
		});
	}

	public override Task Handle(SelectUnrealSampleCommandArgs args)
	{
		var pathToUproject = Path.Combine(args.ConfigService.BaseDirectory, "BeamableUnreal.uproject");
		if (!File.Exists(pathToUproject))
			throw new CliException("Please ensure this is called from inside the UnrealSDK project folder.");

		var uprojectJsonStr = File.ReadAllText(pathToUproject);
		var uproject = Json.Deserialize(uprojectJsonStr) as ArrayDict;

		var uprojectPlugins = uproject["Plugins"] as IList;
		var foundCorrectPlugin = false;
		var foundPluginName = "";
		foreach (object uprojectPlugin in uprojectPlugins!)
		{
			if (uprojectPlugin is ArrayDict plugin)
			{
				var pluginName = plugin["Name"].ToString();
				if (pluginName.StartsWith("BEAMPROJ_"))
				{
					var isActive = pluginName.Contains(args.SampleName);
					plugin["Enabled"] = isActive;
					foundCorrectPlugin |= isActive;
					if (isActive)
						foundPluginName = pluginName;
				}
			}
		}

		if (!foundCorrectPlugin)
			throw new CliException($"Failed to find sample [{args.SampleName}]. Please provide a valid BEAMPROJ_SampleName argument.");


		// We also need to make sure the microservices for the selected sample are the only ones enabled.
		var serviceListToEnable = new List<string>();
		ProjectCommand.FinalizeServicesArg(args,
			withTags: new List<string>(new[] { foundPluginName }),
			withoutTags: new(),
			includeStorage: true,
			ref serviceListToEnable);

		var serviceListToDisable = new List<string>();
		ProjectCommand.FinalizeServicesArg(args,
			withTags: new(),
			withoutTags: new() { foundPluginName },
			includeStorage: true,
			ref serviceListToDisable);

		_ = SetEnabledCommand.SetProjectEnabled(serviceListToEnable, args.BeamoLocalSystem.BeamoManifest, true);
		_ = SetEnabledCommand.SetProjectEnabled(serviceListToDisable, args.BeamoLocalSystem.BeamoManifest, false);

		uprojectJsonStr = Json.Serialize(uproject, new StringBuilder());
		uprojectJsonStr = Json.FormatJson(uprojectJsonStr);
		File.WriteAllText(pathToUproject, uprojectJsonStr);
		BeamableLogger.Log($"Selected BEAMPROJ: {args.SampleName}");

		// Then we regenerate the project files
		MachineHelper.RunUnrealGenerateProjectFiles(args.ConfigService.BaseDirectory);

		return Task.CompletedTask;
	}
}
