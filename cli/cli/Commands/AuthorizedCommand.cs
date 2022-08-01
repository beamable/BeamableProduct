using Beamable.Common;
using Beamable.Common.Api.Auth;
using cli.Utils;

namespace cli;

public abstract class AuthorizedCommand<T> : AppCommand<T> where T : AuthorizedCommandArgs
{
	private readonly IAppContext _ctx;
	protected readonly IAuthApi AuthApi;

	protected AuthorizedCommand(IAppContext ctx, IAuthApi authApi, string name, string description = null) : base(name, description)
	{
		_ctx = ctx;
		AuthApi = authApi;
	}
	public override void Configure()
	{
		AddOption(new UsernameOption(), (args, i) => args.username = i);
		AddOption(new PasswordOption(), (args, i) => args.password = i);
		AddOption(new CustomerScopedOption(), (args, b) => args.customerScoped = b);
	}

	public override async Task Handle(T args)
	{
		if (!string.IsNullOrWhiteSpace(_ctx.RefreshToken))
		{
			var resp = await AuthApi.LoginRefreshToken(_ctx.RefreshToken);
			_ctx.UpdateToken(resp);
			return;
		}

		if (string.IsNullOrWhiteSpace(args.username) || string.IsNullOrWhiteSpace(args.password))
		{
			BeamableLogger.LogError("Refresh token, username and password are empty, skipping login");
			return;
		}
		var response = await AuthApi.Login(args.username, args.password, true, args.customerScoped).ShowLoading("Authorizing...");
		_ctx.UpdateToken(response);
	}
}

public class AuthorizedCommandArgs : CommandArgs {
	public string username;
	public string password;
	public bool customerScoped;
}
