using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
// using Serilog.Sinks.SystemConsole.Themes;
using Spectre.Console;
using System.Text;

namespace cli.Utils;

public static class AnsiConsoleSinkExtensions
{
	public static LoggerConfiguration BeamAnsi(
		this LoggerSinkConfiguration sinkConfiguration,
		string outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
		)
	{
		var formatter = new MessageTemplateTextFormatter(outputTemplate);
		return sinkConfiguration.Sink(new AnsiConsoleSink(formatter));
	}
}

public class AnsiConsoleSink : ILogEventSink
{
	private readonly ITextFormatter _formatter;
	private readonly StringWriter _writer;

	public AnsiConsoleSink(
		ITextFormatter formatter)
	{
		_writer = new StringWriter();
		this._formatter = formatter;
	}

	public void Emit(LogEvent logEvent)
	{
		_writer.GetStringBuilder().Clear();
		_formatter.Format(logEvent, _writer);
		_writer.Flush();
		var str = _writer.GetStringBuilder().ToString();
		AnsiConsole.WriteLine(str);
	}

}
