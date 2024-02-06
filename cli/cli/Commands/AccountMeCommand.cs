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

public class AccountMeCommand : AtomicCommand<AccountMeCommandArgs, User>
{
	public AccountMeCommand() : base("me", "Fetch the current account") { }

	public override void Configure()
	{
		AddOption(new PlainOutputOption(), (args, b) => args.plainOutput = b);
	}

	protected override User GetHelpInstance()
	{
		return new User
		{
			email = "user@example.com",
			deviceIds = new List<string>(),
			scopes = new List<string>(),
			language = "en",
			thirdPartyAppAssociations = new List<string>(),
			external = new List<ExternalIdentity>
			{
			}
		};
	}

	public override async Task<User> GetResult(AccountMeCommandArgs args)
	{
		try
		{
			var response = await args.AuthApi.GetUser().ShowLoading("Sending Request...");
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

			return response;
		}
		catch (Exception e)
		{
			throw new CliException($"Failed to get user data due to error: {e.Message}");
		}
	}
}
