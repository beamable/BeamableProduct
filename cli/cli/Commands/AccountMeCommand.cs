using Beamable.Common;
using Beamable.Common.Api.Auth;
using cli.Utils;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Json;

namespace cli;

public class AccountMeCommandArgs : CommandArgs { }

public class AccountMeCommand : AppCommand<AccountMeCommandArgs>
{
	public AccountMeCommand() : base("me", "Temp command to get current account"){}

	public override void Configure(){}

	public override async Task Handle(AccountMeCommandArgs args)
	{
		try
		{
			var response = await args.AuthApi.GetUser().ShowLoading("Sending Request...");
			AnsiConsole.Write(
				new Panel(new JsonText(JsonConvert.SerializeObject(response)))
					.Header("Server response")
					.Collapse()
					.RoundedBorder());
		}
		catch (Exception e)
		{
			throw new CliException($"Failed to get user data due to error: {e.Message}");
		}
	}
}
