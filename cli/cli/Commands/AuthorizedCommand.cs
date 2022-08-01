using Beamable.Common;
using Beamable.Common.Api.Auth;
using cli.Utils;

namespace cli;

public abstract class AuthorizedCommand<T> : AppCommand<T> where T : AuthorizedCommandArgs
{
	private readonly IAppContext _ctx;
	protected readonly IAuthApi AuthApi;
	public AuthorizedCommand(IAppContext ctx, IAuthApi authApi, string name, string description = null) : base(name, description)
	{
		_ctx = ctx;
		AuthApi = authApi;
	}
	public override void Configure()
	{
		AddOption(new UsernameOption(), (args, i) => args.username = i);
		AddOption(new PasswordOption(), (args, i) => args.password = i);
	}

	public override async Task Handle(T args)
	{
		BeamableLogProvider.Provider.Info($"{args.username}, {args.password}");
		var response = await AuthApi.Login(args.username, args.password, true, false).ShowLoading("Authorizing...");
		_ctx.UpdateToken(response);
		BeamableLogProvider.Provider.Info($"{response.access_token}, {response.refresh_token}");
	}
}

public class AuthorizedCommandArgs : CommandArgs {
	public string username;
	public string password;
}
