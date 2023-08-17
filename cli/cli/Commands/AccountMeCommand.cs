using Beamable.Common;
using Beamable.Common.Api.Auth;
using cli.Utils;
using Newtonsoft.Json;
using Spectre.Console;

namespace cli;

public class AccountMeCommandArgs : CommandArgs { }

public class AccountMeCommand : AppCommand<AccountMeCommandArgs>
{

	public AccountMeCommand() : base("me", "Temp command to get current account")
	{}

	public override void Configure(){}

	public override async Task Handle(AccountMeCommandArgs args)
	{
		var response = await args.AuthApi.GetUser().ShowLoading("Sending Request...");
		AnsiConsole.WriteLine(JsonConvert.SerializeObject(response));
	}
}
