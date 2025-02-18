using Beamable.Common;
using Beamable.Common.BeamCli;
using cli.Services;
using CliWrap;
using Errata;
using Serilog;
using Spectre.Console;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;
using microservice.Extensions;
using Command = System.CommandLine.Command;

namespace cli.Version;




public class VersionInstallCommandArgs : CommandArgs
{
	public string version;
}

public class VersionInstallCommand : AppCommand<VersionInstallCommandArgs>
	, IStandaloneCommand
	, IHaveRedirectionConcerns<VersionInstallCommandArgs>
{
	public VersionInstallCommand() : base("install", "Install a different version of the CLI")
	{
		AddAlias("update");
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("version", () => "latest", "The version of the CLI to install"),
			(args, i) => args.version = i);
	}

	public override async Task Handle(VersionInstallCommandArgs args)
	{
		
		var service = args.DependencyProvider.GetService<VersionService>();
		var currentVersionInfo = await service.GetInformationData(args.ProjectService);

		Log.Debug($"setting up CLI install... scope=[{currentVersionInfo.installType}] ");
		
		var data = await service.GetBeamableToolPackageVersions();
		
		var packageVersion = args.version?.ToLower() switch
		{
			"latest" when currentVersionInfo.version.StartsWith("0.0.123") => await GetLatestLocalVersion(args),
			"latest" => data.LastOrDefault(d => !d.packageVersion.Contains("preview")),
			"latest-rc" => data.LastOrDefault(d => d.packageVersion.Contains("preview.rc")),
			(var version) when string.IsNullOrEmpty(version) => throw new CliException($"Given version is not valid. version=[{args.version}]"),
			
			// 0.0.123 is a special "dev" version of the package. It is not supposed to exist on nuget.org. Instead, it comes from the developer's local machine's nuget source.
			(var version) when version.StartsWith("0.0.123") => await GetLatestLocalVersion(args),

			(var version) => data.LastOrDefault(d => d.packageVersion == version)
		};
		if (packageVersion == null)
		{
			if (!PackageVersion.TryFromSemanticVersionString(args.version, out var parsedVersion))
			{
				throw new CliException(
					$"Given version is not available. Use `beam version ls` to view available versions. version=[{args.version}]");
			}

			packageVersion = new VersionService.NugetPackages
			{
				originalVersion = args.version,
				packageVersion = parsedVersion.ToString()
			};
		}


		if (args.Dryrun)
		{
			Log.Information($"Preventing install due to dry run. Would have installed version={packageVersion.originalVersion}");
			return;
		}

		if (!args.Quiet)
		{
			var shouldContinue = AnsiConsole.Prompt(new ConfirmationPrompt(
				$"Are you sure you want to install Beam CLI Version={packageVersion.originalVersion}"));
			if (!shouldContinue)
			{
				return;
			}
		}
		
		var scope = currentVersionInfo.installType == VersionService.VersionInstallType.GlobalTool
			? "global"
			: "local";

		if (packageVersion.packageVersion.StartsWith("0.0.123"))
		{
			// always set the scope to be local for the local dev version.
			//  to install the local tool globally, use the dev.sh script
			scope = "local";
		}
		
		await CliExtensions.GetDotnetCommand(args.AppContext.DotnetPath, $"tool update Beamable.Tools --allow-downgrade --{scope} --version {packageVersion.originalVersion}")
			.WithValidation(CommandResultValidation.ZeroExitCode)
			.ExecuteAsyncAndLog();

		Log.Information($"Beam CLI version=[{packageVersion.originalVersion}] installed successfully as a {scope} tool. Use `beam version` or `beam --version` to verify.");

		// if the tool is local, then likely, the microservices need to be restored as well.
		if (scope == "local")
		{
			foreach (var local in args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions)
			{
				// skip services that don't have local source code
				if (!local.IsLocal) continue;
				Log.Information($"Restoring local project=[{local.AbsoluteProjectPath}]");
				try
				{
					await CliExtensions.GetDotnetCommand(args.AppContext.DotnetPath, $"restore {local.AbsoluteProjectPath.EnquotePath()}")
						.WithValidation(CommandResultValidation.ZeroExitCode)
						.ExecuteAsyncAndLog();
				}
				catch (Exception ex)
				{
					Log.Error($"Failed to restore local project=[{local.AbsoluteProjectPath}] message=[{ex.Message}]");
					throw new CliException(ex.Message);
				}
			}
		}
	}

	public void ValidationRedirection(InvocationContext context, Command command, VersionInstallCommandArgs args,
		StringBuilder errorStream, out bool isValid)
	{
		isValid = true;
		if (!args.Quiet)
		{
			errorStream.AppendLine("Must include the quiet flag.");
			isValid = false;
		}
	}

	public void WriteValidationMessage(Command command, TextWriter writer)
	{
		writer.WriteLine("The quiet flag must be used.");
	}

	async Task<VersionService.NugetPackages> GetLatestLocalVersion(VersionInstallCommandArgs args)
	{
		// this is a dev install...
		// the user did not specify a version, that means update to the latest dev version.
		Log.Information("updating local dev version...");

		// need to resolve to the latest version... 
		var sourcePath = "";
		var highestBuildNumber = 0;


		{
			// find the path to the local beamable nuget feed
			var sourceOutput = new StringBuilder();
			await CliExtensions.GetDotnetCommand(args.AppContext.DotnetPath, "nuget list source")
				.WithValidation(CommandResultValidation.ZeroExitCode)
				.WithStandardOutputPipe(PipeTarget.ToStringBuilder(sourceOutput))
				.ExecuteAsync();
			var sourceOutputLines = sourceOutput.ToString().Split(Environment.NewLine);
			Log.Verbose($"nuget list output\n{sourceOutput}");
			for (var i = 0; i < sourceOutputLines.Length - 1; i++)
			{
				if (sourceOutputLines[i].Contains("BeamableNugetSource"))
				{
					sourcePath = sourceOutputLines[i + 1].Trim();
					break;
				}
			}

			if (string.IsNullOrEmpty(sourcePath))
			{
				throw new CliException("Cannot find beamable nuget source. Please run the setup.sh script");
			}
			Log.Verbose($"Found source path=[{sourcePath}]");
		}

		{
			// search through the files in the nuget feed and find the highest 0.0.123.x version
			var packages = Directory.GetFiles(sourcePath);
			foreach (var package in packages)
			{
				var packageDotParts = package.Split('.', StringSplitOptions.RemoveEmptyEntries);
				var dotPartNumberCount = 0;
				foreach (var dotPart in packageDotParts)
				{
					if (!int.TryParse(dotPart, out var versionNumber))
					{
						continue;
					}

					dotPartNumberCount++; // this is part of the semver...

					// the first parts of the semver must be the developer version...
					if (dotPartNumberCount == 1 && versionNumber != 0) break;
					if (dotPartNumberCount == 2 && versionNumber != 0) break;
					if (dotPartNumberCount == 3 && versionNumber != 123) break;

					if (dotPartNumberCount ==
					    4) // if this is the 4th one we've seen, then its the build number we are after!
					{
						// keep track of the highest build number we've seen!
						if (versionNumber > highestBuildNumber)
						{
							highestBuildNumber = versionNumber;
						}
					}
				}
			}
		}


		var versionString = $"0.0.123.{highestBuildNumber}";
		return new VersionService.NugetPackages { originalVersion = versionString, packageVersion = versionString };
	}
}
