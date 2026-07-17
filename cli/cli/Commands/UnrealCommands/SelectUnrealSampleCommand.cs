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
		var pathToUproject = Path.Combine(args.ConfigService.BeamableWorkspace, "BeamableUnreal.uproject");
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

		ApplyProjectOverrides(args.ConfigService.BeamableWorkspace, foundPluginName);
		
		_ = SetEnabledCommand.SetProjectEnabled(serviceListToEnable, args.BeamoLocalSystem.BeamoManifest, true);
		_ = SetEnabledCommand.SetProjectEnabled(serviceListToDisable, args.BeamoLocalSystem.BeamoManifest, false);

	
		uprojectJsonStr = Json.Serialize(uproject, new StringBuilder());
		uprojectJsonStr = Json.FormatJson(uprojectJsonStr);
		File.WriteAllText(pathToUproject, uprojectJsonStr);
		BeamableLogger.Log($"Selected BEAMPROJ: {args.SampleName}");

		// Then we regenerate the project files
		MachineHelper.RunUnrealGenerateProjectFiles(args.ConfigService.BeamableWorkspace);
		
		// Whenever we select a sample, we clear the realm override so that the sample's target realm is respected. 
		args.ConfigService.DeleteLocalOverride(ConfigService.CFG_JSON_FIELD_HOST);
		args.ConfigService.DeleteLocalOverride(ConfigService.CFG_JSON_FIELD_PID);
		args.ConfigService.DeleteLocalOverride(ConfigService.CFG_JSON_FIELD_CID);

		return Task.CompletedTask;
	}
	
	
	public static void ApplyProjectOverrides(string projRoot, string beamProj)
	{
		string[] overrides = new[]{
			"steam_appid.txt"
		};
	
		var overridesRoot = Path.Combine(projRoot, "Plugins", beamProj, "Overrides");

		foreach(var entry in overrides)
		{
			var filePath = Path.Combine(projRoot, entry);
			if(File.Exists(filePath)) {
				File.Delete(filePath);
			}
			string targetFilePath = Path.Combine(overridesRoot, entry);
			if(File.Exists(targetFilePath)){
				FileInfo file = new FileInfo(targetFilePath);
				file.CopyTo(Path.Combine(projRoot, entry));
			}
		}
		var overrideFolders = new[] { "Config" };

		foreach (var overrideFolder in overrideFolders)
		{
			var projectPath = Path.Combine(projRoot, overrideFolder);
			var overridesPath = Path.Combine(overridesRoot, overrideFolder);
			if (!Directory.Exists(overridesPath))
			{
				BeamableLogger.Log($"{beamProj} project does not have Overrides directory for this expected override path. Create one at: {overridesPath}");
				return;
			}

			if (Directory.Exists(projectPath))
				Directory.Delete(projectPath, true);

			CopyDirectory(overridesPath, projectPath);
		}

		static void CopyDirectory(string sourceDir, string destinationDir)
		{
			// Get information about the source directory
			var dir = new DirectoryInfo(sourceDir);

			// Check if the source directory exists
			if (!dir.Exists)
				throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

			// Cache directories before we start copying
			DirectoryInfo[] dirs = dir.GetDirectories();

			// Create the destination directory
			Directory.CreateDirectory(destinationDir);

			// Get the files in the source directory and copy to the destination directory
			foreach (FileInfo file in dir.GetFiles())
			{
				string targetFilePath = Path.Combine(destinationDir, file.Name);
				file.CopyTo(targetFilePath, true);
			}

			foreach (DirectoryInfo subDir in dirs)
			{
				string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
				CopyDirectory(subDir.FullName, newDestinationDir);
			}
		}
	}
}
