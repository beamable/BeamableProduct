
using Spectre.Console;
using System.Text;
using ZLogger;

namespace cli.Utils;

public class SpectreZLoggerProcessor : IAsyncLogProcessor
{
	private readonly BeamLogSwitch _logSwitch;

	public SpectreZLoggerProcessor(BeamLogSwitch logSwitch)
	{
		_logSwitch = logSwitch;
	}
	
	public ValueTask DisposeAsync()
	{
		return ValueTask.CompletedTask;
	}

	public void Post(IZLoggerEntry log)
	{
		if (log.LogInfo.LogLevel < _logSwitch.Level) return; // skip
		
		AnsiConsole.WriteLine(log.ToString());
	}
}
