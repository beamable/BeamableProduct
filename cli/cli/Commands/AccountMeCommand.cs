using Beamable.Common;
using Beamable.Common.Api.Auth;
using cli.Utils;
using Newtonsoft.Json;

namespace cli;

public class AccountMeCommandArgs : AuthorizedCommandArgs
{

}
public class AccountMeCommand : AuthorizedCommand<AccountMeCommandArgs>
{

	public AccountMeCommand(IAppContext ctx, IAuthApi authApi) : base(ctx, authApi, "me", "temp command to get current account")
	{}

	public override void Configure()
	{

	}

	public override async Task Handle(AccountMeCommandArgs args)
	{
		await base.Handle(args);
		var response = await AuthApi.GetUser().ShowLoading("Sending Request...");
		BeamableLogger.Log(JsonConvert.SerializeObject(response));
	}
}
