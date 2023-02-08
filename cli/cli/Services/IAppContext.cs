using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Serilog.Events;
using System.CommandLine.Binding;

namespace cli;


public class AppServices : IServiceProvider
{
	public IServiceProvider duck;
	public object GetService(Type serviceType) => duck.GetService(serviceType);
}

public interface IAppContext
{
	public bool IsDryRun { get; }
	public LogEventLevel LogLevel { get; }
	public string Cid { get; }
	public string Pid { get; }
	public string Host { get; }
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
	private readonly PlatformOption _platformOption;
	private readonly AccessTokenOption _accessTokenOption;
	private readonly RefreshTokenOption _refreshTokenOption;
	private readonly LogOption _logOption;
	private readonly ConfigDirOption _configDirOption;
	private readonly ConfigService _configService;
	private readonly CliEnvironment _environment;
	public bool IsDryRun { get; private set; }

	public IAccessToken Token => _token;
	private CliToken _token;

	private string _cid, _pid, _host, _dir;
	private string _refreshToken;

	public string Cid => _cid;
	public string Pid => _pid;
	public string Host => _host;
	public string RefreshToken => _refreshToken;
	public string WorkingDirectory => _configService.WorkingDirectory;
	public LogEventLevel LogLevel { get; private set; }

	public DefaultAppContext(DryRunOption dryRunOption, CidOption cidOption, PidOption pidOption, PlatformOption platformOption,
		AccessTokenOption accessTokenOption, RefreshTokenOption refreshTokenOption, LogOption logOption, ConfigDirOption configDirOption,
		ConfigService configService, CliEnvironment environment)
	{
		_dryRunOption = dryRunOption;
		_cidOption = cidOption;
		_pidOption = pidOption;
		_platformOption = platformOption;
		_accessTokenOption = accessTokenOption;
		_refreshTokenOption = refreshTokenOption;
		_logOption = logOption;
		_configDirOption = configDirOption;
		_configService = configService;
		_environment = environment;
	}

	public void Apply(BindingContext bindingContext)
	{
		IsDryRun = bindingContext.ParseResult.GetValueForOption(_dryRunOption);

		// Configure log level from option
		{
			var logLevelOption = bindingContext.ParseResult.GetValueForOption(_logOption);
			if (!string.IsNullOrEmpty(logLevelOption))
				App.LogLevel.MinimumLevel = LogLevel = (LogEventLevel)Enum.Parse(typeof(LogEventLevel), logLevelOption, true);
			else if (!string.IsNullOrEmpty(_environment.LogLevel))
				App.LogLevel.MinimumLevel = LogLevel = (LogEventLevel)Enum.Parse(typeof(LogEventLevel), _environment.LogLevel, true);
			else
			{
#if BEAMABLE_DEVELOPER
				App.LogLevel.MinimumLevel = LogLevel = LogEventLevel.Debug;
#else
				App.LogLevel.MinimumLevel = LogLevel = = LogEventLevel.Warning
#endif
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

		if (!_configService.TryGetSetting(out _host, bindingContext, _platformOption))
		{
			_host = Constants.DEFAULT_PLATFORM;
			// throw new CliException("cannot run without a cid. Please login.");
		}


		string defaultAccessToken = string.Empty;
		string defaultRefreshToken = string.Empty;
		if (_configService.ReadTokenFromFile(out var response))
		{
			defaultAccessToken = response.access_token;
			defaultRefreshToken = response.refresh_token;
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
		_token.Token = response.access_token;
		_token.RefreshToken = response.refresh_token;
	}

}
