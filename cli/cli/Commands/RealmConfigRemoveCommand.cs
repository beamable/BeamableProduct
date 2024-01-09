using Beamable.Common;
using Beamable.Common.Api.Realms;
using cli.Utils;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Json;
using System.CommandLine;

namespace cli;

public class RealmConfigRemoveCommandArgs : CommandArgs
{
	public bool plainOutput;
	public string name;
}

public class RealmConfigRemoveCommand : AppCommand<RealmConfigRemoveCommandArgs>, IResultSteam<DefaultStreamResultChannel, RealmConfigView>
{
	public RealmConfigRemoveCommand() : base("remove", "Remove realm config value") { }

	public override void Configure()
	{
		AddOption(new PlainOutputOption(), (args, b) => args.plainOutput = b);
		AddOption(new Option<string>("--name", "The realm config name."),
			(args, i) => args.name = i);
	}

	public override async Task Handle(RealmConfigRemoveCommandArgs args)
	{
		var currentConfig = await GetRealmConfig(args);
		var configName = await GetConfigName(args);
		if (currentConfig.Config.ContainsKey(configName))
		{
			currentConfig.Config.Remove(configName);
		}
		await args.RealmsApi.UpdateRealmConfig(currentConfig);
		await DisplayRealmConfig(args, currentConfig);
	}

	private async Promise<RealmConfigView> GetRealmConfig(RealmConfigRemoveCommandArgs args)
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

	private async Task DisplayRealmConfig(RealmConfigRemoveCommandArgs args, [CanBeNull] RealmConfigView data = null)
	{
		data ??= await GetRealmConfig(args);
		this.SendResults(data);
		var json = JsonConvert.SerializeObject(data);
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
	}

	private Task<string> GetConfigName(RealmConfigRemoveCommandArgs args)
	{
		if (!string.IsNullOrEmpty(args.name))
			return Task.FromResult(args.name);

		return Task.FromResult(AnsiConsole.Prompt(
			new TextPrompt<string>("Please enter realm config [green]name[/]:")
				.PromptStyle("green")
		));
	}
}
