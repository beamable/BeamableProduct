﻿using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using cli.Utils;
using Newtonsoft.Json;
using Spectre.Console;

namespace cli;

public class LoginCommandArgs : CommandArgs
{
	public string username;
	public string password;
	public bool saveToEnvironment;
	public bool saveToFile;
	public bool printToConsole;
	public bool customerScoped;
	public string refreshToken = "";
}

public class LoginCommand : AppCommand<LoginCommandArgs>
{
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
		AddOption(new SaveToFileOption(), (args, b) => args.saveToFile = b);
		AddOption(new CustomerScopedOption(), (args, b) => args.customerScoped = b);
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
				response = await _authApi.Login(username, password, false, args.customerScoped)
					.ShowLoading("Authorizing...");
			}
			catch (Exception e)
			{
				BeamableLogger.LogError($"Login failed with Exception: {e.Message}");
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
			catch (Exception e)
			{
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

		_ctx.UpdateToken(response);

		if (args.saveToEnvironment)
		{
			BeamableLogger.Log($"Saving refresh token as {Constants.KEY_ENV_REFRESH_TOKEN} env variable");
			Environment.SetEnvironmentVariable(Constants.KEY_ENV_REFRESH_TOKEN, response.refresh_token);
		}

		if (args.saveToFile)
		{
			BeamableLogger.Log($"Saving refresh token to {Constants.CONFIG_TOKEN_FILE_NAME}-" +
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
