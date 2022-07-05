using Beamable.Common.Api.Auth;
using Newtonsoft.Json;
using Spectre.Console;
using System.Net.Http.Json;

namespace cli;

public class AccountMeCommandArgs : CommandArgs
{

}
public class AccountMeCommand : AppCommand<AccountMeCommandArgs>
{
	private readonly IAuthApi _auth;

	public AccountMeCommand(IAuthApi auth) : base("me", "temp command to get current account")
	{
		_auth = auth;
	}

	public override void Configure()
	{

	}

	public override async Task Handle(AccountMeCommandArgs args)
	{
		var response = await AnsiConsole.Status()
			.Spinner(Spinner.Known.Default)
			.StartAsync("Sending Request...", async ctx =>

				await _auth.GetUser()
			);
		Console.WriteLine(JsonConvert.SerializeObject(response));
	}
}
