using System.CommandLine.Binding;

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
	private readonly CliRequester _requester;
	public bool IsDryRun { get; set; }
	public string Cid { get; set; }
	public string Pid { get; set; }

	public DefaultAppContext(DryRunOption dryRunOption, CidOption cidOption, PidOption pidOption, CliRequester requester)
	{
		_dryRunOption = dryRunOption;
		_cidOption = cidOption;
		_pidOption = pidOption;
		_requester = requester;
	}

	public async void Apply(BindingContext bindingContext)
	{
		IsDryRun = bindingContext.ParseResult.GetValueForOption(_dryRunOption);
		Cid = bindingContext.ParseResult.GetValueForOption(_cidOption) ?? "unset";
		Pid = bindingContext.ParseResult.GetValueForOption(_pidOption) ?? "unset";

		_requester.SetPidAndCid(Cid, Pid);
	} 
}
