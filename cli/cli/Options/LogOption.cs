using Serilog.Events;
using System.CommandLine.Completions;

namespace cli;

public class LogOption : ConfigurableOption
{
	public LogOption() : base("log", "Extra logs gets printed out")
	{
		AddAlias("--logs");
	}

	public override IEnumerable<CompletionItem> GetCompletions(CompletionContext context) =>
		Enum.GetNames(typeof(LogEventLevel)).Select(name => new CompletionItem(name));
}
