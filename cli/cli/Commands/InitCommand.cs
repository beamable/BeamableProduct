using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Realms;
using cli.Utils;
using Spectre.Console;
using System.CommandLine;

namespace cli;

public class InitCommandArgs : LoginCommandArgs
{
	public string selectedEnvironment = "";
	public string cid;
	public string pid;
}

public class InitCommand : AppCommand<InitCommandArgs>, IResultSteam<DefaultStreamResultChannel, InitCommandResult>
{
	private readonly LoginCommand _loginCommand;
	private IRealmsApi _realmsApi;
	private IAliasService _aliasService;
	private IAppContext _ctx;
	private ConfigService _configService;

	public InitCommand(LoginCommand loginCommand)
		: base("init", "Initialize a new Beamable project in the current directory")
	{
		_loginCommand = loginCommand;
	}

	public override void Configure()
	{
		AddOption(new UsernameOption(), (args, i) => args.username = i);
		AddOption(new PasswordOption(), (args, i) => args.password = i);

		// Options to allow for re-initializing a project to a different host/cid/pid and user
		AddOption(new HostOption(), (args, i) => args.selectedEnvironment = i);
		AddOption(new CidOption(), (args, i) => args.cid = i);
		AddOption(new PidOption(), (args, i) => args.pid = i);
		AddOption(new RefreshTokenOption(), (args, i) => args.refreshToken = i);

		AddOption(new SaveToEnvironmentOption(), (args, b) => args.saveToEnvironment = b);
		AddOption(new SaveToFileOption(), (args, b) => args.saveToFile = b);
		AddOption(new CustomerScopedOption(), (args, b) => args.customerScoped = b);
		AddOption(new PrintToConsoleOption(), (args, b) => args.printToConsole = b);
	}

	public override async Task Handle(InitCommandArgs args)
	{
		_ctx = args.AppContext;
		_configService = args.ConfigService;
		_aliasService = args.AliasService;
		_realmsApi = args.RealmsApi;

		AnsiConsole.Write(
			new FigletText("Beam")
				.LeftAligned()
				.Color(Color.Red));

		var host = _configService.SetConfigString(Constants.CONFIG_PLATFORM, GetHost(args));
		var cid = await GetCid(args);
		_ctx.Set(cid, _ctx.Pid, host);

		if (!AliasHelper.IsCid(cid))
		{
			var aliasResolve = await _aliasService.Resolve(cid).ShowLoading("Resolving alias...");
			cid = aliasResolve.Cid.GetOrElse(() => throw new CliException("Invalid alias"));
		}

		_configService.SetConfigString(Constants.CONFIG_CID, cid);
		var success = await GetPidAndAuth(args, cid, host);
		if (!success)
		{
			AnsiConsole.MarkupLine("Failure! :thumbs_down:");
			return;
		}
		AnsiConsole.MarkupLine("Success! :thumbs_up: Here are your connection details");
		BeamableLogger.Log(args.ConfigService.ConfigFilePath);
		BeamableLogger.Log($"cid=[{args.AppContext.Cid}] pid=[{args.AppContext.Pid}]");
		BeamableLogger.Log(args.ConfigService.PrettyPrint());
		this.SendResults(new InitCommandResult()
		{
			host = args.ConfigService.GetConfigString(Constants.CONFIG_PLATFORM),
			cid = args.ConfigService.GetConfigString(Constants.CONFIG_CID),
			pid = args.ConfigService.GetConfigString(Constants.CONFIG_PID)
		});
	}

	private async Task<bool> GetPidAndAuth(InitCommandArgs args, string cid, string host)
	{
		if (!string.IsNullOrEmpty(_ctx.Pid) && string.IsNullOrEmpty(args.pid))
		{
			_ctx.Set(cid, _ctx.Pid, host);
			_configService.SetBeamableDirectory(_ctx.WorkingDirectory);
			_configService.FlushConfig();

			var didLogin = await Login(args);

			return didLogin;
		}

		// If we have a given pid, let's login there.
		if (!string.IsNullOrEmpty(args.pid))
		{
			_ctx.Set(cid, args.pid, host);

			var didLogin = await Login(args);
			if (didLogin)
			{
				_configService.SetBeamableDirectory(_ctx.WorkingDirectory);
				_configService.SetConfigString(Constants.CONFIG_PID, args.pid);
				_configService.FlushConfig();
			}
			else
			{
				_configService.RemoveConfigFolderContent();
			}

			return didLogin;
		}

		_ctx.Set(cid, null, host);
		_configService.SetBeamableDirectory(_ctx.WorkingDirectory);
		_configService.FlushConfig();

		var pid = await PickGameAndRealm(args);
		if (string.IsNullOrWhiteSpace(pid))
		{
			_configService.RemoveConfigFolderContent();
			return false;
		}

		_ctx.Set(cid, pid, host);
		_configService.SetConfigString(Constants.CONFIG_PID, pid);
		_configService.FlushConfig();

		return await Login(args);
	}

	private async Task<bool> Login(LoginCommandArgs args)
	{
		try
		{
			await _loginCommand.Handle(args);
			return _loginCommand.Successful;
		}
		catch
		{
			BeamableLogger.LogError("Login failed. Init aborted.");
			return false;
		}
	}

	private async Task<string> PickGameAndRealm(InitCommandArgs args)
	{
		var didLogin = await Login(args);
		if (!didLogin)
		{
			return string.Empty; // cannot fetch games without correct credentials
		}
		var games = await _realmsApi.GetGames().ShowLoading("Fetching games...");
		var gameChoices = games.Select(g => g.DisplayName.Replace("[PROD]", "")).ToList();
		var gameSelection = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("What [green]game[/] are you using?")
				.AddChoices(gameChoices)
		);
		var game = games.FirstOrDefault(g => g.DisplayName.Replace("[PROD]", "") == gameSelection);

		var realms = await _realmsApi.GetRealms(game).ShowLoading("Fetching realms...");
		var realmChoices = realms.Select(r => r.DisplayName.Replace("[", "").Replace("]", ""));
		var realmSelection = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("What [green]realm[/] are you using?")
				.AddChoices(realmChoices)
		);
		var realm = realms.FirstOrDefault(g => g.DisplayName.Replace("[", "").Replace("]", "") == realmSelection);
		return realm.Pid;
	}

	private async Task<string> GetCid(InitCommandArgs args)
	{
		if (!string.IsNullOrEmpty(_ctx.Cid) && string.IsNullOrEmpty(args.cid))
			return _ctx.Cid;

		if (!string.IsNullOrEmpty(args.cid))
			return args.cid;

		return AnsiConsole.Prompt(
			new TextPrompt<string>("Please enter your [green]cid or alias[/]:")
				.PromptStyle("green")
				.ValidationErrorMessage("[red]Not a valid cid or alias[/]")
		);
	}

	private string GetHost(InitCommandArgs args)
	{
		if (!string.IsNullOrEmpty(_ctx.Host) && string.IsNullOrEmpty(args.selectedEnvironment))
			return _ctx.Host;

		const string prod = "prod";
		const string staging = "staging";
		const string dev = "dev";
		const string custom = "custom";
		var env = !string.IsNullOrEmpty(args.selectedEnvironment)
			? args.selectedEnvironment
			: AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.Title("What Beamable [green]environment[/] would you like to use?")
					.AddChoices(prod, staging, dev, custom)
			);

		// If we were given a host that is a path, let's just return it.
		if (env.StartsWith("https"))
			return env;

		// Otherwise, we try to convert it into a valid URL.
		return (env switch
		{
			dev => Constants.PLATFORM_DEV,
			staging => Constants.PLATFORM_STAGING,
			prod => Constants.PLATFORM_PRODUCTION,
			custom => AnsiConsole.Prompt(
				new TextPrompt<string>("Enter the Beamable platform [green]uri[/]:")
					.PromptStyle("green")
					.ValidationErrorMessage("[red]Not a valid uri[/]")
					.Validate(age =>
					{
						if (!age.StartsWith("http://") && !age.StartsWith("https://")) return ValidationResult.Error("[red]Not a valid url[/]");
						return ValidationResult.Success();
					})).ToString(),
			_ => throw new ArgumentOutOfRangeException()
		});
	}
}

public class InitCommandResult
{
	public string host;
	public string cid;
	public string pid;
}
