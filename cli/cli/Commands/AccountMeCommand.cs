using Beamable.Common.Api.Auth;
using cli.Utils;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Json;
namespace cli;

public class AccountMeCommandArgs : CommandArgs
{
}

public class AccountMeCommand : AtomicCommand<AccountMeCommandArgs, User>
{
	public override int Order => 200;
	public AccountMeCommand() : base("me", "Fetch the current account") { }

	public override void Configure()
	{
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
			return response;
		}
		catch (Exception e)
		{
			throw new CliException($"Failed to get user data due to error: {e.Message}");
		}
	}
}
