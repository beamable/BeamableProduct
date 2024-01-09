using Beamable.Common;
using Beamable.Common.Api.Realms;
using cli.Utils;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Json;
using System.CommandLine;

namespace cli;

public class RealmConfigSetCommandArgs : CommandArgs
{
	public bool plainOutput;
	public string name;
	public string value;
}

public class RealmConfigSetCommand : AppCommand<RealmConfigSetCommandArgs>, IResultSteam<DefaultStreamResultChannel, RealmConfigView>
{
	public RealmConfigSetCommand() : base("set", "Set realm config value") { }

	public override void Configure()
	{
		AddOption(new PlainOutputOption(), (args, b) => args.plainOutput = b);
		AddOption(new Option<string>("--name", "The realm config name."),
			(args, i) => args.name = i);
		AddOption(new Option<string>("--value", "The realm config value."),
			(args, i) => args.value = i);
	}

	public override async Task Handle(RealmConfigSetCommandArgs args)
	{
		var currentConfig = await GetRealmConfig(args);
		var configName = await GetConfigName(args);
		var configValue = await GetConfigValue(args);
		currentConfig.Config[configName] = configValue;
		await args.RealmsApi.UpdateRealmConfig(currentConfig);
		await DisplayRealmConfig(args, currentConfig);
	}

	private async Promise<RealmConfigView> GetRealmConfig(RealmConfigSetCommandArgs args)
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

	private async Task DisplayRealmConfig(RealmConfigSetCommandArgs args, [CanBeNull] RealmConfigView data = null)
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

	private Task<string> GetConfigName(RealmConfigSetCommandArgs args)
	{
		if (!string.IsNullOrEmpty(args.name))
			return Task.FromResult(args.name);

		return Task.FromResult(AnsiConsole.Prompt(
			new TextPrompt<string>("Please enter realm config [green]name[/]:")
				.PromptStyle("green")
		));
	}

	private Task<string> GetConfigValue(RealmConfigSetCommandArgs args)
	{
		if (!string.IsNullOrEmpty(args.value))
			return Task.FromResult(args.value);

		return Task.FromResult(AnsiConsole.Prompt(
			new TextPrompt<string>("Please enter realm config [green]value[/]:")
				.PromptStyle("green")
		));
	}
}
