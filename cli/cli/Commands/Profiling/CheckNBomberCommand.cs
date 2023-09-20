using Beamable.Common;
using Csv;
using Newtonsoft.Json;
using System.CommandLine;

namespace cli;

public class CheckNBomberCommandArgs : CommandArgs
{
	public string nBomberJsonFilePath;
	public double failLimit;
	public double p95Limit;
}
public class CheckNBomberCommand : AppCommand<CheckNBomberCommandArgs>, IStandaloneCommand
{
	public CheckNBomberCommand() : base("check-nbomber", "Read the results of a n-bomber .csv file and determine if there are errors")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("nbomber-file-path", "The path to the nbomber output csv file"), (args, i) => args.nBomberJsonFilePath = i);
		AddOption(new Option<double>("--fail-limit", () => 0, "The max number of failed requests"), (arg, i) => arg.failLimit = i);
		AddOption(new Option<double>("--p95-limit", () => 2500, "The max p95 in ms"), (arg, i) => arg.p95Limit = i);
	}

	public override Task Handle(CheckNBomberCommandArgs args)
	{
		var csv = File.ReadAllText(args.nBomberJsonFilePath);
		var lines = CsvReader.ReadFromText(csv);
		var warnings = new List<string>();

		foreach (var line in lines)
		{
			if (!double.TryParse(line["failed"], out var failCount))
			{
				warnings.Add("No parsable failed column");
			}

			if (failCount > args.failLimit)
			{
				warnings.Add($"fails above limit. fails=[{failCount}] limit=[{args.failLimit}]");
			}

			if (!double.TryParse(line["95_percent"], out var p95))
			{
				warnings.Add("No parsable p95 column");
			}

			if (p95 > args.p95Limit)
			{
				warnings.Add($"p95 above limit. p95=[{p95}] limit=[{args.p95Limit}]");
			}

		}

		if (warnings.Count > 0)
		{
			throw new CliException(string.Join(",", warnings));
		}

		BeamableLogger.Log("No issues found.");
		return Task.CompletedTask;
	}
}
