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
	public bool plainOutput;
	public List<string> keys = new();
}

public class RealmConfigRemoveCommand : AtomicCommand<RealmConfigRemoveCommandArgs, RealmConfigData>
{
	public RealmConfigRemoveCommand() : base("remove", "Remove realm config values") { }

	public override void Configure()
	{
		AddOption(new PlainOutputOption(), (args, b) => args.plainOutput = b);
		AddOption(new RealmConfigKeyOption(), (args, b) => args.keys = b.ToList());
	}

	public override async Task<RealmConfigData> GetResult(RealmConfigRemoveCommandArgs args)
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
			return await DisplayRealmConfig(args, currentConfig);
		}
		catch (ArgumentException e)
		{
			AnsiConsole.MarkupLine($"[red]Provide a list of realm config keys in a 'namespace|key' format.[/]");
			AnsiConsole.WriteException(e);
			throw;
		}
	}

	private async Promise<RealmConfigData> GetRealmConfig(RealmConfigRemoveCommandArgs args)
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

	private async Task<RealmConfigData> DisplayRealmConfig(RealmConfigRemoveCommandArgs args, [CanBeNull] RealmConfigData data = null)
	{
		data ??= await GetRealmConfig(args);

		var json = JsonConvert.SerializeObject(data.ConvertToView());
		if (args.plainOutput)
		{
			AnsiConsole.WriteLine(json);
		}
		else
		{
			AnsiConsole.Write(
				new Panel(new JsonText(json))
					.Header("Server response")
					.Collapse()
					.RoundedBorder());
		}

		return data;
	}
}
