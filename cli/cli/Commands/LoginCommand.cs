using Beamable.Common.Api.Auth;

namespace cli;

public class LoginCommandArgs : CommandArgs
{
	public string username;
	public string password;
}

public class LoginCommand : AppCommand<LoginCommandArgs>
{
	private readonly IAuthApi _authApi;

	public LoginCommand(IAuthApi authApi) : base("login", "save credentials to file")
	{
		_authApi = authApi;
	}

	public override void Configure()
	{
		AddOption(new UsernameOption(), (args, i) => args.username = i);
		AddOption(new PasswordOption(), (args, i) => args.password = i);
	}

	public override async Task Handle(LoginCommandArgs args)
	{
		var response = await _authApi.Login(args.username, args.password, true, true);
		Console.WriteLine(response?.access_token);
		// now save the token if login succeed
	}
}
