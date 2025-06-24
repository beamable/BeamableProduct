

using System.Buffers;
using System.Text;
using Spectre.Console;
using ZLogger;

namespace cli.Utils;

public class SpectreZLoggerProcessor : IAsyncLogProcessor
{
	private readonly BeamLogSwitch _logSwitch;
	private readonly ZLoggerOptions _options;
	private IZLoggerFormatter _formatter;

	public SpectreZLoggerProcessor(BeamLogSwitch logSwitch, ZLoggerOptions options)
	{
		_logSwitch = logSwitch;
		_options = options;

		// TODO: it is dangerous to create the formatter in the constructor, because the OPTIONS 
		//  maybe changed after the builder creates this processor. But for now, :shrug:
		_formatter = options.CreateFormatter();
	}
	
	public ValueTask DisposeAsync()
	{
		return ValueTask.CompletedTask;
	}

	public void Post(IZLoggerEntry log)
	{
		if (log.LogInfo.LogLevel < _logSwitch.Level) return; // skip

		var buffer = new ArrayBufferWriter<byte>();
		_formatter.FormatLogEntry(buffer, log);
		var result = Encoding.UTF8.GetString(buffer.WrittenMemory.Span);
		
		AnsiConsole.WriteLine(result);
	}
}
