using Beamable.Common.Api.Realms;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Json;

namespace cli;

public class RealmConfigCommandArgs : CommandArgs
{
	public bool plainOutput;
}

public class RealmConfigCommand : AppCommand<RealmConfigCommandArgs>, IResultSteam<DefaultStreamResultChannel, RealmConfigView>
{
	public RealmConfigCommand() : base("realm", "Get current realm config values") { }

	public override void Configure()
	{
		AddOption(new PlainOutputOption(), (args, b) => args.plainOutput = b);
	}

	public override async Task Handle(RealmConfigCommandArgs args)
	{
		try
		{
			var data = await args.RealmsApi.GetRealmConfig();
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
		catch (Exception e)
		{
			throw new CliException($"Failed to get realm config data due to error: {e.Message}");
		}
	}
}
