using System.CommandLine.Binding;
using Beamable.Common.Api.Auth;

namespace cli;

public interface IAppContext
{
	public bool IsDryRun { get; }
	public string Cid { get; }
	public string Pid { get; }

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
	private readonly UsernameOption _usernameOption;
	private readonly IAuthApi _authApi;
	private readonly CliRequester _requester;
	public bool IsDryRun { get; set; }
	public string Cid { get; set; }
	public string Pid { get; set; }
	
	public DefaultAppContext(DryRunOption dryRunOption, CidOption cidOption, PasswordOption passwordOption, PidOption pidOption, UsernameOption usernameOption, IAuthApi authApi, CliRequester requester)
	{
		_dryRunOption = dryRunOption;
		_cidOption = cidOption;
		_passwordOption = passwordOption;
		_pidOption = pidOption;
		_usernameOption = usernameOption;
		_authApi = authApi;
		_requester = requester;
	}

	public async void Apply(BindingContext bindingContext)
	{
		IsDryRun = bindingContext.ParseResult.GetValueForOption(_dryRunOption);
		Cid = bindingContext.ParseResult.GetValueForOption(_cidOption) ?? "unset";
		Pid = bindingContext.ParseResult.GetValueForOption(_pidOption) ?? "unset";
		var password = bindingContext.ParseResult.GetValueForOption(_passwordOption) ?? string.Empty;
		var userName = bindingContext.ParseResult.GetValueForOption(_usernameOption) ?? string.Empty;
		Console.WriteLine($"{userName}:{password}");
		var resp = await _authApi.Login(userName, password);
		_requester.UpdateToken(resp, Cid, Pid);
		// generate token based on password and username
	} 
}
