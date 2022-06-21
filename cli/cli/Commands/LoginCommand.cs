using Beamable.Common.Api.Auth;
using Spectre.Console;

namespace cli;

public class LoginCommandArgs : CommandArgs
{
	public string username;
	public string password;
}

public class LoginCommand : AppCommand<LoginCommandArgs>
{
	private readonly IAppContext _ctx;
	private readonly ConfigService _configService;
	private readonly IAuthApi _authApi;

	public LoginCommand(IAppContext ctx, ConfigService configService, IAuthApi authApi) : base("login", "save credentials to file")
	{
		_ctx = ctx;
		_configService = configService;
		_authApi = authApi;
	}

	public override void Configure()
	{
		AddOption(new UsernameOption(), (args, i) => args.username = i);
		AddOption(new PasswordOption(), (args, i) => args.password = i);
	}

	public override async Task Handle(LoginCommandArgs args)
	{
		var username = GetUserName(args);
		var password = GetPassword(args);

		var response = await AnsiConsole.Status()
			.Spinner(Spinner.Known.Circle)
			.StartAsync("Authorizing...", async ctx =>

				await _authApi.Login(username, password, true, true)
			);

		_configService.SetConfigString(Constants.CONFIG_ACCESS_TOKEN, response.access_token);
		_configService.SetConfigString(Constants.CONFIG_REFRESH_TOKEN, response.refresh_token);
		_configService.FlushConfig();


	}

	private string GetUserName(LoginCommandArgs args)
	{
		if (!string.IsNullOrEmpty(args.username)) return args.username;
		return AnsiConsole.Prompt(
			new TextPrompt<string>("Please enter your [green]username[/]:")
				.PromptStyle("green")
				.ValidationErrorMessage("[red]Not a valid username[/]")
				.Validate(email =>
				{
					if (!email.Contains("@")) return ValidationResult.Error("[red]username must be an email[/]");
					return ValidationResult.Success();
				})).ToString();
	}


	private string GetPassword(LoginCommandArgs args)
	{
		if (!string.IsNullOrEmpty(args.password)) return args.password;
		return AnsiConsole.Prompt(
			new TextPrompt<string>("Please enter your [green]password[/]:")
				.PromptStyle("green")
				.Secret()
				.ValidationErrorMessage("[red]Not a valid password[/]")
		);
	}
}
