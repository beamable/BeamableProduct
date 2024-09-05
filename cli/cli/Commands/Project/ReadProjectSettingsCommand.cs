using cli.Dotnet;
using Serilog;

namespace cli.Commands.Project;

public class ReadProjectSettingsCommandArgs : CommandArgs
{
	public List<string> services = new List<string>();

}

[Serializable]
public class ProjectSettingsOutput
{
	public string serviceName;
	public List<SettingOutput> settings = new List<SettingOutput>();
}

[Serializable]
public class SettingOutput
{
	public string key, value;
}

public class ReadProjectSettingsCommandOutput
{
	public List<ProjectSettingsOutput> settings = new List<ProjectSettingsOutput>();
}

public class ReadProjectSettingsCommand : AtomicCommand<ReadProjectSettingsCommandArgs, ReadProjectSettingsCommandOutput>
{
	public override bool IsForInternalUse => true;

	public ReadProjectSettingsCommand() : base("read-settings", "Get the localDev settings for the beamable workspace")
	{
	}

	public override void Configure()
	{
		ProjectCommand.AddIdsOption(this, (args, i) => args.services = i);

	}

	public override Task<ReadProjectSettingsCommandOutput> GetResult(ReadProjectSettingsCommandArgs args)
	{
		var res = new ReadProjectSettingsCommandOutput();
		ProjectCommand.FinalizeServicesArg(args, ref args.services);

		
		foreach (var (service, http) in args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols)
		{
			if (!args.services.Contains(service)) continue; // skip
			var collection = new ProjectSettingsOutput { serviceName = service };
			res.settings.Add(collection);
			foreach (var (key, value) in http.Settings)
			{
				Log.Verbose($"found info service=[{service}] key=[{key}] val=[{value}]" );
				collection.settings.Add(new SettingOutput
				{
					key = key, value = value
				});
			}
		}

		return Task.FromResult(res);
	}
}
