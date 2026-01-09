using Beamable.Common;
using Newtonsoft.Json;
using System.CommandLine;

namespace cli;

public class CheckCountersCommandArgs : CommandArgs
{
	public string countersJsonFilePath;
	public double cpuMaxLimitPercent;
	public double memMaxLimitMb;
}

public class CheckCountersCommand : AtomicCommand<CheckCountersCommandArgs, CheckPerfCommandOutput>, IStandaloneCommand
{
	public override bool IsForInternalUse => true;

	public CheckCountersCommand() : base("check-counters", "Read the results of a dotnet-counters json file and determine if there are errors")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("counters-file-path", "The path to the dotnet-counters output json file"), (args, i) => args.countersJsonFilePath = i);
		AddOption(new Option<double>("--cpu-limit", () => 12, "The max cpu spike limit %"), (arg, i) => arg.cpuMaxLimitPercent = i);
		AddOption(new Option<double>("--mem-limit", () => 160, "The max mem spike limit MB"), (arg, i) => arg.memMaxLimitMb = i);
	}

	public override Task<CheckPerfCommandOutput> GetResult(CheckCountersCommandArgs args)
	{
		/*
		 * Check for spikes
		 * Check for increasing memory?
		 * Check for total counts
		 * Check for historical differences? 
		 */

		var counterJson = File.ReadAllText(args.countersJsonFilePath);
		var data = JsonConvert.DeserializeObject<DotnetCountersData>(counterJson);

		// process data into separate categories
		var dataGroups = data.Events.GroupBy(evt => evt.name).ToDictionary(group => group.Key, group => group.ToArray());
		if (!dataGroups.TryGetValue("dotnet.process.cpu.count ({cpu})", out var cpuData))
		{
			throw new CliException("File does not contain CPU data");
		}
		if (!dataGroups.TryGetValue("dotnet.process.memory.working_set (By)", out var memData))
		{
			throw new CliException("File does not contain memory data");
		}

		var warnings = new List<DotnetCounterPerfWarning>();

		var cpuLimit = args.cpuMaxLimitPercent; //%
		var memLimit = args.memMaxLimitMb; //MB
		for (var i = 0; i < cpuData.Length; i++)
		{
			// check for spike
			if (cpuData[i].value > cpuLimit)
			{
				warnings.Add(new DotnetCounterPerfWarning($"CPU utilization spiked beyond limit=[{cpuLimit}] i=[{i}]", cpuData[i]));
			}
		}

		for (var i = 0; i < memData.Length; i++)
		{
			// check for spike
			var mb = memData[i].value / (1000 * 1000); // value is in bytes
			if (mb > memLimit)
			{
				warnings.Add(new DotnetCounterPerfWarning($"Memory utilization spiked beyond limit=[{memLimit}] i=[{i}]", memData[i]));
			}
		}

		if (warnings.Count > 0)
		{
			throw new CliException(string.Join(",", warnings.Select(w => w.ToString())));
		}

		return Task.FromResult(new CheckPerfCommandOutput { message = "No issues found." });
	}
}

public class DotnetCounterPerfWarning
{
	public DotnetCounterEntry entry;
	public string message;

	public DotnetCounterPerfWarning(string message, DotnetCounterEntry entry)
	{
		this.message = message;
		this.entry = entry;
	}

	public override string ToString()
	{
		return message + "\n" + JsonConvert.SerializeObject(entry) + "\n";
	}
}

public class DotnetCountersData
{
	public string TargetProcess;
	public string StartTime;
	public DotnetCounterEntry[] Events;
}
public struct DotnetCounterEntry
{
	public string timestamp;
	public string provider; // System.Runtime
	public string name;
	public string tags;
	public string counterType; // Rate or Metric
	public double value;
}
