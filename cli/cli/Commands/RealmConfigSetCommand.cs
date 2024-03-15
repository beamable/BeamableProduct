using Beamable.Common;
using Beamable.Common.Api.Realms;
using cli.Utils;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Json;

namespace cli;

public class RealmConfigSetCommandArgs : CommandArgs
{
	public List<string> keyValuePairs = new();
}

public class RealmConfigSetCommand : AtomicCommand<RealmConfigSetCommandArgs, RealmConfigOutput>
{
	public RealmConfigSetCommand() : base("set", "Set realm config values") { }

	public override void Configure()
	{
		AddOption(new RealmConfigKeyValueOption(), (args, b) => args.keyValuePairs = b.ToList());
	}

	public override async Task<RealmConfigOutput> GetResult(RealmConfigSetCommandArgs args)
	{
		try
		{
			var currentConfig = await GetRealmConfig(args);
			foreach (var pair in args.keyValuePairs)
			{
				var data = RealmConfigInputParser.Parse(pair);
				currentConfig.Config[data.NamespaceKey()] = data.Value;
			}
			await args.RealmsApi.UpdateRealmConfig(currentConfig.Config);

			currentConfig = await GetRealmConfig(args);
			LogResult(currentConfig.ConvertToView());
			return new RealmConfigOutput
			{
				Config = currentConfig.Config
			};
		}
		catch (ArgumentException e)
		{
			AnsiConsole.MarkupLine($"[red]Provide a list of realm config key/value pairs in a 'namespace|key::value' format.[/]");
			AnsiConsole.WriteException(e);
			throw;
		}
	}

	private async Promise<RealmConfigData> GetRealmConfig(RealmConfigSetCommandArgs args)
	{
		try
		{
			return await args.RealmsApi.GetRealmConfig().ShowLoading("Sending Request...");
		}
		catch (Exception e)
		{
			throw new CliException($"Failed to get realm config data due to error: {e.Message}");
		}
	}
}
