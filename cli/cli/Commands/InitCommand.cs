using Beamable.Common.Api;
using Beamable.Common.Api.Realms;
using cli.Utils;
using Spectre.Console;
using System.CommandLine;

namespace cli;

public class InitCommandArgs : LoginCommandArgs
{
	public bool selectEnvironment = false;
}
public class InitCommand : AppCommand<InitCommandArgs>
{
	private readonly LoginCommand _loginCommand;
	private readonly ConfigCommand _configCommand;
	private IRealmsApi _realmsApi;
	private IAliasService _aliasService;
	private IAppContext _ctx;
	private ConfigService _configService;

	public InitCommand(LoginCommand loginCommand, ConfigCommand configCommand)
		: base("init", "Initialize a new beamable project in the current directory.")
	{
		_loginCommand = loginCommand;
		_configCommand = configCommand;
	}

	public override void Configure()
	{
		AddOption(new UsernameOption(), (args, i) => args.username = i);
		AddOption(new PasswordOption(), (args, i) => args.password = i);
		AddOption(new SaveToEnvironmentOption(), (args, b) => args.saveToEnvironment = b);
		AddOption(new SaveToFileOption(), (args, b) => args.saveToFile = b);
		AddOption(new CustomerScopedOption(), (args, b) => args.customerScoped = b);
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
		var cid = GetCid(args);
		_ctx.Set(cid, _ctx.Pid, host);

		if (!AliasHelper.IsCid(cid))
		{
			var aliasResolve = await _aliasService.Resolve(cid).ShowLoading("Resolving alias...");
			cid = aliasResolve.Cid.GetOrElse(() => throw new CliException("Invalid alias"));
		}

		_configService.SetConfigString(Constants.CONFIG_CID, cid);
		await GetPidAndAuth(args, cid, host);

		AnsiConsole.MarkupLine("Success! :thumbs up: Here are your connection details");
		await _configCommand.Handle(new ConfigCommandArgs());
	}

	private async Task GetPidAndAuth(InitCommandArgs args, string cid, string host)
	{

		var hasPid = !string.IsNullOrEmpty(_ctx.Pid);
		if (hasPid)
		{
			_ctx.Set(cid, _ctx.Pid, host);
			_configService.SetBeamableDirectory(_ctx.WorkingDirectory);
			_configService.FlushConfig();

			await _loginCommand.Handle(args);

			return;
		}

		_ctx.Set(cid, null, host);
		_configService.SetBeamableDirectory(_ctx.WorkingDirectory);
		_configService.FlushConfig();

		var pid = await PickGameAndRealm(args);
		_ctx.Set(cid, pid, host);
		_configService.SetConfigString(Constants.CONFIG_PID, pid);
		_configService.FlushConfig();

		await _loginCommand.Handle(args); // login again with the scoped token
	}

	private async Task<string> PickGameAndRealm(InitCommandArgs args)
	{
		await _loginCommand.Handle(args);
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

	private string GetCid(InitCommandArgs args)
	{
		if (!string.IsNullOrEmpty(_ctx.Cid))
			return _ctx.Cid;

		return AnsiConsole.Prompt(
			new TextPrompt<string>("Please enter your [green]cid or alias[/]:")
				.PromptStyle("green")
				.ValidationErrorMessage("[red]Not a valid cid or alias[/]")
				);
	}

	private string GetHost(InitCommandArgs args)
	{
		if (!string.IsNullOrEmpty(_ctx.Host) && !args.selectEnvironment)
			return _ctx.Host;

		const string prod = "prod";
		const string staging = "staging";
		const string dev = "dev";
		const string custom = "custom";
		var env = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("What Beamable [green]environment[/] would you like to use?")
				.AddChoices(prod, staging, dev, custom)
		);

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
					})).ToString()
		});
	}
}
