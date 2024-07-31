using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.Dependencies;
using Beamable.Server.Common;
using cli.Options;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Spectre.Console;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using UnityEngine;

namespace cli;


public class AppServices : IServiceProvider
{
	public IDependencyProvider duck;
	public object GetService(Type serviceType) => duck.GetService(serviceType);
}

public interface IAppContext
{
	public bool IsDryRun { get; }
	public LogEventLevel LogLevel { get; }
	public string Cid { get; }
	public string Pid { get; }
	public string Host { get; }
	public bool UsePipeOutput { get; }
	public bool ShowRawOutput { get; }
	public bool ShowPrettyOutput { get; }
	public string DotnetPath { get; }
	public string WorkingDirectory { get; }
	public IAccessToken Token { get; }
	public string RefreshToken { get; }
	bool ShouldUseLogFile { get; }
	string TempLogFilePath { get;  }
	bool ShouldMaskLogs { get; }

	/// <summary>
	/// Control how basic options are found from the console context.
	/// As we add more context variables, this method is responsible for "figuring them out"
	/// </summary>
	/// <param name="bindingContext"></param>
	void Apply(BindingContext bindingContext);

	void Set(string cid, string pid, string host);
	void UpdateToken(TokenResponse response);
}

public class DefaultAppContext : IAppContext
{
	private readonly InvocationContext _consoleContext;
	private readonly DryRunOption _dryRunOption;
	private readonly CidOption _cidOption;
	private readonly PidOption _pidOption;
	private readonly HostOption _hostOption;
	private readonly AccessTokenOption _accessTokenOption;
	private readonly RefreshTokenOption _refreshTokenOption;
	private readonly LogOption _logOption;
	private readonly ConfigDirOption _configDirOption;
	private readonly ConfigService _configService;
	private readonly CliEnvironment _environment;
	private readonly ShowRawOutput _showRawOption;
	private readonly ShowPrettyOutput _showPrettyOption;
	private readonly LoggingLevelSwitch _logSwitch;
	private readonly UnmaskLogsOption _unmaskLogsOption;
	private readonly NoLogFileOption _noLogFileOption;
	private readonly SkipStandaloneValidationOption _skipValidationOption;
	private readonly DotnetPathOption _dotnetPathOption;
	public bool IsDryRun { get; private set; }
	public bool UsePipeOutput { get; private set; }
	public bool ShowRawOutput { get; private set; }
	public bool ShowPrettyOutput { get; private set; }

	public string DotnetPath { get; private set; }

	public bool ShouldUseLogFile => !_consoleContext.ParseResult.GetValueForOption(_noLogFileOption);
	public string TempLogFilePath => Path.Combine(Path.GetTempPath(), "beamCliLog.txt");
	public bool ShouldMaskLogs => !_consoleContext.ParseResult.GetValueForOption(_unmaskLogsOption);

	public IAccessToken Token => _token;
	private CliToken _token;

	private string _cid, _pid, _host;
	private string _refreshToken;
	private BindingContext _bindingContext;

	public string Cid => _cid;
	public string Pid => _pid;
	public string Host => _host;
	public string RefreshToken => _refreshToken;
	public string WorkingDirectory => _configService.WorkingDirectory;
	public LogEventLevel LogLevel { get; private set; }

	public DefaultAppContext(InvocationContext consoleContext, DryRunOption dryRunOption, CidOption cidOption, PidOption pidOption, HostOption hostOption,
		AccessTokenOption accessTokenOption, RefreshTokenOption refreshTokenOption, LogOption logOption, ConfigDirOption configDirOption,
		ConfigService configService, CliEnvironment environment, ShowRawOutput showRawOption, SkipStandaloneValidationOption skipValidationOption,
		DotnetPathOption dotnetPathOption, ShowPrettyOutput showPrettyOption, LoggingLevelSwitch logSwitch,
		UnmaskLogsOption unmaskLogsOption, NoLogFileOption noLogFileOption)
	{
		_consoleContext = consoleContext;
		_dryRunOption = dryRunOption;
		_cidOption = cidOption;
		_pidOption = pidOption;
		_hostOption = hostOption;
		_accessTokenOption = accessTokenOption;
		_refreshTokenOption = refreshTokenOption;
		_logOption = logOption;
		_configDirOption = configDirOption;
		_configService = configService;
		_environment = environment;
		_showRawOption = showRawOption;
		_showPrettyOption = showPrettyOption;
		_logSwitch = logSwitch;
		_unmaskLogsOption = unmaskLogsOption;
		_noLogFileOption = noLogFileOption;
		_skipValidationOption = skipValidationOption;
		_dotnetPathOption = dotnetPathOption;
	}

	void SetupOutputStrategy()
	{
		// by default, set logs to INFO
		_logSwitch.MinimumLevel = LogEventLevel.Information;

		TextWriter spectreOutput = Console.Error;
		var invisibleStream = new StringWriter();

		if (ShowRawOutput)
		{
			// when --raw is included, there are no logs by default
			_logSwitch.MinimumLevel = LogEventLevel.Fatal;

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
				Out = new AnsiConsoleOutput(spectreOutput)
			});
		}

		// Configure log level from option
		{
			var logLevelOption = _bindingContext.ParseResult.GetValueForOption(_logOption);

			if (string.IsNullOrEmpty(logLevelOption))
			{
				// do nothing.
			}
			else if (LogUtil.TryParseLogLevel(logLevelOption, out var level))
			{
				_logSwitch.MinimumLevel = level;
			}
			else if (!string.IsNullOrEmpty(_environment.LogLevel) &&
					 LogUtil.TryParseLogLevel(_environment.LogLevel, out level))
			{
				_logSwitch.MinimumLevel = level;
			}
		}
	}

	public void Apply(BindingContext bindingContext)
	{
		_bindingContext = bindingContext;
		ShowRawOutput = bindingContext.ParseResult.GetValueForOption(_showRawOption);
		ShowPrettyOutput = bindingContext.ParseResult.GetValueForOption(_showPrettyOption);
		UsePipeOutput = Console.IsOutputRedirected;

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


		_configService.Init(bindingContext);

		if (!_configService.TryGetSetting(out _cid, bindingContext, _cidOption))
		{
			// throw new CliException("cannot run without a cid. Please login.");
		}

		if (!_configService.TryGetSetting(out _pid, bindingContext, _pidOption))
		{
			// throw new CliException("cannot run without a cid. Please login.");
		}

		if (!_configService.TryGetSetting(out _host, bindingContext, _hostOption))
		{
			_host = Constants.DEFAULT_PLATFORM;
			// throw new CliException("cannot run without a cid. Please login.");
		}


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

		_token = new CliToken(accessToken, RefreshToken, _cid, _pid);
		Set(_cid, _pid, _host);


	}

	public void Set(string cid, string pid, string host)
	{
		_cid = cid;
		_pid = pid;
		_host = host;
		_token.Cid = _cid;
		_token.Pid = _pid;
	}

	public void UpdateToken(TokenResponse response)
	{
		_token = new CliToken(response, _cid, _pid);
	}

}
