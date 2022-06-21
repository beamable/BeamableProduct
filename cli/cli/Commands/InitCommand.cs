using System.CommandLine;
using Beamable.Common.Api;
using Spectre.Console;

namespace cli;

public class InitCommandArgs : LoginCommandArgs
{
	public bool forcePrompt = false;
}
public class InitCommand : AppCommand<InitCommandArgs>
{
	private readonly IAppContext _ctx;
	private readonly ConfigService _configService;
	private readonly LoginCommand _loginCommand;

	public InitCommand(IAppContext ctx, ConfigService configService, LoginCommand loginCommand)
		: base("init", "Initialize a new beamable project in the current directory.")
	{
		_ctx = ctx;
		_configService = configService;
		_loginCommand = loginCommand;
	}

	public override void Configure()
	{
		AddOption(new UsernameOption(), (args, i) => args.username = i);
		AddOption(new PasswordOption(), (args, i) => args.password = i);

		AddOption(new Option<bool>("--force", "forces user prompts and ignores any other configurations"), (args, i) => args.forcePrompt = i);
	}

	public override async Task Handle(InitCommandArgs args)
	{
		AnsiConsole.Write(
			new FigletText("Beam")
				.LeftAligned()
				.Color(Color.Red));

		var cid = _configService.SetConfigString(Constants.CONFIG_CID, GetCid(args));
		var pid = _configService.SetConfigString(Constants.CONFIG_PID, GetPid(args));
		var host = _configService.SetConfigString(Constants.CONFIG_PLATFORM, GetHost(args));

		_ctx.Set(cid, pid, host);
		_configService.SetBeamableDirectory(_ctx.WorkingDirectory);
		_configService.FlushConfig();

		await _loginCommand.Handle(args);

	}

	private string GetCid(InitCommandArgs args)
	{
		if (!args.forcePrompt && !string.IsNullOrEmpty(_ctx.Cid))
			return _ctx.Cid;

		return AnsiConsole.Prompt(
			new TextPrompt<string>("Please enter your [green]cid or alias[/]:")
				.PromptStyle("green")
				.ValidationErrorMessage("[red]Not a valid cid or alias[/]")
				);
	}

	private string GetPid(InitCommandArgs args)
	{
		if (!args.forcePrompt && !string.IsNullOrEmpty(_ctx.Pid))
			return _ctx.Pid;

		return  AnsiConsole.Prompt(
			new TextPrompt<string>("Please enter your [green]pid[/]:")
				.PromptStyle("green")
				.ValidationErrorMessage("[red]Not a valid pid[/]")
				.Validate(age =>
				{
					if (!age.StartsWith("DE_")) return ValidationResult.Error("[red]Pid must start with DE_[/]");
					if (age.Length < 3) return ValidationResult.Error("[red]Pid is too short[/]");
					return ValidationResult.Success();
				})).ToString();
	}


	private string GetHost(InitCommandArgs args)
	{
		if (!args.forcePrompt && !string.IsNullOrEmpty(_ctx.Host))
			return _ctx.Host;

		var env = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("What Beamable [green]environment[/] would you like to use?")
				.AddChoices("prod", "staging", "dev", "custom")
		);

		return (env switch
		{
			"dev" => Constants.PLATFORM_DEV,
			"staging" => Constants.PLATFORM_STAGING,
			"prod" => Constants.PLATFORM_PRODUCTION,
			"custom" => AnsiConsole.Prompt(
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