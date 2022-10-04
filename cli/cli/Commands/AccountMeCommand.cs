using Beamable.Common;
using Beamable.Common.Api.Auth;
using cli.Utils;
using Newtonsoft.Json;

namespace cli;

public class AccountMeCommandArgs : CommandArgs { }

public class AccountMeCommand : AppCommand<AccountMeCommandArgs>
{
	public IAuthApi AuthApi { get; }

	public AccountMeCommand(IAuthApi authApi) : base("me", "temp command to get current account")
	{
		AuthApi = authApi;
	}

	public override void Configure()
	{

	}

	public override async Task Handle(AccountMeCommandArgs args)
	{
		var response = await AuthApi.GetUser().ShowLoading("Sending Request...");
		BeamableLogger.Log(JsonConvert.SerializeObject(response));
	}
}
