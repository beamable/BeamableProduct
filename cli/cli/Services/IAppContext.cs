using System.CommandLine;
using System.CommandLine.Binding;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Newtonsoft.Json;

namespace cli;

public interface IAppContext
{
	public bool IsDryRun { get; }
	public bool IsVerbose { get; }
	public string? Cid { get; }
	public string? Pid { get; }
	public string? Host { get; }
	public string WorkingDirectory { get; }
	public IAccessToken Token { get; }

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
	private readonly PasswordOption _passwordOption;
	private readonly PidOption _pidOption;
	private readonly PlatformOption _platformOption;
	private readonly AccessTokenOption _accessTokenOption;
	private readonly RefreshTokenOption _refreshTokenOption;
	private readonly VerboseOption _verboseOption;
	private readonly UsernameOption _usernameOption;
	private readonly ConfigService _configService;
	public bool IsDryRun { get; private set; }
	public bool IsVerbose { get; private set; }

	public IAccessToken Token => _token;
	private CliToken _token;

	private string? _cid, _pid, _host;

	public string? Cid => _cid;
	public string? Pid => _pid;
	public string? Host => _host;

	public string WorkingDirectory { get; private set; }

	public DefaultAppContext(DryRunOption dryRunOption, CidOption cidOption, PidOption pidOption, PlatformOption platformOption,
									AccessTokenOption accessTokenOption, RefreshTokenOption refreshTokenOption, VerboseOption verboseOption,
	                         ConfigService configService)
	{
		_dryRunOption = dryRunOption;
		_cidOption = cidOption;
		_pidOption = pidOption;
		_platformOption = platformOption;
		_accessTokenOption = accessTokenOption;
		_refreshTokenOption = refreshTokenOption;
		_verboseOption = verboseOption;
		_configService = configService;
	}

	public void Apply(BindingContext bindingContext)
	{
		IsDryRun = bindingContext.ParseResult.GetValueForOption(_dryRunOption);
		IsVerbose = bindingContext.ParseResult.GetValueForOption(_verboseOption);
		WorkingDirectory = Directory.GetCurrentDirectory();
		if (!TryGetSetting(out _cid, bindingContext, _cidOption))
		{
			// throw new CliException("cannot run without a cid. Please login.");
		}

		if (!TryGetSetting(out _pid, bindingContext, _pidOption))
		{
			// throw new CliException("cannot run without a cid. Please login.");
		}

		if (!TryGetSetting(out _host, bindingContext, _platformOption))
		{
			// throw new CliException("cannot run without a cid. Please login.");
		}

		TryGetSetting(out var accessToken, bindingContext, _accessTokenOption);
		TryGetSetting(out var refreshToken, bindingContext, _refreshTokenOption);

		_token = new CliToken(accessToken, refreshToken, _cid, _pid);
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

	private bool TryGetSetting(out string? value, BindingContext context, ConfigurableOption option, string? defaultValue=null)
	{
		value = context.ParseResult.GetValueForOption(option);
		if (value == null)
		{
			value = _configService.GetConfigString(option.OptionName, defaultValue);
		}

		var hasValue = !string.IsNullOrEmpty(value);
		return hasValue;
	}
}