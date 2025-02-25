using Serilog.Events;
using System.CommandLine;
using System.CommandLine.Completions;

namespace cli;

public class LogOption : ConfigurableOption
{
	public static LogOption Instance => new LogOption();
	
	private LogOption() : base("log", "Extra logs gets printed out")
	{
		AddAlias("--logs");
	}

	public override IEnumerable<CompletionItem> GetCompletions(CompletionContext context) =>
		Enum.GetNames(typeof(LogEventLevel)).Select(name => new CompletionItem(name));
}

public class AllHelpOption : Option<bool>
{
	public static readonly AllHelpOption Instance = new AllHelpOption();
	public AllHelpOption() : base("--help-all", "Show help for all commands")
	{
		AddAlias("--help-a");
		AddAlias("--helpa");
		AddAlias("--helpall");
		AddAlias("--all-help");
		AddAlias("--a-help");
		AddAlias("--allhelp");
		AddAlias("--ahelp");
		IsHidden = true;
	}
}
