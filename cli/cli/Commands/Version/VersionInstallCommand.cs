using Beamable.Common.BeamCli;
using cli.Services;
using CliWrap;
using Errata;
using Serilog;
using Spectre.Console;
using System.CommandLine;

namespace cli.Version;


public class SpecialErrorStream<T> : IResultChannel
{
	public string ChannelName { get; }
}

public class SpecialErrorData<T>
{
	public T customData;
	public ErrorOutput common;
}
public interface IThrowAnException<T> : IResultSteam<IThrowAnException<T>.ErrorStream, T>
	// where T
{
	public class ErrorStream : IResultChannel
	{
		public string ChannelName => "error-" + typeof(T).Name;
	}
}


public class VersionInstallCommandArgs : CommandArgs
{
	public string version;
}

public class GrumpyData : ErrorOutput
{
	public int x;
}
public class CliExceptionSub1 : CliException<GrumpyData>
{
	public CliExceptionSub1(GrumpyData data) : base("grumpy")
	{
		payload = data;
	}

	public CliExceptionSub1(string message, int nonZeroOrOneExitCode, bool useStdOut, string additionalNote = null, IEnumerable<Diagnostic> additionalReports = null) : base(message, nonZeroOrOneExitCode, useStdOut, additionalNote, additionalReports)
	{
	}
}


public class VersionInstallCommand : AppCommand<VersionInstallCommandArgs>
	, IStandaloneCommand
	, IThrowAnException<GrumpyData>
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
		throw new CliException<GrumpyData>(new GrumpyData
		{
			x =3
		});
		var service = args.DependencyProvider.GetService<VersionService>();
		var currentVersionInfo = await service.GetInformationData(args.ProjectService);

		Log.Debug($"setting up CLI install... scope=[{currentVersionInfo.installType}] ");

		var data = await service.GetBeamableToolPackageVersions();

		var packageVersion = args.version?.ToLower() switch
		{
			// 0.0.123 is a special "dev" version of the package. It is not supposed to exist on nuget.org. Instead, it comes from the developer's local machine's nuget source.
			"0.0.123" => new VersionService.NugetPackages
			{
				originalVersion = "0.0.123",
				packageVersion = "0.0.123"
			},
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
		await CliExtensions.GetDotnetCommand(args.AppContext.DotnetPath, $"tool update Beamable.Tools --allow-downgrade --{scope} --version {packageVersion.originalVersion}")
			.WithValidation(CommandResultValidation.ZeroExitCode)
			.ExecuteAsyncAndLog();

		Log.Information($"Beam CLI version=[{packageVersion.originalVersion}] installed successfully as a {scope} tool. Use `beam version` or `beam --version` to verify.");
	}

}
