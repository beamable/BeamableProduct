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
		AddOption(new RefreshTokenOption(), (args, s) => args.refreshToken = s);
	}

	public override async Task Handle(T args)
	{
		if (!string.IsNullOrWhiteSpace(args.refreshToken))
		{
			var resp = await AuthApi.LoginRefreshToken(args.refreshToken);
			BeamableLogProvider.Provider.Info($"{resp.access_token}, {resp.refresh_token}");
			_ctx.UpdateToken(resp);
			return;
		}

		if (string.IsNullOrWhiteSpace(args.username) || string.IsNullOrWhiteSpace(args.password))
		{
			BeamableLogger.LogError("Username or password is empty, skipping login");
			return;
		}
		var response = await AuthApi.Login(args.username, args.password, true, false).ShowLoading("Authorizing...");
		_ctx.UpdateToken(response);
		BeamableLogProvider.Provider.Info($"{response.access_token}, {response.refresh_token}");
	}
}

public class AuthorizedCommandArgs : CommandArgs {
	public string username;
	public string password;
	public string refreshToken;
}
