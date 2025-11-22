using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.BeamCli;
using cli.Utils;
using Newtonsoft.Json;
using Spectre.Console;
using Beamable.Server;
using cli.Services;

namespace cli;

public interface IArgsWithSaveToFile
{
	public bool SaveToFile { get; set; }
}

public class LoginCommandArgs : CommandArgs, IArgsWithSaveToFile
{
	public string username;
	public string password;
	public bool saveToEnvironment;
	public bool saveToFile;
	public bool printToConsole;
	public bool realmScoped;
	public string refreshToken = "";

	public bool SaveToFile
	{
		get => saveToFile;
		set => saveToFile = value;
	}
}

[Serializable]
public class LoginResults
{
	
}

[Serializable]
public class LoginFailedError : ErrorOutput
{
}

public class LoginCommand : AppCommand<LoginCommandArgs>
	, IHaveRedirectionConcerns<LoginCommandArgs>
	, ISkipManifest
	, IReportException<LoginFailedError>
	, IResultSteam<DefaultStreamResultChannel, LoginResults>
{
	public const int LOGIN_FAILED_ERROR_CODE = 100;
	public bool Successful { get; private set; } = false;
	private IAppContext _ctx;
	private ConfigService _configService;
	private IAuthApi _authApi;

	public LoginCommand() : base("login", "Save credentials")
	{
	}

	public override void Configure()
	{
		AddOption(new UsernameOption(), (args, i) => args.username = i);
		AddOption(new PasswordOption(), (args, i) => args.password = i);
		AddOption(new SaveToEnvironmentOption(), (args, b) => args.saveToEnvironment = b);
		SaveToFileOption.Bind(this);
		AddOption(new RealmScopedOption(), (args, b) => args.realmScoped = b);
		AddOption(new RefreshTokenOption(), (args, i) => args.refreshToken = i);
		AddOption(new PrintToConsoleOption(), (args, b) => args.printToConsole = b);
	}

	public override async Task Handle(LoginCommandArgs args)
	{
		_ctx = args.AppContext;
		_configService = args.ConfigService;
		_authApi = args.AuthApi;

		TokenResponse response;
		BeamableLogger.Log($"signing into... {_ctx.Cid}.{_ctx.Pid}");

		if (string.IsNullOrEmpty(args.refreshToken))
		{
			var username = GetUserName(args);
			var password = GetPassword(args);
			try
			{
				response = await _authApi.Login(username, password, false, !args.realmScoped)
					.ShowLoading("Authorizing...");

				this.SendResults<DefaultStreamResultChannel, LoginResults>(new LoginResults());
			}
			catch (RequesterException e) when (e.RequestError.status == 401) // for invalid credentials
			{
				Log.Verbose(e.Message + " " + e.StackTrace);
				if (args.Quiet)
				{
					BeamableLogger.LogError($"Login failed: {e.RequestError.message}. Failing due to -q flag.");
					throw new CliException<LoginFailedError>(e.RequestError.message, LOGIN_FAILED_ERROR_CODE)
					{
						payload = new LoginFailedError()
					};
				}
				
				BeamableLogger.LogError($"Login failed: {e.RequestError.message} Try again");
				await Handle(args);
				return;
			}
			catch (Exception e)
			{
				Log.Verbose(e.Message + " " + e.StackTrace);
				BeamableLogger.LogError($"Login failed: {e.Message}");
				return;
			}

			args.username = username;
			args.password = password;
		}
		else
		{
			try
			{
				response = await _authApi.LoginRefreshToken(args.refreshToken).ShowLoading("Authorizing...");
			}
			catch (RequesterException e)
			{
				Log.Verbose(e.Message + " " + e.StackTrace);
				AnsiConsole.WriteLine($"Login failed: {e.RequestError.message} Try again");
				await Handle(args);
				return;
			}
			catch (Exception e)
			{
				Log.Verbose(e.Message + " " + e.StackTrace);
				BeamableLogger.LogError($"Login failed with Exception: {e.Message}");
				return;
			}
		}

		Successful = HandleResponse(args, response);
	}

	private bool HandleResponse(LoginCommandArgs args, TokenResponse response)
	{
		if (string.IsNullOrWhiteSpace(response.refresh_token))
		{
			BeamableLogger.LogError("Login failed");
			return false;
		}

		_ctx.SetToken(response);

		if (args.saveToEnvironment)
		{
			BeamableLogger.Log($"Saving refresh token as {Constants.KEY_ENV_REFRESH_TOKEN} env variable");
			Environment.SetEnvironmentVariable(Constants.KEY_ENV_REFRESH_TOKEN, response.refresh_token);
		}

		if (args.saveToFile)
		{
			BeamableLogger.Log($"Saving refresh token to {ConfigService.CFG_TOKEN_FILE_NAME}-" +
			                   " do not add it to control version system. It should be used only locally.");
			_configService.SaveTokenToFile(_ctx.Token);
		}

		if (args.printToConsole)
		{
			BeamableLogger.Log(JsonConvert.SerializeObject(response, Formatting.Indented));
		}

		return true;
	}

	private string GetUserName(LoginCommandArgs args)
	{
		if (!string.IsNullOrEmpty(args.username)) return args.username;
		return AnsiConsole.Prompt(
			new TextPrompt<string>("Please enter your [green]email[/]:")
				.PromptStyle("green")
				.ValidationErrorMessage("[red]Not a valid email[/]")
				.Validate(email =>
				{
					if (!email.Contains("@")) return ValidationResult.Error("[red]email is invalid[/]");
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
