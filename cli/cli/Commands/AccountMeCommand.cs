using Beamable.Common.Api.Auth;
using cli.Utils;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Json;

namespace cli;

public class AccountMeCommandArgs : CommandArgs
{
	public bool plainOutput;
}

public class AccountMeCommand : AppCommand<AccountMeCommandArgs>, IResultSteam<DefaultStreamResultChannel, User>
{
	public AccountMeCommand() : base("me", "Temp command to get current account") { }

	public override void Configure()
	{
		AddOption(new PlainOutputOption(), (args, b) => args.plainOutput = b);
	}

	public override async Task Handle(AccountMeCommandArgs args)
	{
		try
		{
			var response = await args.AuthApi.GetUser().ShowLoading("Sending Request...");
			this.SendResults(response);
			var json = JsonConvert.SerializeObject(response);
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
			throw new CliException($"Failed to get user data due to error: {e.Message}");
		}
	}
}
