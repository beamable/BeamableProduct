using System.CommandLine;
using System.CommandLine.Binding;
using Newtonsoft.Json;

namespace cli;

public interface IAppContext
{
	public bool IsDryRun { get; }
	public string Cid { get; }
	public string Pid { get; }
	public string Host { get; }

	/// <summary>
	/// Control how basic options are found from the console context.
	/// As we add more context variables, this method is responsible for "figuring them out"
	/// </summary>
	/// <param name="bindingContext"></param>
	void Apply(BindingContext bindingContext);
}

public class DefaultAppContext : IAppContext
{
	private readonly DryRunOption _dryRunOption;
	private readonly CidOption _cidOption;
	private readonly PasswordOption _passwordOption;
	private readonly PidOption _pidOption;
	private readonly PlatformOption _platformOption;
	private readonly UsernameOption _usernameOption;
	private readonly CliRequester _requester;
	private readonly ConfigService _configService;
	public bool IsDryRun { get; set; }

	private string? _cid, _pid, _host;

	public string Cid => _cid;
	public string Pid => _pid;
	public string Host => _host;

	public DefaultAppContext(DryRunOption dryRunOption, CidOption cidOption, PidOption pidOption, PlatformOption platformOption,
	                         CliRequester requester, ConfigService configService)
	{
		_dryRunOption = dryRunOption;
		_cidOption = cidOption;
		_pidOption = pidOption;
		_platformOption = platformOption;
		_requester = requester;
		_configService = configService;
	}

	public async void Apply(BindingContext bindingContext)
	{
		IsDryRun = bindingContext.ParseResult.GetValueForOption(_dryRunOption);

		if (!TryGetSetting(out _cid, bindingContext, _cidOption))
		{
			// TODO: throw an error saying a cid is required.
		}
		if (!TryGetSetting(out _pid, bindingContext, _pidOption))
		{
			// TODO: throw an error saying a cid is required.
		}

		if (!TryGetSetting(out _host, bindingContext, _platformOption))
		{
			// TODO: throw an error saying a platform is required.
		}

		_requester.Init(Cid, Pid, Host);
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