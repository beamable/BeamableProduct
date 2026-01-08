using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Realms;
using cli.Services;
using cli.Utils;
using Spectre.Console;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;
using System.Text;
using Beamable.Common.BeamCli;
using Beamable.Server;
using Command = System.CommandLine.Command;

namespace cli;

public class InitCommandArgs : LoginCommandArgs
{
	public string selectedEnvironment = "";
	public string cid;
	public string pid;
	public string path;
	public List<string> addExtraPathsToFile = new List<string>();
	public List<string> pathsToIgnore = new List<string>();
	public bool ignoreExistingPid;
}

[Serializable]
public class InvalidCidError : ErrorOutput
{
	
}

public class InitCommand : AtomicCommand<InitCommandArgs, InitCommandResult>,
	IStandaloneCommand,
	IHaveRedirectionConcerns<InitCommandArgs>,
	IReportException<LoginFailedError>,
	IReportException<InvalidCidError>,
	ISkipManifest
{
	private readonly LoginCommand _loginCommand;
	private IRealmsApi _realmsApi;
	private IAppContext _ctx;
	private ConfigService _configService;
	private bool _retry = false;

	public override bool AutoLogOutput => false;

	public InitCommand(LoginCommand loginCommand)
		: base("init", "Initialize a new Beamable project in the current directory")
	{
		_loginCommand = loginCommand;
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>(
			name: "path", 
			getDefaultValue: () => ".",
			description: "the folder that will be initialized as a beamable project. "), 
			(args, i) => args.path = Path.GetFullPath(i));
		
		AddOption(new UsernameOption(), (args, i) => args.username = i);
		AddOption(new PasswordOption(), (args, i) => args.password = i);

		// Options to allow for re-initializing a project to a different host/cid/pid and user
		AddOption(new HostOption(), (args, i) => args.selectedEnvironment = i);
		AddOption(new RefreshTokenOption(), (args, i) => args.refreshToken = i);

		AddOption(new Option<bool>("--ignore-pid", "Ignore the existing pid while initializing"),
			(args, i) => args.ignoreExistingPid = i);
		AddOption(
			new Option<List<string>>(new string[] { "--save-extra-paths" }, () => new List<string>(),
				"Overwrite the stored extra paths for where to find projects")
			{
				AllowMultipleArgumentsPerToken = true,
				Arity = ArgumentArity.ZeroOrMore
			},
			(args, i) =>
			{
				args.addExtraPathsToFile = i;
			});
		
		AddOption(
			new Option<List<string>>(new string[] { "--paths-to-ignore" }, () => new List<string>(),
				"Paths to ignore when searching for services")
			{
				AllowMultipleArgumentsPerToken = true,
				Arity = ArgumentArity.ZeroOrMore
			},
			(args, i) =>
			{
				args.pathsToIgnore = i;
			});

		AddOption(new SaveToEnvironmentOption(), (args, b) => args.saveToEnvironment = b);
		SaveToFileOption.Bind(this);
		AddOption(new RealmScopedOption(), (args, b) => args.realmScoped = b);
		AddOption(new PrintToConsoleOption(), (args, b) => args.printToConsole = b);
	}


	async Task<bool> HandleExistingWorkspaceCase(InitCommandArgs args)
	{
		string cid, host, workingDir, configFolder;

		{ // find an existing .beamable folder, or don't. If not, then this is not the handle-existing-workspace case!
			workingDir = Path.Combine(args.ConfigService.WorkingDirectory, args.path);
			if (!ConfigService.TryToFindBeamableFolder(workingDir, out configFolder))
			{
				return false;
			}
			// If the config file does not exist, we should be handling this workspace as a new one and require the full-login process to happen.
			else if (args.ConfigService.GetConfigString("cid") == null || args.ConfigService.GetConfigString("pid") == null)
			{
				return false;
			}
		}

		{ // handle confirmation step to let people know they are not creating a new workspace!
			var shouldContinue = args.Quiet || AnsiConsole.Prompt(
				new ConfirmationPrompt($"Existing beamable workspace found at {configFolder}.\n" +
				                       $"Would you like to change CID and PID within the existing project? \n" +
				                       $"(Otherwise, you will create a new beamable workspace)")
					.ShowChoices());
			if (!shouldContinue)
			{
				return false;
			}
		}

		{ // switch the runtime to be operating in the existing workspace folder
			args.ConfigService.SetWorkingDir(Path.GetDirectoryName(configFolder));
		}

		{ // set the host string from existing value or given parameter, and then reset cid/pid
			host = _configService.SetConfigString(ConfigService.CFG_JSON_FIELD_HOST, GetHost(args));
			await _ctx.Set(string.Empty, string.Empty, host);
		}
		
		SaveExtraPathFiles(args);

		{ // resolve the CID
			cid = await GetCid(args);
			if (!AliasHelper.IsCid(cid))
			{
				try
				{
					var aliasResolve = await args.AliasService.Resolve(cid).ShowLoading("Resolving alias...");
					cid = aliasResolve.Cid.GetOrElse(() => throw new CliException("Invalid alias"));
				}
				catch (RequesterException)
				{
					throw new CliException<InvalidCidError>($"Organization not found for cid='{cid}', try again");
				}
				catch (AliasService.AliasDoesNotExistException)
				{
					throw new CliException<InvalidCidError>($"Organization not found for alias='{cid}', try again");
				}
				catch (Exception e)
				{
					BeamableLogger.LogError(e.Message);
					throw;
				}
				
			}
			else
			{
				// need to validate that cid actually exists...
				
			}
			_configService.SetConfigString(ConfigService.CFG_JSON_FIELD_CID, cid);
		}

		{ // resolve the new PID
			var success = await GetPidAndAuth(args, cid, host);
			if (!success)
			{
				AnsiConsole.MarkupLine(":thumbs_down: Failure! try again");
				throw new CliException("invalid authorization");
			}
		}

		return true;
	}

	void SaveExtraPathFiles(InitCommandArgs args)
	{
		// save the extra-paths and the paths to ignore to the config folder
		_configService.SaveExtraPathsToFile(args.addExtraPathsToFile);
		_configService.SavePathsToIgnoreToFile(args.pathsToIgnore);
	}


	public override async Task<InitCommandResult> GetResult(InitCommandArgs args)
	{
		var originalPath = Path.GetFullPath(Directory.GetCurrentDirectory());
		var parseResult = args.DependencyProvider.GetService<BindingContext>().ParseResult;
		args.cid = parseResult.GetValueForOption(CidOption.Instance);
		args.pid = parseResult.GetValueForOption(PidOption.Instance);

		_ctx = args.AppContext;
		_configService = args.ConfigService;
		_realmsApi = args.RealmsApi;

		
		var exitEarly = await HandleExistingWorkspaceCase(args);
		if (exitEarly)
		{
			return new InitCommandResult
			{
				host = _ctx.Host,
				cid = _ctx.Cid,
				pid = _ctx.Pid
			};
		}

		Directory.CreateDirectory(args.path);
		args.ConfigService.SetWorkingDir(args.path);
		
		// Setup integration with DotNet for C#MSs --- If we ever have integrations with other microservice languages, we 
		{
			_configService.EnforceDotNetToolsManifest(out var manifestFile);
			if (!File.Exists(manifestFile))
			{
				throw new CliException("Could not create Dotnet tools manifest file");
			}
			var (result, buffer) = await CliExtensions.RunWithOutput(_ctx.DotnetPath, "tool restore", args.path);
			if (result.ExitCode != 0)
			{
				throw new CliException($"Failed to restore Dotnet tools, command output: {buffer}");
			}
			
			if(_configService.TryGetProjectBeamableCLIVersion(out var cliVersion))
				_configService.SetConfigString(ConfigService.CFG_JSON_FIELD_CLI_VERSION, cliVersion);
		}

		SaveExtraPathFiles(args);

		if (!_retry) AnsiConsole.Write(new FigletText("Beam").Color(Color.Red));
		else await _ctx.Set(string.Empty, string.Empty, _ctx.Host);

		var host = _configService.SetConfigString(ConfigService.CFG_JSON_FIELD_HOST, GetHost(args));
		var cid = await GetCid(args);
		await _ctx.Set(cid, string.Empty, host);

		if (!AliasHelper.IsCid(cid))
		{
			try
			{
				var aliasResolve = await args.AliasService.Resolve(cid).ShowLoading("Resolving alias...");
				cid = aliasResolve.Cid.GetOrElse(() => throw new CliException("Invalid alias"));
			}
			catch (RequesterException)
			{
				BeamableLogger.LogError($"Organization not found for '{cid}', try again");
				_retry = true;
				return await GetResult(args);
			}
			catch (Exception e)
			{
				BeamableLogger.LogError(e.Message);
				throw;
			}
		}

		_configService.SetConfigString(ConfigService.CFG_JSON_FIELD_CID, cid);
		var success = await GetPidAndAuth(args, cid, host);
		if (!success)
		{
			AnsiConsole.MarkupLine(":thumbs_down: Failure! try again");
			throw new CliException("invalid authorization");
		}
		AnsiConsole.MarkupLine(":thumbs_up: Success! Here are your connection details");
		BeamableLogger.Log(args.ConfigService.ConfigDirectoryPath);
		BeamableLogger.Log($"cid=[{args.AppContext.Cid}] pid=[{args.AppContext.Pid}]");
		BeamableLogger.Log(args.ConfigService.PrettyPrint());

		var relativePath = Path.GetRelativePath(originalPath, args.path);
		if (relativePath != ".")
		{
			Log.Information($"The beamable project has been initialized in {relativePath}.\nTo get started,");
			Log.Information($" cd {relativePath}");
		}
		else
		{
			Log.Information("The beamable project has been initialized in the current folder.");
		}

		return new InitCommandResult()
		{
			host = args.ConfigService.GetConfigString(ConfigService.CFG_JSON_FIELD_HOST),
			cid = args.ConfigService.GetConfigString(ConfigService.CFG_JSON_FIELD_CID),
			pid = args.ConfigService.GetConfigString(ConfigService.CFG_JSON_FIELD_PID)
		};
	}

	private async Task<bool> GetPidAndAuth(InitCommandArgs args, string cid, string host)
	{
		var didCidChange = cid != _ctx.Cid;
		var ignoreExistingPid = args.ignoreExistingPid;
		if (didCidChange)
		{
			// if the given CID is different than the CID stored on disk, 
			//  then it is unlikely that the PID on disk should be re-used. 
			ignoreExistingPid = true;
		}
		
		// [Tech_debt] This is quite hard to read, lots of duplication calls, could use some refactor, also it does way more stuff
		//  then just returning the pid and auth
		if (!string.IsNullOrEmpty(_ctx.Pid) && string.IsNullOrEmpty(args.pid) && !ignoreExistingPid)
		{
			await _ctx.Set(cid, _ctx.Pid, host);
			_configService.SetWorkingDir(_ctx.WorkingDirectory);
			_configService.SetConfigString(ConfigService.CFG_JSON_FIELD_PID, _ctx.Pid);
			_configService.FlushConfig();
			_configService.CreateIgnoreFile();

			var didLogin = await Login(args);

			return didLogin;
		}

		// If we have a given pid, let's login there.
		if (!string.IsNullOrEmpty(args.pid))
		{
			await _ctx.Set(cid, args.pid, host);
			_configService.SetConfigString(ConfigService.CFG_JSON_FIELD_PID, args.pid);
			
			var didLogin = !args.SaveToFile || await Login(args);
			if (didLogin)
			{
				_configService.SetWorkingDir(_ctx.WorkingDirectory);
				_configService.FlushConfig();
				_configService.CreateIgnoreFile();
				return true;
			}
			else
			{
				throw new CliException("Failed to log in.");
			}
		}

		await _ctx.Set(cid, null, host);
		_configService.SetWorkingDir(_ctx.WorkingDirectory);
		_configService.FlushConfig();
		_configService.CreateIgnoreFile();

		var pid = await PickGameAndRealm(args);
		if (string.IsNullOrWhiteSpace(pid))
			throw new CliException("Failed to find a realm to target.");

		await _ctx.Set(cid, pid, host);
		_configService.SetConfigString(ConfigService.CFG_JSON_FIELD_PID, pid);
		_configService.FlushConfig();
		
		// Whenever we swap realms using init, we also clear the local override for the selected realm.
		_configService.DeleteLocalOverride(ConfigService.CFG_JSON_FIELD_PID);
		_configService.FlushLocalOverrides();
		
		return await Login(args);
	}

	private async Task<bool> Login(LoginCommandArgs args)
	{
		try
		{
			await _loginCommand.Handle(args);
			return _loginCommand.Successful;
		}
		catch (Exception ex)
		{
			BeamableLogger.LogError("Login failed. Init aborted. " + ex.Message);
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
		var gameSelection = args.Quiet
			? gameChoices.First()
			: AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.Title("What [green]game[/] are you using?")
					.AddChoices(gameChoices)
					.AddBeamHightlight()
			);
		var game = games.FirstOrDefault(g => g.DisplayName.Replace("[PROD]", "") == gameSelection);

		var realms = await _realmsApi.GetRealms(game).ShowLoading("Fetching realms...");
		var realmChoices = realms
			.Where(r => !r.Archived)
			.Select(r => $"{r.DisplayName.Replace("[", "").Replace("]", "")} - {r.Pid}");
		var realmSelection = args.Quiet
			?	FindBestDefaultRealm()
			: AnsiConsole.Prompt(new SelectionPrompt<string>()
				.Title("What [green]realm[/] are you using?")
				.AddChoices(realmChoices)
				.AddBeamHightlight()
			);
		var realm = realms.FirstOrDefault(g => realmSelection.Contains(g.Pid) && !g.Archived) ?? realms.First(g => g.IsDev);
		return realm.Pid;

		string FindBestDefaultRealm(int allowedDepth=2) // a depth of 2 signals a "dev" realm. 
		{
			if (allowedDepth < 0)
			{
				// the min depth is zero (a production realm), so at this point, there are NO realms left to use as the default.
				throw new CliException(
					"There are no valid default realms. Please select a realm manually with the --pid option");
			}
			
			var realmsAtDepth = realms.Where(r => !r.Archived && r.Depth >= allowedDepth).ToList();
			if (realmsAtDepth.Count == 0)
			{
				// uh oh, there are no non-archived realms at this depth. 
				//  it isn't ideal, but try looking for realms at a lower depth (staging, and production)
				return FindBestDefaultRealm(allowedDepth - 1);
			}

			// order the realms by PID, because PIDs are sequentially issued (higher pids mean later creation date)
			return realmsAtDepth
				.OrderBy(r => r.Pid)
				.Select(r => $"{r.DisplayName.Replace("[", "").Replace("]", "")} - {r.Pid}")
				.First();
		}
	}

	public void ValidationRedirection(InvocationContext context, Command command, InitCommandArgs args, StringBuilder errorStream,
		out bool isValid)
	{
		var ctx = args.AppContext;
		IHaveRedirectionConcerns<InitCommandArgs>.DefaultValidationRedirection(context, command, args, errorStream, out isValid);
		// add some custom concerns...
		if (string.IsNullOrEmpty(ctx.Cid) && string.IsNullOrEmpty(args.cid))
		{
			errorStream.AppendLine("must provide cid");
			isValid = false;
		}
		if (string.IsNullOrEmpty(ctx.Pid) && string.IsNullOrEmpty(args.pid))
		{
			errorStream.AppendLine("must provide pid");
			isValid = false;
		}
		if (string.IsNullOrEmpty(ctx.Host) && string.IsNullOrEmpty(args.selectedEnvironment))
		{
			errorStream.AppendLine("must provide environment");
			isValid = false;
		}
	}

	private Task<string> GetCid(InitCommandArgs args)
	{
		if (!string.IsNullOrEmpty(args.cid))
			return Task.FromResult(args.cid);

		return Task.FromResult(AnsiConsole.Prompt(
			new TextPrompt<string>("Please enter your [green]cid or alias[/]:")
				.PromptStyle("green")
				.ValidationErrorMessage("[red]Not a valid cid or alias[/]")
		));
	}

	private string GetHost(InitCommandArgs args)
	{
		if (!string.IsNullOrEmpty(_ctx.Host) && string.IsNullOrEmpty(args.selectedEnvironment))
			return _ctx.Host;

		const string prod = "prod";
		const string staging = "staging";
		const string dev = "dev";
		const string custom = "custom";

		var env = !string.IsNullOrEmpty(args.selectedEnvironment) ? args.selectedEnvironment : "prod";
		if (!args.Quiet)
		{
			env = !string.IsNullOrEmpty(args.selectedEnvironment)
				? args.selectedEnvironment
				: AnsiConsole.Prompt(
					new SelectionPrompt<string>()
						.Title("What Beamable [green]environment[/] would you like to use?")
						.AddChoices(prod, staging, dev, custom)
						.AddBeamHightlight()
				);
		}

		// If we were given a host that is a path, let's just return it.
		if (env.StartsWith("http"))
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
