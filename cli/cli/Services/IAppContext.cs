using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.Dependencies;
using Beamable.Server.Common;
using Serilog;
using Serilog.Events;
using System.CommandLine.Binding;

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
	public bool UseFatalAsReportingChannel { get; }
	public string WorkingDirectory { get; }
	public IAccessToken Token { get; }
	public string RefreshToken { get; }

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
	private readonly EnableReporterOption _reporterOption;
	private readonly SkipStandaloneValidationOption _skipValidationOption;
	public bool IsDryRun { get; private set; }
	public bool UseFatalAsReportingChannel { get; private set; }

	public IAccessToken Token => _token;
	private CliToken _token;

	private string _cid, _pid, _host;
	private string _refreshToken;

	public string Cid => _cid;
	public string Pid => _pid;
	public string Host => _host;
	public string RefreshToken => _refreshToken;
	public string WorkingDirectory => _configService.WorkingDirectory;
	public LogEventLevel LogLevel { get; private set; }

	public DefaultAppContext(DryRunOption dryRunOption, CidOption cidOption, PidOption pidOption, HostOption hostOption,
		AccessTokenOption accessTokenOption, RefreshTokenOption refreshTokenOption, LogOption logOption, ConfigDirOption configDirOption,
		ConfigService configService, CliEnvironment environment, EnableReporterOption reporterOption, SkipStandaloneValidationOption skipValidationOption)
	{
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
		_reporterOption = reporterOption;
		_skipValidationOption = skipValidationOption;
	}

	public void Apply(BindingContext bindingContext)
	{
		UseFatalAsReportingChannel = bindingContext.ParseResult.GetValueForOption(_reporterOption);
		IsDryRun = bindingContext.ParseResult.GetValueForOption(_dryRunOption);

		// Configure log level from option
		{
			var logLevelOption = bindingContext.ParseResult.GetValueForOption(_logOption);

			if (string.IsNullOrEmpty(logLevelOption))
			{
				App.LogLevel.MinimumLevel = LogEventLevel.Information;
			}
			else if (LogUtil.TryParseLogLevel(logLevelOption, out var level))
			{
				App.LogLevel.MinimumLevel = level;
			}
			else if (!string.IsNullOrEmpty(_environment.LogLevel) &&
					 LogUtil.TryParseLogLevel(_environment.LogLevel, out level))
			{
				App.LogLevel.MinimumLevel = level;
			}
		}
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
