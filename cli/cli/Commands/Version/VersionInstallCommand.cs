using cli.Services;
using CliWrap;
using Serilog;
using Spectre.Console;
using System.CommandLine;

namespace cli.Version;

public class VersionInstallCommandArgs : CommandArgs
{
	public string version;
	public bool quiet;
}

public class VersionInstallCommand : AppCommand<VersionInstallCommandArgs>, IStandaloneCommand
{
	public VersionInstallCommand() : base("install", "Install a different version of the CLI")
	{

	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("version", () => "latest", "The version of the CLI to install"),
			(args, i) => args.version = i);

		var option = AddOption(new Option<bool>("--quiet", () => false, "When true, no prompts will be displayed"),
			(args, i) => args.quiet = i);
		option.AddAlias("-q");
	}

	public override async Task Handle(VersionInstallCommandArgs args)
	{
		var service = args.DependencyProvider.GetService<VersionService>();
		var currentVersionInfo = await service.GetInformationData(args.ProjectService);

		if (currentVersionInfo.installType != VersionService.VersionInstallType.GlobalTool)
		{
			throw new CliException(
				$"This command can only update a globally installed Beamable CLI via dotnet tools. " +
				$"Use the `dotnet tool` suite of commands to manage the install. " +
				$"Use `beam version` to discover where this Beam CLI install is located. ");
		}

		var data = await service.GetBeamableToolPackageVersions();

		var packageVersion = args.version?.ToLower() switch
		{
			"latest" => data.LastOrDefault(d => !d.packageVersion.Contains("preview")),
			"latest-rc" => data.LastOrDefault(d => d.packageVersion.Contains("preview.rc")),
			(var version) when string.IsNullOrEmpty(version) => throw new CliException($"Given version is not valid. version=[{args.version}]"),
			(var version) => data.LastOrDefault(d => d.packageVersion == version)
		};
		if (packageVersion == null)
		{
			throw new CliException(
				$"Given version is not available on Nuget. Use `beam version ls` to view available versions. version=[{args.version}]");
		}


		if (args.Dryrun)
		{
			Log.Information($"Preventing install due to dry run. Would have installed version={packageVersion.originalVersion}");
			return;
		}

		if (!args.quiet)
		{
			var shouldContinue = AnsiConsole.Prompt(new ConfirmationPrompt(
				$"Are you sure you want to install Beam CLI Version={packageVersion.originalVersion}"));
			if (!shouldContinue)
			{
				return;
			}
		}

		await CliExtensions.GetDotnetCommand(args.AppContext.DotnetPath, $"tool update Beamable.Tools --global --version {packageVersion.originalVersion}")
			.WithValidation(CommandResultValidation.ZeroExitCode)
			.ExecuteAsyncAndLog();

		Log.Information($"Beam CLI version=[{packageVersion.originalVersion}] installed successfully as a global tool. Use `beam version` or `beam --version` to verify.");
	}

}
