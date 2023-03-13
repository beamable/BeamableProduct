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

public class CheckCountersCommand : AppCommand<CheckCountersCommandArgs>
{
	public CheckCountersCommand() : base("check-counters", "read the results of a dotnet-counters json file and determine if there are errors")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("counters-file-path", "the path to the dotnet-counters output json file"), (args, i) => args.countersJsonFilePath = i);
		AddOption(new Option<double>("--cpu-limit", () => 12, "the max cpu spike limit %"), (arg, i) => arg.cpuMaxLimitPercent = i);
		AddOption(new Option<double>("--mem-limit", () => 160, "the max mem spike limit MB"), (arg, i) => arg.memMaxLimitMb = i);
	}

	public override Task Handle(CheckCountersCommandArgs args)
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
		if (!dataGroups.TryGetValue("CPU Usage (%)", out var cpuData))
		{
			throw new CliException("File does not contain CPU data");
		}
		if (!dataGroups.TryGetValue("Working Set (MB)", out var memData))
		{
			throw new CliException("File does not contain memory data");
		}

		var warnings = new List<DotnetCounterPerfWarning>();

		var cpuLimit = args.cpuMaxLimitPercent; //%
		var memLimit = args.memMaxLimitMb; //MB
		for (var i = 0; i < cpuData.Length; i++)
		{
			// check for spike
			if (cpuData[i].value > cpuLimit) // 12% CPU utilization
			{
				warnings.Add(new DotnetCounterPerfWarning($"CPU utilization spiked beyond limit=[{cpuLimit}] i=[{i}]", cpuData[i]));
			}
		}
		
		for (var i = 0; i < memData.Length; i++)
		{
			// check for spike
			if (memData[i].value > memLimit) // 12% CPU utilization
			{
				warnings.Add(new DotnetCounterPerfWarning($"Memory utilization spiked beyond limit=[{memLimit}] i=[{i}]", memData[i]));
			}
		}

		if (warnings.Count > 0)
		{
			throw new CliException(string.Join(",", warnings.Select(w => w.ToString())), true);
		}

		BeamableLogger.Log("No issues found.");
		return Task.CompletedTask;
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
// {
// "timestamp": "2023-02-28 22:09:30Z",
// "provider": "System.Runtime",
// "name": "POH (Pinned Object Heap) Size (B)",
// "tags": "",
// "counterType": "Metric",
// "value": 0
// },
