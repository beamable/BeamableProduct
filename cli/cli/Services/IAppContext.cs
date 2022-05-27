using System.CommandLine.Binding;

namespace cli;

public interface IAppContext
{
	public bool IsDryRun { get; }
	public string Cid { get; }

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
	public bool IsDryRun { get; set; }
	public string Cid { get; set; }

	public DefaultAppContext(DryRunOption dryRunOption, CidOption cidOption)
	{
		_dryRunOption = dryRunOption;
		_cidOption = cidOption;
	}

	public void Apply(BindingContext bindingContext)
	{
		IsDryRun = bindingContext.ParseResult.GetValueForOption(_dryRunOption);
		Cid = bindingContext.ParseResult.GetValueForOption(_cidOption) ?? "unset";
	}
}
