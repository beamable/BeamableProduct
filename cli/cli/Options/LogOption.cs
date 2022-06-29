using System.CommandLine.Completions;
using Serilog.Events;

namespace cli;

public class LogOption : ConfigurableOption
{
	public LogOption() : base("log", "should extra logs get printed out")
	{
	}

	public override IEnumerable<CompletionItem> GetCompletions(CompletionContext context) => 
		Enum.GetNames(typeof(LogEventLevel)).Select(name => new CompletionItem(name));
}