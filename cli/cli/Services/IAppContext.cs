using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Realms;
using Beamable.Common.Dependencies;
using Beamable.Server.Common;
using cli.Options;
using cli.Services;
using Spectre.Console;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;
using Microsoft.Extensions.Logging;
using Beamable.Server;

namespace cli;


public class AppServices : IServiceProvider
{
	public IDependencyProvider duck;
	public object GetService(Type serviceType) => duck.GetService(serviceType);
}

public interface IAppContext : IRealmInfo, IRequesterInfo
{
	public BeamLogSwitch LogSwitch { get; }
	public string Cid { get; }
	public string Pid { get; }
	public string EngineCalling { get; }
	public string EngineSdkVersion { get; }
	public string EngineVersion { get; }
	public bool PreferRemoteFederation { get; }
	public bool UsePipeOutput { get; }
	public bool ShowRawOutput { get; }
	public bool ShowPrettyOutput { get; }
	public string DotnetPath { get; }
	public HashSet<string> IgnoreBeamoIds { get; }
	public string WorkingDirectory { get; }
	public string RefreshToken { get; }
	bool ShouldUseLogFile { get; }
	bool TryGetTempLogFilePath(out string logFile);
	bool ShouldMaskLogs { get; }
	bool ShouldEmitLogs { get; }
	
	/// <summary>
	/// The version of the CLI that is currently running.
	/// </summary>
	string ExecutingVersion { get; }
	
	/// <summary>
	/// true if the CLI is running in a directory that has a .beamable folder and a .config/dotnet-tools.json
	/// </summary>
	bool IsLocalProject { get; }
	
	/// <summary>
	/// The version of the CLI defined in the local project's .config/dotnet-tools.json file; or null if this
	/// isn't a local project. 
	/// </summary>
	string LocalProjectVersion { get; }
	string DockerPath { get; }

	/// <summary>
	/// Control how basic options are found from the console context.
	/// As we add more context variables, this method is responsible for "figuring them out"
	/// </summary>
	/// <param name="bindingContext"></param>
	Task Apply(BindingContext bindingContext);

	/// <summary>
	/// Sets a new cid/pid/host combination ONLY at runtime. Does not actually save this to disk.
	/// </summary>
	Task Set(string cid, string pid, string host);
	
}

public class DefaultAppContext : IAppContext
{
	private readonly InvocationContext _consoleContext;
	private readonly DryRunOption _dryRunOption;
	private readonly CidOption _cidOption;
	private readonly EngineCallerOption _engineOption;
	private readonly EngineVersionOption _engineVersionOption;
	private readonly EngineSdkVersionOption _engineSdkVersionOption;
	private readonly PidOption _pidOption;
	private readonly HostOption _hostOption;
	private readonly AccessTokenOption _accessTokenOption;
	private readonly RefreshTokenOption _refreshTokenOption;
	private readonly LogOption _logOption;
	private readonly ConfigService _configService;
	private readonly ShowRawOutput _showRawOption;
	private readonly ShowPrettyOutput _showPrettyOption;
	private readonly BeamLogSwitch _logSwitch;
	private readonly UnmaskLogsOption _unmaskLogsOption;
	private readonly NoLogFileOption _noLogFileOption;
	private readonly PreferRemoteFederationOption _routeMapOption;
	private readonly SkipStandaloneValidationOption _skipValidationOption;
	private readonly DotnetPathOption _dotnetPathOption;
	public bool IsDryRun { get; private set; }
	public bool PreferRemoteFederation { get; private set; }
	public bool UsePipeOutput { get; private set; }
	public bool ShowRawOutput { get; private set; }
	public bool ShowPrettyOutput { get; private set; }

	public string DotnetPath { get; private set; }
	public string DockerPath { get; private set; }
	public HashSet<string> IgnoreBeamoIds { get; private set; }


	/// <inheritdoc cref="IAppContext.ExecutingVersion"/>
	public string ExecutingVersion => VersionService.GetNugetPackagesForExecutingCliVersion().ToString();

	/// <inheritdoc cref="IAppContext.IsLocalProject"/>
	public bool IsLocalProject => LocalProjectVersion != null;

	/// <inheritdoc cref="IAppContext.LocalProjectVersion"/>
	public string LocalProjectVersion
	{
		get
		{
			if (_configService.TryGetProjectBeamableCLIVersion(out var version))
			{
				return version;
			}

			return null;
		}
	}

	public bool ShouldUseLogFile => !_consoleContext.ParseResult.GetValueForOption(_noLogFileOption);

	static DateTimeOffset _logTime = DateTimeOffset.Now;

	public bool TryGetTempLogFilePath(out string logFile)
	{
		logFile = null;
		if (! (_configService.DirectoryExists ?? false))
		{
			// there is no .beamable folder
			return false;
		}

		
		var subPath = Path.Combine(
			".beamable",
			"temp",
			"logs",
			$"beamCliLog-{_logTime.ToFileTime()}.txt");
		logFile = _configService.GetRelativeToExecutionPath(subPath);
		
		return true;
	}
	
	public bool ShouldMaskLogs => !_consoleContext.ParseResult.GetValueForOption(_unmaskLogsOption);
	public bool ShouldEmitLogs => _consoleContext.ParseResult.GetValueForOption(EmitLogsOption.Instance);

	public IAccessToken Token => _token;
	private CliToken _token;

	private string _cid, _pid, _host;
	private string _engine, _engineVersion, _engineSdkVersion;
	private string _refreshToken;
	private BindingContext _bindingContext;
	private IDependencyProvider _provider;
	public string Cid => _cid;
	public string Pid => _pid;
	public string Host => _host;
	public string EngineCalling => _engine;
	public string EngineSdkVersion => _engineSdkVersion;
	public string EngineVersion => _engineVersion;
	public string RefreshToken => _refreshToken;
	public string WorkingDirectory => _configService.WorkingDirectory;
	public BeamLogSwitch LogSwitch => _logSwitch;

	public DefaultAppContext(InvocationContext consoleContext, DryRunOption dryRunOption, CidOption cidOption, EngineCallerOption engineOption, EngineVersionOption engineVersionOption, EngineSdkVersionOption engineSdkVersionOption,PidOption pidOption, HostOption hostOption,
		AccessTokenOption accessTokenOption, RefreshTokenOption refreshTokenOption, LogOption logOption,
		ConfigService configService, ShowRawOutput showRawOption, SkipStandaloneValidationOption skipValidationOption,
		DotnetPathOption dotnetPathOption, ShowPrettyOutput showPrettyOption, BeamLogSwitch logSwitch,
		UnmaskLogsOption unmaskLogsOption, NoLogFileOption noLogFileOption, DockerPathOption dockerPathOption,
		PreferRemoteFederationOption routeMapOption, IDependencyProvider provider)
	{
		_provider = provider;
		_consoleContext = consoleContext;
		_dryRunOption = dryRunOption;
		_cidOption = cidOption;
		_engineOption = engineOption;
		_engineVersionOption = engineVersionOption;
		_engineSdkVersionOption = engineSdkVersionOption;
		_pidOption = pidOption;
		_hostOption = hostOption;
		_accessTokenOption = accessTokenOption;
		_refreshTokenOption = refreshTokenOption;
		_logOption = logOption;
		_configService = configService;
		_showRawOption = showRawOption;
		_showPrettyOption = showPrettyOption;
		_logSwitch = logSwitch;
		_unmaskLogsOption = unmaskLogsOption;
		_noLogFileOption = noLogFileOption;
		_routeMapOption = routeMapOption;
		_skipValidationOption = skipValidationOption;
		_dotnetPathOption = dotnetPathOption;
		DockerPath = consoleContext.ParseResult.GetValueForOption(dockerPathOption);
		IgnoreBeamoIds =
			new HashSet<string>(consoleContext.ParseResult.GetValueForOption(IgnoreBeamoIdsOption.Instance));
	}

	void SetupOutputStrategy()
	{
		// by default, set logs to INFO
		_logSwitch.Level = LogLevel.Information;

		TextWriter spectreOutput = Console.Error;
		var invisibleStream = new StringWriter();

		if (ShowRawOutput)
		{
			// when --raw is included, there are no logs by default
			_logSwitch.Level = LogLevel.Critical;

			// the user has asked for raw output, which means by default, pretty must be request.
			if (ShowPrettyOutput)
			{
				spectreOutput = Console.Error;
			}
			else
			{
				spectreOutput = invisibleStream;
			}
		}

		if (UsePipeOutput)
		{
			// the user is piping the raw data, which means by default, there is nothing on stderr, 
			//  unless pretty print has been requested
			if (ShowPrettyOutput)
			{
				spectreOutput = Console.Error;
			}
			else
			{
				spectreOutput = invisibleStream;
			}
		}

		if (AnsiConsole.Console.GetType().Assembly == typeof(AnsiConsole).Assembly)
		{
			AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings
			{
				Out = new AnsiConsoleOutput(spectreOutput),
				Enrichment = new ProfileEnrichment
				{
					Enrichers = new List<IProfileEnricher>(),
					UseDefaultEnrichers = false
				},
			});
		}

		// Configure log level from option
		{
			var logLevelOption = _bindingContext.ParseResult.GetValueForOption(_logOption);
			if (!string.IsNullOrEmpty(logLevelOption) && LogUtil.TryParseSystemLogLevel(logLevelOption, out var level)) 
				_logSwitch.Level = level;
		}
	}

	public async Task Apply(BindingContext bindingContext)
	{
		_bindingContext = bindingContext;
		ShowRawOutput = bindingContext.ParseResult.GetValueForOption(_showRawOption);
		ShowPrettyOutput = bindingContext.ParseResult.GetValueForOption(_showPrettyOption);
		UsePipeOutput = Console.IsOutputRedirected;

		PreferRemoteFederation = _bindingContext.ParseResult.GetValueForOption(_routeMapOption);
		IsDryRun = bindingContext.ParseResult.GetValueForOption(_dryRunOption);

		DotnetPath = bindingContext.ParseResult.GetValueForOption(_dotnetPathOption);
		if (string.IsNullOrEmpty(DotnetPath))
		{
			DotnetPath = Environment.GetEnvironmentVariable(Beamable.Common.Constants.EnvironmentVariables.BEAM_DOTNET_PATH);

			if (string.IsNullOrEmpty(DotnetPath))
			{
				DotnetPath = "dotnet";
			}
		}

		SetupOutputStrategy();


		_configService.RefreshConfig();

		if (!_configService.TryGetSetting(out string cid, bindingContext, _cidOption))
		{
			// throw new CliException("cannot run without a cid. Please login.");
		}

		if (!_configService.TryGetSetting(out string pid, bindingContext, _pidOption))
		{
			// throw new CliException("cannot run without a cid. Please login.");
		}

		if (!_configService.TryGetSetting(out string host, bindingContext, _hostOption))
		{
			host = Constants.DEFAULT_PLATFORM;
			// throw new CliException("cannot run without a cid. Please login.");
		}

		if (!_configService.TryGetSetting(out string engine, bindingContext, _engineOption))
		{
		}

		if (!_configService.TryGetSetting(out string engineVersion, bindingContext, _engineVersionOption))
		{

		}

		if (!_configService.TryGetSetting(out string engineSdkVersion, bindingContext, _engineSdkVersionOption))
		{

		}

		_engine = engine;
		_engineVersion = engineVersion;
		_engineSdkVersion = engineSdkVersion;

		string defaultAccessToken = string.Empty;
		string defaultRefreshToken = string.Empty;
		if (_configService.ReadTokenFromFile(out var response))
		{
			if (response.ExpiresAt > DateTime.Now)
			{
				defaultAccessToken = response.Token;
			}
			defaultRefreshToken = response.RefreshToken;
		}
		_configService.TryGetSetting(out var accessToken, bindingContext, _accessTokenOption, defaultAccessToken);
		_configService.TryGetSetting(out _refreshToken, bindingContext, _refreshTokenOption, defaultRefreshToken);


		_token = new CliToken(accessToken, RefreshToken, cid, pid);
		await Set(cid, pid, host);
	}

	public async Task Set(string cid, string pid, string host)
	{
		_host = host;

		if (!string.IsNullOrEmpty(cid))
		{
			var service = _provider.GetService<IAliasService>();
			service.Requester = new NoAuthHttpRequester(host);
			var aliasResolve = await service.Resolve(cid);
			_cid = aliasResolve.Cid;
		}
		else
		{
			_cid = cid;
		}
		_pid = pid;
		_token.Cid = _cid;
		_token.Pid = _pid;
	}

	public void SetToken(TokenResponse tokenResponse)
	{
		_token = new CliToken(tokenResponse, _cid, _pid);
	}

	string IRealmInfo.CustomerID => _cid;

	string IRealmInfo.ProjectName => _pid;
}
