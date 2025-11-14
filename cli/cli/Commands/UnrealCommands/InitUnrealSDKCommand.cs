using Beamable.Common;
using Beamable.Serialization.SmallerJSON;
using cli.Utils;
using System.Collections;
using System.CommandLine;
using System.Text;

namespace cli.UnrealCommands;

public class InitUnrealSDKCommandArgs : CommandArgs
{
	public string BeamableUnrealSdkRepoPath;
	public string UProjectFilePath;
	public string UProjectFileName;
	public bool IsInstallingOnlineSubsystem;
}

public class InitUnrealSDKCommand : AppCommand<InitUnrealSDKCommandArgs>
{
	public override bool IsForInternalUse => true;

	public InitUnrealSDKCommand() : base("init", "Ran by `beam_init_game_maker.sh` in order to do a bunch of set up in an Unreal project automatically")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("unrealSdkRepoPath", "Path to your clone of Beamable's UnrealSDK repo in your machine"), (args, unrealSdkRepoPath) =>
		{
			if (!Path.Exists(unrealSdkRepoPath)) throw new CliException($"given ´unrealSdkRepoPath´ does not exist, unrealSdkRepoPath=[${unrealSdkRepoPath}]");
			if (!File.GetAttributes(unrealSdkRepoPath).HasFlag(FileAttributes.Directory)) throw new CliException($"given ´unrealSdkRepoPath´ must be a directory, unrealSdkRepoPath=[${unrealSdkRepoPath}]");

			args.BeamableUnrealSdkRepoPath = unrealSdkRepoPath;
		});

		AddArgument(new Argument<string>("uprojectFilePath", "Path to the .uproject file of your project"), (args, uprojectFilePath) =>
		{
			if (!Path.Exists(uprojectFilePath)) throw new CliException($"given ´uprojectFilePath´ does not exist, uprojectFilePath=[${uprojectFilePath}]");

			if (Path.GetExtension(uprojectFilePath) is ".uproject")
			{
				args.UProjectFilePath = uprojectFilePath;
				args.UProjectFileName = Path.GetFileName(uprojectFilePath);
			}
			else
			{
				throw new CliException($"given uprojectFilePath must be a path to a uproject file, uprojectFilePath=[${uprojectFilePath}]");
			}
		});

		AddArgument(new Argument<bool>("installOss", "Whether or not we are installing the OnlineSubsystemBeamable"), (args, installOss) =>
		{
			args.IsInstallingOnlineSubsystem = installOss;
		});
	}

	public override Task Handle(InitUnrealSDKCommandArgs args)
	{
		var uprojectJsonStr = File.ReadAllText(args.UProjectFilePath);
		var uproject = Json.Deserialize(uprojectJsonStr) as ArrayDict;

		// Should never be seen
		if (uproject == null)
			throw new CliException("If you're seeing this it means your uproject file is not valid JSON and is in a bad state. Please take a look at it and ensure it's content is a valid JSON.");

		// Find out if the Plugins in the uproject file are already configured and, if not, configure them correctly.
		{
			var uprojectPlugins = uproject["Plugins"] as IList;
			var beamableCoreIsInstalled = false;
			var onlineSubsystemBeamableIsInstalled = false;

			foreach (object uprojectPlugin in uprojectPlugins!)
			{
				if (uprojectPlugin is ArrayDict plugin)
				{
					if (plugin["Name"] is "BeamableCore")
					{
						beamableCoreIsInstalled = true;
					}

					if (plugin["Name"] is "OnlineSubsystemBeamable")
					{
						onlineSubsystemBeamableIsInstalled = true;
					}
				}
			}

			// Set up the BeamableCore plugin
			var shouldInstallCore = !beamableCoreIsInstalled;
			if (!beamableCoreIsInstalled)
			{
				uprojectPlugins.Add(new ArrayDict() { { "Name", "BeamableCore" }, { "Enabled", true } });
			}

			// Set up the OnlineSubsystemBeamable plugin, if requested
			var shouldInstallOss = !onlineSubsystemBeamableIsInstalled && args.IsInstallingOnlineSubsystem;
			if (shouldInstallOss)
			{
				uprojectPlugins.Add(new ArrayDict() { { "Name", "OnlineSubsystemBeamable" }, { "Enabled", true } });
			}

			if (shouldInstallCore || shouldInstallOss)
			{
				uprojectJsonStr = Json.Serialize(uproject, new StringBuilder());
				uprojectJsonStr = Json.FormatJson(uprojectJsonStr);
				File.WriteAllText(args.UProjectFilePath, uprojectJsonStr);
				BeamableLogger.Log("Updated uproject file with Beamable Plugin(s).");
			}
		}


		// If we have the BeamableCore plugin installed, delete it and then copy it in.
		var unrealRootPath = Path.GetDirectoryName(args.UProjectFilePath)!;
		{
			var gameMakerBeamablePluginPath = Path.Combine(unrealRootPath, "Plugins", "BeamableCore");
			var sdkBeamablePluginPath = Path.Combine(args.BeamableUnrealSdkRepoPath, "Plugins", "BeamableCore");
			if (Directory.Exists(gameMakerBeamablePluginPath))
			{
				BeamableLogger.Log("Already installed BeamableCore plugin. Removing it so we can re-install it.");
				Directory.Delete(gameMakerBeamablePluginPath, true);
			}

			CopyDirectory(sdkBeamablePluginPath, gameMakerBeamablePluginPath);
			BeamableLogger.Log("Installed BeamableCore plugin.");
		}

		// If we have the OnlineSubsystem plugin installed, delete ONLY the non-Customer parts of it then copy it in.
		if (args.IsInstallingOnlineSubsystem)
		{
			var gameMakerOssPath = Path.Combine(unrealRootPath, "Plugins", "OnlineSubsystemBeamable");
			var sdkBeamablePluginPath = Path.Combine(args.BeamableUnrealSdkRepoPath, "Plugins", "OnlineSubsystemBeamable");

			if (Directory.Exists(gameMakerOssPath))
			{
				BeamableLogger.Log("Already installed OnlineSubsystemBeamable plugin. Removing it so we can re-install it.");

				// Copy the Content/Beamable folder
				BeamableLogger.Log("Copying OnlineSubsystemBeamable plugin's Beamable Content folder.");
				var gameMakerContentPath = Path.Combine(gameMakerOssPath, "Content", "Beamable");
				var sdkContentPath = Path.Combine(sdkBeamablePluginPath, "Content", "Beamable");
				if (Path.Exists(gameMakerContentPath))
				{
					Directory.Delete(gameMakerContentPath, true);
				}

				if (Path.Exists(sdkContentPath))
				{
					CopyDirectory(sdkContentPath, gameMakerContentPath);
				}


				// Copy the Build.cs file
				BeamableLogger.Log("Copying OnlineSubsystemBeamable plugin's Build.cs file.");
				var gameMakerBuildPath = Path.Combine(gameMakerOssPath, "Source", "OnlineSubsystemBeamable", "OnlineSubsystemBeamable.Build.cs");
				var sdkBuildPath = Path.Combine(sdkBeamablePluginPath, "Source", "OnlineSubsystemBeamable", "OnlineSubsystemBeamable.Build.cs");
				if (Path.Exists(gameMakerBuildPath))
				{
					File.Delete(gameMakerBuildPath);
				}

				File.Copy(sdkBuildPath, gameMakerBuildPath);


				// Copy the Source files
				{
					{
						BeamableLogger.Log("Copying OnlineSubsystemBeamable plugin's Source/OnlineSubsystemBeamable/Beamable files.");
						var gameMakerSourcePath = Path.Combine(gameMakerOssPath, "Source", "OnlineSubsystemBeamable", "Public", "Beamable");
						var sdkSourcePath = Path.Combine(sdkBeamablePluginPath, "Source", "OnlineSubsystemBeamable", "Public", "Beamable");
						if (Path.Exists(gameMakerSourcePath))
						{
							Directory.Delete(gameMakerSourcePath, true);
						}

						CopyDirectory(sdkSourcePath, gameMakerSourcePath);
					}

					{
						var gameMakerSourcePath = Path.Combine(gameMakerOssPath, "Source", "OnlineSubsystemBeamable", "Private", "Beamable");
						var sdkSourcePath = Path.Combine(sdkBeamablePluginPath, "Source", "OnlineSubsystemBeamable", "Private", "Beamable");
						if (Path.Exists(gameMakerSourcePath))
						{
							Directory.Delete(gameMakerSourcePath, true);
						}

						CopyDirectory(sdkSourcePath, gameMakerSourcePath);
					}
				}
			}
			else
			{
				BeamableLogger.Log("Copying OnlineSubsystemBeamable plugin.");
				CopyDirectory(sdkBeamablePluginPath, gameMakerOssPath);
			}

			BeamableLogger.Log("Installed OnlineSubsystemBeamable plugin.");
		}


		// Copy over the Beam utility functions to the project's main Target.cs file
		var projectName = Path.GetFileNameWithoutExtension(args.UProjectFileName);
		var gameMakerTargetCsFile = Path.GetDirectoryName(args.UProjectFilePath)!;
		gameMakerTargetCsFile = Path.Combine(gameMakerTargetCsFile, "Source", $"{projectName}.Target.cs");
		if (!File.Exists(gameMakerTargetCsFile))
			throw new CliException("Failed to find the project's target file.");

		var sdkMainTargetCsFile = args.BeamableUnrealSdkRepoPath;
		sdkMainTargetCsFile = Path.Combine(sdkMainTargetCsFile, "Source", $"BeamableUnreal.Target.cs");

		// Read out the game-maker's main Target.cs file so that we can add the Beamable utilities code to it.
		var gameMakerTargetFile = File.ReadAllText(gameMakerTargetCsFile);
		var sdkTargetFile = File.ReadAllText(sdkMainTargetCsFile);

		// Find the beamable utility code that we need to paste into the project
		{
			const string startTag = "/* BEAMABLE CODE TO COPY PASTE START */";
			const string endTag = "/* BEAMABLE CODE TO COPY PASTE END */";

			var sdkStartIdx = sdkTargetFile.IndexOf(startTag, StringComparison.Ordinal);
			var sdkEndIdx = sdkTargetFile.IndexOf(endTag, StringComparison.Ordinal) + endTag.Length;

			if (sdkStartIdx < 0 || sdkEndIdx < 0)
				throw new CliException("Failed to find the Beam utilities code in the SDK's own target file. If you see this, please report a bug to Beamable.");

			var sdkBeamableUtilitiesCode = sdkTargetFile[sdkStartIdx..sdkEndIdx];


			// If it already had the utility code in it, let's remove it for a clean install.
			if (gameMakerTargetFile.Contains(startTag))
			{
				var gameMakerStartIdx = gameMakerTargetFile.IndexOf(startTag, StringComparison.Ordinal);
				var gameMakerEndIdx = gameMakerTargetFile.LastIndexOf(endTag, StringComparison.Ordinal) + endTag.Length;
				gameMakerTargetFile = gameMakerTargetFile.Remove(gameMakerStartIdx, gameMakerEndIdx - gameMakerStartIdx);
			}

			// Append the utilities code to the target file
			gameMakerTargetFile += sdkBeamableUtilitiesCode;
		}

		// Add the using statements required by the utilities code 
		{
			string startTag = "/* BEAMABLE USINGS TO COPY PASTE START */";
			string endTag = $"/* BEAMABLE USINGS TO COPY PASTE END */";

			var sdkStartIdx = sdkTargetFile.IndexOf(startTag, StringComparison.Ordinal);
			var sdkEndIdx = sdkTargetFile.IndexOf(endTag, StringComparison.Ordinal) + endTag.Length;

			if (sdkStartIdx < 0 || sdkEndIdx < 0)
				throw new CliException("Failed to find the Beam usings code in the SDK's own target file. If you see this, please report a bug to Beamable.");

			var sdkBeamableUsingCode = sdkTargetFile[sdkStartIdx..sdkEndIdx];

			// If it already had the utility code in it, let's remove it for a clean install.
			if (gameMakerTargetFile.Contains(startTag))
			{
				var gameMakerStartIdx = gameMakerTargetFile.IndexOf(startTag, StringComparison.Ordinal);
				var gameMakerEndIdx = gameMakerTargetFile.IndexOf(endTag, StringComparison.Ordinal) + endTag.Length;
				gameMakerTargetFile = gameMakerTargetFile.Remove(gameMakerStartIdx, gameMakerEndIdx - gameMakerStartIdx);
			}

			var gameMakerUsingIdx = gameMakerTargetFile.IndexOf("using ", StringComparison.Ordinal);
			gameMakerTargetFile = gameMakerTargetFile.Insert(gameMakerUsingIdx, $"{sdkBeamableUsingCode}\n");
		}

		// Write the file back in
		File.WriteAllText(gameMakerTargetCsFile, gameMakerTargetFile);

		// Regenerate project files as we made changes to the Target.cs file.
		MachineHelper.RunUnrealGenerateProjectFiles(unrealRootPath);

		return Task.CompletedTask;
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
