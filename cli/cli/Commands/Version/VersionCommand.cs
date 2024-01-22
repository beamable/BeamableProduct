using cli.Services;
using Serilog;
using System.CommandLine;

namespace cli.Version;

public class VersionCommandArgs : CommandArgs
{
	public bool showTemplates = true;
	public bool showVersion = true;
	public bool showLocation = true;
	public bool showType = true;
	public string output;
}

public class VersionResults
{
	public string version, location, type, templates;
}


public class VersionCommand : AtomicCommand<VersionCommandArgs, VersionResults>, IStandaloneCommand
{
	public VersionCommand() : base("version", "Commands for managing the CLI version")
	{

	}

	public override void Configure()
	{
		AddOption(new Option<bool>("--show-version", () => true, "Displays the executing CLI version"), (args, i) => args.showVersion = i);
		AddOption(new Option<bool>("--show-location", () => true, "Displays the executing CLI install location"), (args, i) => args.showLocation = i);
		AddOption(new Option<bool>("--show-templates", () => true, "Displays available Beamable template version"), (args, i) => args.showTemplates = i);
		AddOption(new Option<bool>("--show-type", () => true, "Displays the executing CLI install type"), (args, i) => args.showType = i);
		AddOption(new Option<string>("--output", () => "log", "How to display the information, anything other than log will print straight to console with no labels"), (args, i) => args.output = i);
	}

	public override async Task<VersionResults> GetResult(VersionCommandArgs args)
	{
		var info = await args.DependencyProvider.GetService<VersionService>().GetInformationData(args.ProjectService);

		if (args.showVersion)
		{
			Print("version", info.version);
		}

		if (args.showLocation)
		{
			Print("location", info.location);
		}

		if (args.showTemplates)
		{

			Print("templates", info.templateVersion);
		}

		if (args.showType)
		{
			Print("install-type", info.installType.ToString());
		}

		return new VersionResults
		{
			location = info.location,
			templates = info.templateVersion,
			type = info.installType.ToString(),
			version = info.version
		};

		void Print(string label, string data)
		{
			if (args.output == "log")
			{
				Log.Information($"{label} -- {data}");
			}
			else
			{
				Console.WriteLine(data);
			}
		}
	}


}
