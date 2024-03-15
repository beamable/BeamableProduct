using Beamable.Common;
using Beamable.Common.Api.Realms;
using cli.Utils;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Json;

namespace cli;

public class RealmConfigRemoveCommandArgs : CommandArgs
{
	public List<string> keys = new();
}

public class RealmConfigRemoveCommand : AtomicCommand<RealmConfigRemoveCommandArgs, RealmConfigOutput>
{
	public RealmConfigRemoveCommand() : base("remove", "Remove realm config values") { }

	public override void Configure()
	{
		AddOption(new RealmConfigKeyOption(), (args, b) => args.keys = b.ToList());
	}

	public override bool AutoLogOutput => false;

	public override async Task<RealmConfigOutput> GetResult(RealmConfigRemoveCommandArgs args)
	{
		try
		{
			var currentConfig = await GetRealmConfig(args);
			foreach (var key in args.keys)
			{
				var data = RealmConfigInputParser.Parse(key);
				currentConfig.Config.Remove(data.NamespaceKey());
			}
			await args.RealmsApi.UpdateRealmConfig(currentConfig.Config);

			currentConfig = await GetRealmConfig(args);
			LogResult(currentConfig.ConvertToView());
			return currentConfig;
		}
		catch (ArgumentException e)
		{
			AnsiConsole.MarkupLine($"[red]Provide a list of realm config keys in a 'namespace|key' format.[/]");
			AnsiConsole.WriteException(e);
			throw;
		}
	}

	private async Promise<RealmConfigOutput> GetRealmConfig(RealmConfigRemoveCommandArgs args)
	{
		try
		{
			var res = await args.RealmsApi.GetRealmConfig().ShowLoading("Sending Request...");
			return new RealmConfigOutput { Config = res.Config };
		}
		catch (Exception e)
		{
			throw new CliException($"Failed to get realm config data due to error: {e.Message}");
		}
	}
}
